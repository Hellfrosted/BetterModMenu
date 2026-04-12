using HarmonyLib;
using Godot;
using MegaCrit.Sts2.Core.Nodes.Screens.ModdingScreen;
using BetterModMenu.Data;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using MegaCrit.Sts2.Core.Saves;

namespace BetterModMenu.Patches;

[HarmonyPatch(typeof(NModdingScreen))]
public static class NModdingScreenPatch
{
    private static WeakReference<NModdingScreen>? _currentScreenRef;
    private static readonly ConditionalWeakTable<NModdingScreen, ModdingScreenSession> Sessions = new();

    [HarmonyPatch(nameof(NModdingScreen.OnModEnabledOrDisabled))]
    [HarmonyPostfix]
    public static void Postfix_OnModEnabledOrDisabled(NModdingScreen __instance)
    {
        if (!IsAutoSaveSuppressed(__instance))
            ProfileManager.SnapshotCurrentStateAndSave();
    }

    [HarmonyPatch(nameof(NModdingScreen._Ready))]
    [HarmonyPostfix]
    public static void Postfix_Ready(NModdingScreen __instance)
    {
        _currentScreenRef = new(__instance);
        var session = GetSession(__instance);

        // Clip only the Mask so the ScrollContainer's scrollbar remains visible
        var scrollContainer = __instance.GetNodeOrNull<Control>("%ModsScrollContainer");
        if (scrollContainer != null)
        {
            var mask = scrollContainer.GetNodeOrNull<Control>("Mask");
            if (mask != null)
                mask.ClipContents = true;
        }

        EnsureChromeRoot(__instance, session);

        if (!session.LayoutSignalsConnected)
            ConnectLayoutSignals(__instance, session);

        if (session.TopBarControls == null || !GodotObject.IsInstanceValid(session.TopBarControls.Bar) || session.TopBarControls.Bar.GetParent() != session.ChromeRoot)
            BuildTopBar(session);

        if (session.GroupBarControls == null || !GodotObject.IsInstanceValid(session.GroupBarControls.Bar) || session.GroupBarControls.Bar.GetParent() != session.ChromeRoot)
            BuildGroupBar(session);

        RefreshProfileDropdown();
        RefreshGroupsUI();
        UpdateChromeLayout(__instance);
        Callable.From(() =>
        {
            if (IsCurrentScreen(__instance))
            {
                RefreshGroupsUI();
                UpdateChromeLayout(__instance);
            }
        }).CallDeferred();
    }

    [HarmonyPatch(nameof(NModdingScreen._ExitTree))]
    [HarmonyPostfix]
    public static void Postfix_ExitTree(NModdingScreen __instance)
    {
        if (IsCurrentScreen(__instance))
            _currentScreenRef = null;

        var session = GetSession(__instance);
        session.AutoSaveSuppressionDepth = 0;
        session.TickboxSuppressionDepth = 0;
        session.GeneratedGroupNodes.Clear();
        session.ChromeRoot = null;
        session.GroupBarControls = null;
        session.LayoutSignalsConnected = false;
        session.TopBarControls = null;
    }

    public static bool IsCurrentScreen(NModdingScreen? screen)
    {
        return screen != null &&
            _currentScreenRef?.TryGetTarget(out var current) == true &&
            current == screen &&
            GodotObject.IsInstanceValid(screen);
    }

    internal static bool IsTickboxHandlerSuppressed(NModdingScreen screen)
    {
        return GetSession(screen).TickboxSuppressionDepth > 0;
    }

    private static bool IsAutoSaveSuppressed(NModdingScreen screen)
    {
        return GetSession(screen).AutoSaveSuppressionDepth > 0;
    }

    private static ModdingScreenSession GetSession(NModdingScreen screen)
    {
        return Sessions.GetOrCreateValue(screen);
    }

