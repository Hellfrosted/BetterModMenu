using System;
using Godot;
using BetterModMenu.Data;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.Screens.ModdingScreen;

namespace BetterModMenu.Patches;

internal static class ModdingScreenChromeOps
{
    private static readonly string[] LayoutControlPaths =
    {
        "%InstalledModsTitle",
        "%ModsScrollContainer",
        "%ModInfoContainer"
    };

    public static void PrepareScreen(
        NModdingScreen screen,
        ModdingScreenSession session,
        Action<long> onProfileSelected,
        Action onNewProfilePressed,
        Action onRenameProfilePressed,
        Action onDeleteProfilePressed,
        Action<bool> onPortableModeToggled,
        Action onManualBackupPressed,
        Action onLoadBackupPressed,
        Action onExportModListPressed,
        Action onViewLogsPressed,
        Action onTutorialPressed,
        Action onCloudBackupPressed,
        Func<string, bool> onAddGroupRequested)
    {
        ApplyScrollMaskClip(screen);
        EnsureChromeRoot(screen, session);
        EnsureLayoutSignals(screen, session);
        EnsureScrollbarPersistenceSignals(screen, session);
        EnsureTopBar(session, onProfileSelected, onNewProfilePressed, onRenameProfilePressed, onDeleteProfilePressed);
        EnsureGroupBar(session, onPortableModeToggled, onManualBackupPressed, onLoadBackupPressed, onExportModListPressed, onViewLogsPressed, onTutorialPressed, onCloudBackupPressed, onAddGroupRequested);
    }

    public static void RefreshGroupsUI(
        NModdingScreen screen,
        ModdingScreenSession session,
        Action refreshGroupsUI,
        Action<string> renameGroup,
        Action<string, int> moveGroup,
        Action<string, bool> toggleAllInGroup)
    {
        var modRowContainer = ModdingScreenNodeOps.GetModRowContainer(screen);
        if (modRowContainer == null)
            return;

        ModdingScreenGroupUi.RefreshGroupsUI(
            modRowContainer,
            session.GeneratedGroupNodes,
            refreshGroupsUI,
            renameGroup,
            moveGroup,
            toggleAllInGroup);

        SyncModsScrollbar(screen, modRowContainer);
        UpdateLayout(screen, session);
        Callable.From(() =>
        {
            if (ModdingScreenContext.IsCurrentScreen(screen))
            {
                SyncModsScrollbar(screen, modRowContainer);
                KeepModsScrollbarVisible(screen);
            }
        }).CallDeferred();
    }

    public static void UpdateLayout(NModdingScreen screen, ModdingScreenSession session)
    {
        var chromeRoot = session.ChromeRoot;
        if (chromeRoot == null || !GodotObject.IsInstanceValid(chromeRoot))
            return;

        chromeRoot.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);

        var titleNode = screen.GetNodeOrNull<Control>("%InstalledModsTitle");
        var scrollContainer = screen.GetNodeOrNull<Control>("%ModsScrollContainer");
        Vector2 screenOffset = screen.GlobalPosition;

        float groupBarHeight = GetGroupBarHeight(scrollContainer);

        if (scrollContainer != null)
            ReserveModListHeaderSpace(session, scrollContainer, groupBarHeight);

        if (session.TopBarControls != null && GodotObject.IsInstanceValid(session.TopBarControls.Bar))
            LayoutTopBar(session.TopBarControls, titleNode, scrollContainer, screenOffset);

        if (session.GroupBarControls != null && GodotObject.IsInstanceValid(session.GroupBarControls.Bar))
            LayoutGroupBar(session.GroupBarControls, scrollContainer, screenOffset, groupBarHeight);

