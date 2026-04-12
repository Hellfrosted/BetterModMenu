using System;
using Godot;
using BetterModMenu.Data;
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
        Func<string, bool> onAddGroupRequested)
    {
        ApplyScrollMaskClip(screen);
        EnsureChromeRoot(screen, session);
        EnsureLayoutSignals(screen, session);
        EnsureTopBar(session, onProfileSelected, onNewProfilePressed, onRenameProfilePressed, onDeleteProfilePressed);
        EnsureGroupBar(session, onPortableModeToggled, onAddGroupRequested);
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

        UpdateLayout(screen, session);
    }

    public static void UpdateLayout(NModdingScreen screen, ModdingScreenSession session)
    {
        var chromeRoot = session.ChromeRoot;
        if (chromeRoot == null || !GodotObject.IsInstanceValid(chromeRoot))
            return;

        chromeRoot.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);

        var titleNode = screen.GetNodeOrNull<Control>("%InstalledModsTitle");
        var scrollContainer = screen.GetNodeOrNull<Control>("%ModsScrollContainer");
        var modInfoPanel = screen.GetNodeOrNull<Control>("%ModInfoContainer");
        Vector2 screenOffset = screen.GlobalPosition;

        if (session.TopBarControls != null && GodotObject.IsInstanceValid(session.TopBarControls.Bar))
            LayoutTopBar(session.TopBarControls, titleNode, scrollContainer, screenOffset);

        if (session.GroupBarControls != null && GodotObject.IsInstanceValid(session.GroupBarControls.Bar))
            LayoutGroupBar(session.GroupBarControls, modInfoPanel, screenOffset);
    }

    private static void ApplyScrollMaskClip(NModdingScreen screen)
    {
        var scrollContainer = screen.GetNodeOrNull<Control>("%ModsScrollContainer");
        if (scrollContainer == null)
            return;

        var mask = scrollContainer.GetNodeOrNull<Control>("Mask");
        if (mask != null)
            mask.ClipContents = true;
    }

    private static void EnsureChromeRoot(NModdingScreen screen, ModdingScreenSession session)
    {
        if (session.ChromeRoot != null && GodotObject.IsInstanceValid(session.ChromeRoot) && session.ChromeRoot.GetParent() == screen)
            return;

        var chromeRoot = new Control { Name = ModdingScreenConstants.ChromeRootName };
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

    private static void LayoutGroupBar(GroupBarControls groupBarControls, Control? modInfoPanel, Vector2 screenOffset)
    {
        var groupBar = groupBarControls.Bar;
        float x = ModdingScreenConstants.GroupBarFallbackX;
        float y = ModdingScreenConstants.GroupBarFallbackY;
        float width = ModdingScreenConstants.GroupBarFallbackWidth;

        if (modInfoPanel != null)
        {
            x = modInfoPanel.GlobalPosition.X - screenOffset.X;
            y = modInfoPanel.GlobalPosition.Y - screenOffset.Y - ModdingScreenConstants.GroupBarYOffset;
            width = modInfoPanel.Size.X;
        }

        bool isCompact = width < ModdingScreenConstants.GroupBarCompactThreshold;
        groupBarControls.SetCompact(isCompact);
        groupBar.Position = new Vector2(x, y);
        groupBar.Size = new Vector2(width, isCompact ? ModdingScreenConstants.GroupBarCompactHeight : ModdingScreenConstants.GroupBarWideHeight);
    }
}