    private static bool TryGetCurrentScreen(out NModdingScreen? screen)
    {
        if (_currentScreenRef?.TryGetTarget(out screen) == true && GodotObject.IsInstanceValid(screen))
            return true;

        screen = null;
        return false;
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

    private static void BuildTopBar(ModdingScreenSession session)
    {
        if (session.ChromeRoot == null)
            return;

        var builtTopBar = ModdingScreenBars.CreateTopBar(
            OnProfileSelected,
            OnNewProfilePressed,
            OnRenameProfilePressed,
            OnDelProfilePressed);

        session.TopBarControls = builtTopBar;
        session.ChromeRoot.AddChild(builtTopBar.Bar);
    }

    private static void BuildGroupBar(ModdingScreenSession session)
    {
        if (session.ChromeRoot == null)
            return;

        bool portableModeEnabled = ProfileManager.TryGetPortableConfigPath(out string portableConfigPath) &&
            System.IO.File.Exists(portableConfigPath);
        var builtGroupBar = ModdingScreenBars.CreateGroupBar(
            portableModeEnabled,
            OnPortableModeToggled,
            OnAddGroupRequested);
        session.GroupBarControls = builtGroupBar;
        session.ChromeRoot.AddChild(builtGroupBar.Bar);
    }

    private static void ConnectLayoutSignals(NModdingScreen screen, ModdingScreenSession session)
    {
        session.LayoutSignalsConnected = true;
        screen.Resized += () =>
        {
            if (IsCurrentScreen(screen))
                UpdateChromeLayout(screen);
        };

        foreach (string path in new[] { "%InstalledModsTitle", "%ModsScrollContainer", "%ModInfoContainer" })
        {
            var control = screen.GetNodeOrNull<Control>(path);
            if (control == null)
                continue;

            control.Resized += () =>
            {
                if (IsCurrentScreen(screen))
                    UpdateChromeLayout(screen);
            };
        }
    }

    private static void UpdateChromeLayout(NModdingScreen screen)
    {
        var session = GetSession(screen);
        var chromeRoot = session.ChromeRoot;
        if (chromeRoot == null || !GodotObject.IsInstanceValid(chromeRoot))
            return;

        chromeRoot.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);

        var titleNode = screen.GetNodeOrNull<Control>("%InstalledModsTitle");
        var scrollContainer = screen.GetNodeOrNull<Control>("%ModsScrollContainer");
        var modInfoPanel = screen.GetNodeOrNull<Control>("%ModInfoContainer");
        Vector2 screenOffset = screen.GlobalPosition;

        if (session.TopBarControls != null && GodotObject.IsInstanceValid(session.TopBarControls.Bar))
        {
            var topBar = session.TopBarControls.Bar;
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

            session.TopBarControls.SetCompact(width < ModdingScreenConstants.TopBarCompactThreshold);
            topBar.Position = new Vector2(x, y);
            topBar.Size = new Vector2(width, height);
        }

        if (session.GroupBarControls != null && GodotObject.IsInstanceValid(session.GroupBarControls.Bar))
        {
            var groupBar = session.GroupBarControls.Bar;
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
            session.GroupBarControls.SetCompact(isCompact);
            groupBar.Position = new Vector2(x, y);
            groupBar.Size = new Vector2(width, isCompact ? ModdingScreenConstants.GroupBarCompactHeight : ModdingScreenConstants.GroupBarWideHeight);
        }
    }

    private static void OnPortableModeToggled(bool isToggled)
    {
        try
        {
            if (!ModdingScreenStateOps.SetPortableMode(isToggled))
                ProfileManager.ModLogger.Error("Portable mode state changed in the UI but could not be persisted.");
        }
        catch (System.Exception ex)
        {
            string action = isToggled ? "enable" : "disable";
            ProfileManager.ModLogger.Error($"Failed to {action} portable mode: {ex}");
        }
    }

    private static bool OnAddGroupRequested(string groupName)
    {
        if (!ModdingScreenStateOps.TryAddGroup(groupName))
            return false;

        RefreshGroupsUI();
        return true;
    }

    public static void RefreshGroupsUI()
    {
        if (!TryGetCurrentScreen(out var screen) || screen == null)
            return;

        var modRowContainer = ModdingScreenNodeOps.GetModRowContainer(screen);
        if (modRowContainer == null)
            return;

        ModdingScreenGroupUi.RefreshGroupsUI(modRowContainer, GetSession(screen).GeneratedGroupNodes, RefreshGroupsUI, RenameGroup, MoveGroup, ToggleAllInGroup);
        UpdateChromeLayout(screen);
    }

    private static void MoveGroup(string grpName, int direction)
    {
        if (ModdingScreenStateOps.TryMoveGroup(grpName, direction))
            RefreshGroupsUI();
    }

    private static void RenameGroup(string oldName)
    {
        if (!TryGetCurrentScreen(out var screen) || screen == null)
            return;

        var popup = new AcceptDialog();
        popup.Title = "Rename Group";
        popup.DialogText = "";

        var input = new LineEdit
        {
            Text = oldName,
            CustomMinimumSize = new Vector2(250, 0)
        };
        popup.AddChild(input);

        popup.Confirmed += () =>
        {
            if (ModdingScreenStateOps.TryRenameGroup(oldName, input.Text))
                RefreshGroupsUI();
        };

        screen.AddChild(popup);
        popup.PopupCentered(new Vector2I(300, 100));
    }

    public static void MoveModOrder(string modId, int direction)
    {
        if (ModdingScreenListOps.TryMoveModOrder(modId, direction))
            RefreshGroupsUI();
    }