        EnsureScrollbarPersistenceSignals(screen, session);
        KeepModsScrollbarVisible(screen);
    }

    private static void ApplyScrollMaskClip(NModdingScreen screen)
    {
        var scrollContainer = screen.GetNodeOrNull<Control>("%ModsScrollContainer");
        if (scrollContainer == null)
            return;

        var mask = scrollContainer.GetNodeOrNull<Control>("Mask");
        if (mask != null)
            mask.ClipContents = true;

        if (screen.GetNodeOrNull<NScrollableContainer>("%ModsScrollContainer") is { Scrollbar: not null } modsScroll)
            MaintainModsScrollbar(screen, modsScroll);
    }

    private static void EnsureChromeRoot(NModdingScreen screen, ModdingScreenSession session)
    {
        if (session.ChromeRoot != null && GodotObject.IsInstanceValid(session.ChromeRoot) && session.ChromeRoot.GetParent() == screen)
            return;

        var chromeRoot = new Control
        {
            Name = ModdingScreenConstants.ChromeRootName,
            MouseFilter = Control.MouseFilterEnum.Ignore
        };
        chromeRoot.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        screen.AddChild(chromeRoot);
        session.ChromeRoot = chromeRoot;
    }

    private static void EnsureTopBar(
        ModdingScreenSession session,
        Action<long> onProfileSelected,
        Action onNewProfilePressed,
        Action onRenameProfilePressed,
        Action onDeleteProfilePressed)
    {
        if (session.ChromeRoot == null)
            return;

        if (session.TopBarControls != null &&
            GodotObject.IsInstanceValid(session.TopBarControls.Bar) &&
            session.TopBarControls.Bar.GetParent() == session.ChromeRoot)
        {
            return;
        }

        var builtTopBar = ModdingScreenBars.CreateTopBar(
            onProfileSelected,
            onNewProfilePressed,
            onRenameProfilePressed,
            onDeleteProfilePressed);

        session.TopBarControls = builtTopBar;
        session.ChromeRoot.AddChild(builtTopBar.Bar);
    }

    private static void EnsureGroupBar(
        ModdingScreenSession session,
        Action<bool> onPortableModeToggled,
        Action onManualBackupPressed,
        Action onLoadBackupPressed,
        Action onExportModListPressed,
        Action onViewLogsPressed,
        Action onTutorialPressed,
        Action onCloudBackupPressed,
        Func<string, bool> onAddGroupRequested)
    {
        if (session.ChromeRoot == null)
            return;

        if (session.GroupBarControls != null &&
            GodotObject.IsInstanceValid(session.GroupBarControls.Bar) &&
            session.GroupBarControls.Bar.GetParent() == session.ChromeRoot)
        {
            return;
        }

        bool portableModeEnabled = ProfileManager.TryGetPortableConfigPath(out string portableConfigPath) &&
            System.IO.File.Exists(portableConfigPath);
        var builtGroupBar = ModdingScreenBars.CreateGroupBar(
            portableModeEnabled,
            onPortableModeToggled,
            onManualBackupPressed,
            onLoadBackupPressed,
            onExportModListPressed,
            onViewLogsPressed,
            onTutorialPressed,
            onCloudBackupPressed,
            onAddGroupRequested);

        session.GroupBarControls = builtGroupBar;
        session.ChromeRoot.AddChild(builtGroupBar.Bar);
    }

    private static void EnsureLayoutSignals(NModdingScreen screen, ModdingScreenSession session)
    {
        if (session.LayoutSignalsConnected)
            return;

        session.LayoutSignalsConnected = true;
        screen.Resized += () => UpdateLayoutIfCurrent(screen, session);

        foreach (string path in LayoutControlPaths)
        {
            var control = screen.GetNodeOrNull<Control>(path);
            if (control != null)
                control.Resized += () => UpdateLayoutIfCurrent(screen, session);
        }
    }

    private static void UpdateLayoutIfCurrent(NModdingScreen screen, ModdingScreenSession session)
    {
        if (ModdingScreenContext.IsCurrentScreen(screen))
            UpdateLayout(screen, session);
    }

    private static void EnsureScrollbarPersistenceSignals(NModdingScreen screen, ModdingScreenSession session)
    {
        if (session.ModsScrollbarPersistenceSignalsConnected)
            return;

        if (screen.GetNodeOrNull<NScrollableContainer>("%ModsScrollContainer") is not { Scrollbar: not null } scrollContainer)
            return;

        session.ModsScrollbarPersistenceSignalsConnected = true;
        scrollContainer.GuiInput += _ => ReapplyModsScrollbarAfterFrame(screen, scrollContainer);
        scrollContainer.Scrollbar.GuiInput += _ => ReapplyModsScrollbarAfterFrame(screen, scrollContainer);
        scrollContainer.Scrollbar.VisibilityChanged += () =>
        {
            if (!GodotObject.IsInstanceValid(scrollContainer) ||
                scrollContainer.Scrollbar == null ||
                scrollContainer.Scrollbar.Visible)
            {
                return;
            }

            ReapplyModsScrollbarAfterFrame(screen, scrollContainer);
        };
    }

    private static void SyncModsScrollbar(NModdingScreen screen, Control modRowContainer)
    {
        if (modRowContainer is Container container)
            container.QueueSort();

        modRowContainer.UpdateMinimumSize();

        var scrollContainer = screen.GetNodeOrNull<NScrollableContainer>("%ModsScrollContainer");
        var viewport = modRowContainer.GetParentOrNull<Control>();
        if (scrollContainer?.Scrollbar == null || viewport == null)
            return;

        MaintainModsScrollbar(screen, scrollContainer);
    }

    private static void KeepModsScrollbarVisible(NModdingScreen screen)
    {
        if (screen.GetNodeOrNull<NScrollableContainer>("%ModsScrollContainer") is not { Scrollbar: not null } scrollContainer)
            return;

        MaintainModsScrollbar(screen, scrollContainer);
        Callable.From(() =>
        {
            if (ModdingScreenContext.IsCurrentScreen(screen) && GodotObject.IsInstanceValid(scrollContainer))
                MaintainModsScrollbar(screen, scrollContainer);
        }).CallDeferred();
    }

    private static void ReapplyModsScrollbarAfterFrame(NModdingScreen screen, NScrollableContainer scrollContainer)
    {
        Callable.From(() =>
        {
            if (ModdingScreenContext.IsCurrentScreen(screen) && GodotObject.IsInstanceValid(scrollContainer))
                MaintainModsScrollbar(screen, scrollContainer);
        }).CallDeferred();
    }

    private static void MaintainModsScrollbar(NModdingScreen screen, NScrollableContainer scrollContainer)
    {
        ClampModsScrollIfContentFits(screen, scrollContainer);
        ForceModsScrollbarVisible(scrollContainer);
    }

    private static void ClampModsScrollIfContentFits(NModdingScreen screen, NScrollableContainer scrollContainer)
    {
        var content = ModdingScreenNodeOps.GetModRowContainer(screen);
        var viewport = content?.GetParentOrNull<Control>();
        if (content == null || viewport == null)
            return;

        content.UpdateMinimumSize();
        if (content.GetCombinedMinimumSize().Y > viewport.Size.Y + ModdingScreenConstants.ScrollFitTolerance)
            return;

        scrollContainer.DisableScrollingIfContentFits();
        scrollContainer.InstantlyScrollToTop();
    }

    private static void ForceModsScrollbarVisible(NScrollableContainer scrollContainer)
    {
        if (scrollContainer.Scrollbar == null)
            return;

        scrollContainer.Scrollbar.CustomMinimumSize = new Vector2(ModdingScreenConstants.GroupHeaderScrollbarReserveWidth, 0);
        scrollContainer.Scrollbar.Visible = true;
        scrollContainer.Scrollbar.Show();
        scrollContainer.Scrollbar.MouseFilter = Control.MouseFilterEnum.Stop;
        scrollContainer.Scrollbar.MoveToFront();
    }

    private static void LayoutTopBar(
        TopBarControls topBarControls,
        Control? titleNode,
        Control? scrollContainer,
        Vector2 screenOffset)
    {
        var topBar = topBarControls.Bar;
        float x = ModdingScreenConstants.TopBarFallbackX;
        float y = ModdingScreenConstants.TopBarFallbackY;
        float width = ModdingScreenConstants.TopBarFallbackWidth;
        float height = topBar.GetCombinedMinimumSize().Y;

        if (titleNode != null && scrollContainer != null)
        {
            x = titleNode.GlobalPosition.X - screenOffset.X + titleNode.Size.X + ModdingScreenConstants.TopBarGap;
            y = titleNode.GlobalPosition.Y - screenOffset.Y;
            float leftPanelRight = scrollContainer.GlobalPosition.X - screenOffset.X + scrollContainer.Size.X;
            width = Math.Max(ModdingScreenConstants.TopBarFallbackWidth, leftPanelRight - x - ModdingScreenConstants.TopBarTrailingPadding);
            height = Math.Max(height, titleNode.Size.Y);
        }

        topBarControls.SetCompact(width < ModdingScreenConstants.TopBarCompactThreshold);
        topBar.Position = new Vector2(x, y);
        topBar.Size = new Vector2(width, height);
    }

    private static float GetGroupBarHeight(Control? scrollContainer)
    {
        float width = scrollContainer?.Size.X ?? ModdingScreenConstants.GroupBarFallbackWidth;
        return width < ModdingScreenConstants.GroupBarCompactThreshold
            ? ModdingScreenConstants.GroupBarCompactHeight
            : ModdingScreenConstants.GroupBarWideHeight;
    }

    private static void ReserveModListHeaderSpace(ModdingScreenSession session, Control scrollContainer, float groupBarHeight)
    {
        if (!session.OriginalModsScrollPosition.HasValue)
            session.OriginalModsScrollPosition = scrollContainer.Position;

        if (!session.OriginalModsScrollSize.HasValue)
            session.OriginalModsScrollSize = scrollContainer.Size;

        Vector2 originalPosition = session.OriginalModsScrollPosition.Value;
        Vector2 originalSize = session.OriginalModsScrollSize.Value;
        float reservedHeight = groupBarHeight + ModdingScreenConstants.GroupBarListGap;
        scrollContainer.Position = new Vector2(originalPosition.X, originalPosition.Y + reservedHeight);
        scrollContainer.Size = new Vector2(originalSize.X, Math.Max(120f, originalSize.Y - reservedHeight));
    }

    private static void LayoutGroupBar(GroupBarControls groupBarControls, Control? scrollContainer, Vector2 screenOffset, float groupBarHeight)
    {
        var groupBar = groupBarControls.Bar;
        float x = ModdingScreenConstants.GroupBarFallbackX;
        float y = ModdingScreenConstants.GroupBarFallbackY;
        float width = ModdingScreenConstants.GroupBarFallbackWidth;

        if (scrollContainer != null)
        {
            x = scrollContainer.GlobalPosition.X - screenOffset.X;
            y = scrollContainer.GlobalPosition.Y - screenOffset.Y -
                groupBarHeight -
                ModdingScreenConstants.GroupBarListGap;
            width = scrollContainer.Size.X;
        }

        bool isCompact = groupBarHeight > ModdingScreenConstants.GroupBarWideHeight;
        groupBarControls.SetCompact(isCompact);
        groupBar.Position = new Vector2(x, y);
        groupBar.Size = new Vector2(width, groupBarHeight);
    }

}
