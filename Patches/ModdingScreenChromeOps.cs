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
        Action onStyleEditorPressed,
        Action onTutorialPressed,
        Action onCloudBackupPressed,
        Action<string> onSearchChanged,
        Func<string, bool> onAddGroupRequested)
    {
        ApplyScrollMaskClip(screen);
        EnsureChromeRoot(screen, session);
        EnsureLayoutSignals(screen, session);
        EnsureScrollbarPersistenceSignals(screen, session);
        EnsureTopBar(session, onProfileSelected, onNewProfilePressed, onRenameProfilePressed, onDeleteProfilePressed);
        EnsureGroupBar(session, onPortableModeToggled, onManualBackupPressed, onLoadBackupPressed, onExportModListPressed, onViewLogsPressed, onStyleEditorPressed, onTutorialPressed, onCloudBackupPressed, onSearchChanged, onAddGroupRequested);
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
            session.SearchQuery,
            session.SearchResults,
            refreshGroupsUI,
            renameGroup,
            moveGroup,
            toggleAllInGroup);

        ApplySearchSelectionRule(screen, session, modRowContainer);
        UpdateSearchStatus(session);
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

        PrepareInstalledModsTitle(titleNode);

        float groupBarHeight = GetGroupBarHeight(scrollContainer);
        float searchBarHeight = GetSearchBarHeight(scrollContainer);
        bool stackTopBar = session.TopBarControls != null &&
            GodotObject.IsInstanceValid(session.TopBarControls.Bar) &&
            ShouldStackTopBar(session.TopBarControls, titleNode, scrollContainer, screenOffset);
        float stackedTopBarHeight = stackTopBar
            ? session.TopBarControls!.Bar.GetCombinedMinimumSize().Y + ModdingScreenConstants.TopBarStackGap
            : 0f;

        if (scrollContainer != null)
            ReserveModListChromeSpace(session, scrollContainer, groupBarHeight, searchBarHeight, stackedTopBarHeight);

        if (session.TopBarControls != null && GodotObject.IsInstanceValid(session.TopBarControls.Bar))
            LayoutTopBar(session.TopBarControls, titleNode, scrollContainer, screenOffset, groupBarHeight, stackTopBar);

        if (session.GroupBarControls != null && GodotObject.IsInstanceValid(session.GroupBarControls.Bar))
        {
            LayoutGroupBar(session.GroupBarControls, scrollContainer, screenOffset, groupBarHeight);
            LayoutSearchBar(session.GroupBarControls, scrollContainer, screenOffset, searchBarHeight);
        }

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
        Action onStyleEditorPressed,
        Action onTutorialPressed,
        Action onCloudBackupPressed,
        Action<string> onSearchChanged,
        Func<string, bool> onAddGroupRequested)
    {
        if (session.ChromeRoot == null)
            return;

        if (session.GroupBarControls != null &&
            GodotObject.IsInstanceValid(session.GroupBarControls.Bar) &&
            GodotObject.IsInstanceValid(session.GroupBarControls.SearchBar) &&
            session.GroupBarControls.Bar.GetParent() == session.ChromeRoot &&
            session.GroupBarControls.SearchBar.GetParent() == session.ChromeRoot)
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
            onStyleEditorPressed,
            onTutorialPressed,
            onCloudBackupPressed,
            onSearchChanged,
            onAddGroupRequested);

        session.GroupBarControls = builtGroupBar;
        session.ChromeRoot.AddChild(builtGroupBar.Bar);
        session.ChromeRoot.AddChild(builtGroupBar.SearchBar);
    }

    private static void ApplySearchSelectionRule(NModdingScreen screen, ModdingScreenSession session, Control modRowContainer)
    {
        if (string.IsNullOrWhiteSpace(session.SearchQuery))
        {
            ModdingScreenInfoPanelOps.Refresh(screen, session);
            return;
        }

        string selectedModId = ModSearchRules.PickSelectedModId(session.SelectedModId, session.SearchResults.Values.ToList());
        if (string.IsNullOrWhiteSpace(selectedModId))
        {
            session.SelectedModId = string.Empty;
            ModdingScreenInfoPanelOps.Refresh(screen, session);
            return;
        }

        if (string.Equals(session.SelectedModId, selectedModId, StringComparison.OrdinalIgnoreCase))
        {
            ModdingScreenInfoPanelOps.Refresh(screen, session);
            return;
        }

        var row = modRowContainer.GetChildren()
            .OfType<NModMenuRow>()
            .FirstOrDefault(candidate => string.Equals(candidate.Mod?.manifest?.id, selectedModId, StringComparison.OrdinalIgnoreCase));
        if (row == null)
        {
            ModdingScreenInfoPanelOps.Refresh(screen, session);
            return;
        }

        session.SelectedModId = selectedModId;
        screen.OnRowSelected(row);
        ModdingScreenInfoPanelOps.Refresh(screen, session);
    }

    private static void UpdateSearchStatus(ModdingScreenSession session)
    {
        var controls = session.GroupBarControls;
        if (controls == null || !GodotObject.IsInstanceValid(controls.SearchResultLabel))
            return;

        controls.SearchResultLabel.Text = string.IsNullOrWhiteSpace(session.SearchQuery)
            ? string.Empty
            : ModdingScreenText.Format(BmmText.SearchResultFoundFormat, "{0} found", session.SearchResults.Count);
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
        if (ClampModsScrollIfContentFits(screen, scrollContainer))
            return;

        ForceModsScrollbarVisible(screen, scrollContainer);
    }

    private static bool ClampModsScrollIfContentFits(NModdingScreen screen, NScrollableContainer scrollContainer)
    {
        var content = ModdingScreenNodeOps.GetModRowContainer(screen);
        var viewport = content?.GetParentOrNull<Control>();
        if (content == null || viewport == null)
            return false;

        content.UpdateMinimumSize();
        if (content.GetCombinedMinimumSize().Y > viewport.Size.Y + ModdingScreenConstants.ScrollFitTolerance)
            return false;

        scrollContainer.DisableScrollingIfContentFits();
        scrollContainer.InstantlyScrollToTop();
        return true;
    }

    private static void ForceModsScrollbarVisible(NModdingScreen screen, NScrollableContainer scrollContainer)
    {
        if (scrollContainer.Scrollbar == null)
            return;

        float scrollbarWidth = ModdingScreenConstants.ModsScrollbarLaneWidth;
        float scrollbarX = GetCenteredModsScrollbarX(screen, scrollContainer, scrollbarWidth);
        scrollContainer.Scrollbar.CustomMinimumSize = new Vector2(scrollbarWidth, 0);
        scrollContainer.Scrollbar.Size = new Vector2(scrollbarWidth, scrollContainer.Scrollbar.Size.Y);
        scrollContainer.Scrollbar.Position = new Vector2(scrollbarX, scrollContainer.Scrollbar.Position.Y);
        scrollContainer.Scrollbar.Visible = true;
        scrollContainer.Scrollbar.Show();
        scrollContainer.Scrollbar.MouseFilter = Control.MouseFilterEnum.Stop;
        scrollContainer.Scrollbar.MoveToFront();
    }

    private static float GetCenteredModsScrollbarX(NModdingScreen screen, NScrollableContainer scrollContainer, float scrollbarWidth)
    {
        var infoPanel = screen.GetNodeOrNull<Control>("%ModInfoContainer");
        if (infoPanel == null)
            return scrollContainer.Size.X + ModdingScreenConstants.ModsScrollbarOutsideGap;

        float scrollRight = scrollContainer.GlobalPosition.X + scrollContainer.Size.X;
        float availableGap = infoPanel.GlobalPosition.X - scrollRight;
        if (availableGap <= scrollbarWidth)
            return scrollContainer.Size.X;

        return scrollContainer.Size.X +
            ((availableGap - scrollbarWidth) / 2f) +
            ModdingScreenConstants.ModsScrollbarOpticalCenterOffset;
    }

    private static void LayoutTopBar(
        TopBarControls topBarControls,
        Control? titleNode,
        Control? scrollContainer,
        Vector2 screenOffset,
        float groupBarHeight,
        bool stackTopBar)
    {
        var topBar = topBarControls.Bar;
        float x = ModdingScreenConstants.TopBarFallbackX;
        float y = ModdingScreenConstants.TopBarFallbackY;
        float width = ModdingScreenConstants.TopBarFallbackWidth;
        float height = topBar.GetCombinedMinimumSize().Y;

        if (titleNode != null && scrollContainer != null)
        {
            float leftPanelRight = scrollContainer.GlobalPosition.X - screenOffset.X + scrollContainer.Size.X;
            if (stackTopBar)
            {
                x = scrollContainer.GlobalPosition.X - screenOffset.X;
                y = scrollContainer.GlobalPosition.Y - screenOffset.Y -
                    groupBarHeight -
                    ModdingScreenConstants.GroupBarListGap -
                    height -
                    ModdingScreenConstants.TopBarStackGap;
                width = scrollContainer.Size.X;
            }
            else
            {
                x = titleNode.GlobalPosition.X - screenOffset.X + titleNode.Size.X + ModdingScreenConstants.TopBarGap;
                y = titleNode.GlobalPosition.Y - screenOffset.Y;
                width = Math.Max(0f, leftPanelRight - x - ModdingScreenConstants.TopBarTrailingPadding);
                height = Math.Max(height, titleNode.Size.Y);
            }
        }

        topBarControls.SetCompact(width < ModdingScreenConstants.TopBarCompactThreshold);
        topBar.Position = new Vector2(x, y);
        topBar.Size = new Vector2(width, height);
    }

    private static bool ShouldStackTopBar(TopBarControls topBarControls, Control? titleNode, Control? scrollContainer, Vector2 screenOffset)
    {
        if (titleNode == null || scrollContainer == null)
            return false;

        float titleRight = titleNode.GlobalPosition.X - screenOffset.X + titleNode.Size.X;
        float leftPanelRight = scrollContainer.GlobalPosition.X - screenOffset.X + scrollContainer.Size.X;
        float availableInlineWidth = leftPanelRight - titleRight - ModdingScreenConstants.TopBarGap - ModdingScreenConstants.TopBarTrailingPadding;
        topBarControls.SetCompact(true);
        return ModdingScreenLayoutRules.ShouldStackTopBar(availableInlineWidth, topBarControls.Bar.GetCombinedMinimumSize().X);
    }

    private static void PrepareInstalledModsTitle(Control? titleNode)
    {
        if (titleNode is not Label label)
            return;

        label.AutowrapMode = TextServer.AutowrapMode.Off;
        label.TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis;
        label.ClipText = true;
    }

    private static float GetGroupBarHeight(Control? scrollContainer)
    {
        float width = scrollContainer?.Size.X ?? ModdingScreenConstants.GroupBarFallbackWidth;
        return width < ModdingScreenConstants.GroupBarCompactThreshold
            ? ModdingScreenConstants.GroupBarCompactHeight
            : ModdingScreenConstants.GroupBarWideHeight;
    }

    private static float GetSearchBarHeight(Control? scrollContainer)
    {
        return scrollContainer == null ? 0f : ModdingScreenConstants.SearchBarHeight;
    }

    private static void ReserveModListChromeSpace(
        ModdingScreenSession session,
        Control scrollContainer,
        float groupBarHeight,
        float searchBarHeight,
        float stackedTopBarHeight)
    {
        if (!session.OriginalModsScrollPosition.HasValue)
            session.OriginalModsScrollPosition = scrollContainer.Position;

        if (!session.OriginalModsScrollSize.HasValue)
            session.OriginalModsScrollSize = scrollContainer.Size;

        Vector2 originalPosition = session.OriginalModsScrollPosition.Value;
        Vector2 originalSize = session.OriginalModsScrollSize.Value;
        float reservedTopHeight = groupBarHeight + ModdingScreenConstants.GroupBarListGap + stackedTopBarHeight;
        float reservedBottomHeight = searchBarHeight + ModdingScreenConstants.SearchBarListGap;
        scrollContainer.Position = new Vector2(originalPosition.X, originalPosition.Y + reservedTopHeight);
        scrollContainer.Size = new Vector2(originalSize.X, Math.Max(120f, originalSize.Y - reservedTopHeight - reservedBottomHeight));
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

    private static void LayoutSearchBar(
        GroupBarControls groupBarControls,
        Control? scrollContainer,
        Vector2 screenOffset,
        float searchBarHeight)
    {
        var searchBar = groupBarControls.SearchBar;
        float x = ModdingScreenConstants.GroupBarFallbackX;
        float y = ModdingScreenConstants.GroupBarFallbackY;
        float width = ModdingScreenConstants.GroupBarFallbackWidth;

        if (scrollContainer != null)
        {
            x = scrollContainer.GlobalPosition.X - screenOffset.X;
            y = scrollContainer.GlobalPosition.Y - screenOffset.Y +
                scrollContainer.Size.Y +
                ModdingScreenConstants.SearchBarListGap;
            width = scrollContainer.Size.X;
        }

        searchBar.Position = new Vector2(x, y);
        searchBar.Size = new Vector2(width, searchBarHeight);
    }

}