    private static void ToggleAllInGroup(string groupName, bool isToggled)
    {
        if (!TryGetCurrentScreen(out var screen) || screen == null)
            return;

        var modRowContainer = ModdingScreenNodeOps.GetModRowContainer(screen);
        if (modRowContainer == null)
            return;

        using var autoSaveSuppression = new ScreenSuppressionScope(screen, suppressAutoSave: true, suppressTickboxes: false);
        using var tickboxSuppression = new ScreenSuppressionScope(screen, suppressAutoSave: false, suppressTickboxes: true);
        if (ModdingScreenListOps.ApplyToggleAllInGroup(modRowContainer, groupName, isToggled))
        {
            ProfileManager.SaveInMemoryState();
            SaveManager.Instance?.SaveSettings();
            screen.OnModEnabledOrDisabled();
        }
    }

    private static void RefreshProfileDropdown()
    {
        if (!TryGetCurrentScreen(out var screen) || screen == null)
            return;

        var topBarControls = GetSession(screen).TopBarControls;
        if (topBarControls == null || !GodotObject.IsInstanceValid(topBarControls.ProfileDropdown))
            return;

        ProfileManager.NormalizeProfileIndex();
        topBarControls.ProfileDropdown.Clear();
        for (int i = 0; i < ProfileManager.Profiles.Count; i++)
            topBarControls.ProfileDropdown.AddItem(ProfileManager.Profiles[i].Name, i);

        topBarControls.ProfileDropdown.Select(ProfileManager.CurrentProfileIndex);
        UpdateChromeLayout(screen);
    }

    private static void OnProfileSelected(long index)
    {
        ApplyProfileSelection((int)index, snapshotCurrentProfile: true);
    }

    private static void ApplyProfileSelection(int index, bool snapshotCurrentProfile)
    {
        if (!ModdingScreenProfileOps.TryApplyProfileSelection(index, snapshotCurrentProfile, out var profile) || profile == null)
            return;

        RefreshProfileDropdown();

        if (TryGetCurrentScreen(out var screen) && screen != null)
        {
            using var autoSaveSuppression = new ScreenSuppressionScope(screen, suppressAutoSave: true, suppressTickboxes: false);
            using var tickboxSuppression = new ScreenSuppressionScope(screen, suppressAutoSave: false, suppressTickboxes: true);
            screen.OnModEnabledOrDisabled();

            var modRowContainer = ModdingScreenNodeOps.GetModRowContainer(screen);
            if (modRowContainer != null)
            {
                ModdingScreenProfileOps.SyncTickboxesForProfile(modRowContainer, profile);
                RefreshGroupsUI();
            }
        }
    }

    private static void OnNewProfilePressed()
    {
        ModdingScreenProfileOps.CreateNewProfileFromCurrentState();
        RefreshProfileDropdown();
        RefreshGroupsUI();
    }

    private static void OnRenameProfilePressed()
    {
        if (!TryGetCurrentScreen(out var screen) || screen == null)
            return;

        var popup = new AcceptDialog();
        popup.Title = "Rename Profile";
        popup.DialogText = "";

        var input = new LineEdit
        {
            Text = ProfileManager.CurrentProfile.Name,
            CustomMinimumSize = new Vector2(250, 0)
        };
        popup.AddChild(input);

        popup.Confirmed += () =>
        {
            if (ModdingScreenProfileOps.TryRenameCurrentProfile(input.Text))
            {
                RefreshProfileDropdown();
            }
        };

        screen.AddChild(popup);
        popup.PopupCentered(new Vector2I(300, 100));
    }

    private static void OnDelProfilePressed()
    {
        int? replacementIndex = ModdingScreenProfileOps.DeleteCurrentProfile();
        if (replacementIndex.HasValue)
            ApplyProfileSelection(replacementIndex.Value, snapshotCurrentProfile: false);
    }

    private sealed class ScreenSuppressionScope : System.IDisposable
    {
        private readonly NModdingScreen _screen;
        private readonly bool _suppressAutoSave;
        private readonly bool _suppressTickboxes;

        public ScreenSuppressionScope(NModdingScreen screen, bool suppressAutoSave, bool suppressTickboxes)
        {
            _screen = screen;
            _suppressAutoSave = suppressAutoSave;
            _suppressTickboxes = suppressTickboxes;

            var session = GetSession(screen);
            if (_suppressAutoSave)
                session.AutoSaveSuppressionDepth++;
            if (_suppressTickboxes)
                session.TickboxSuppressionDepth++;
        }

        public void Dispose()
        {
            var session = GetSession(_screen);
            if (_suppressTickboxes && session.TickboxSuppressionDepth > 0)
                session.TickboxSuppressionDepth--;
            if (_suppressAutoSave && session.AutoSaveSuppressionDepth > 0)
                session.AutoSaveSuppressionDepth--;
        }
    }
}
