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

        if (session.TopBar == null || !GodotObject.IsInstanceValid(session.TopBar) || session.TopBar.GetParent() != __instance)
        {
            var titleNode = __instance.GetNodeOrNull<Control>("%InstalledModsTitle");
            var modInfoPanel = __instance.GetNodeOrNull<Control>("%ModInfoContainer");

            BuildTopBar(__instance, session, titleNode, scrollContainer);
            BuildGroupBar(__instance, session, modInfoPanel);
        }

        RefreshProfileDropdown();
        RefreshGroupsUI();
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
        session.GroupBar = null;
        session.ProfileDropdown = null;
        session.TopBar = null;
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

    private static void BuildTopBar(NModdingScreen screen, ModdingScreenSession session, Control? titleNode, Control? scrollContainer)
    {
        var builtTopBar = ModdingScreenBars.CreateTopBar(
            titleNode,
            scrollContainer,
            OnProfileSelected,
            OnNewProfilePressed,
            OnRenameProfilePressed,
            OnDelProfilePressed);

        session.TopBar = builtTopBar.Bar;
        session.ProfileDropdown = builtTopBar.ProfileDropdown;
        screen.AddChild(session.TopBar);
    }

    private static void BuildGroupBar(NModdingScreen screen, ModdingScreenSession session, Control? modInfoPanel)
    {
        session.GroupBar = ModdingScreenBars.CreateGroupBar(
            modInfoPanel,
            System.IO.File.Exists(ProfileManager.PortableConfigPath),
            OnPortableModeToggled,
            OnAddGroupRequested);
        screen.AddChild(session.GroupBar);
    }

    private static void OnPortableModeToggled(bool isToggled)
    {
        try
        {
            ModdingScreenStateOps.SetPortableMode(isToggled);
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

        var profileDropdown = GetSession(screen).ProfileDropdown;
        if (profileDropdown == null || !GodotObject.IsInstanceValid(profileDropdown))
            return;

        ProfileManager.NormalizeProfileIndex();
        profileDropdown.Clear();
        for (int i = 0; i < ProfileManager.Profiles.Count; i++)
            profileDropdown.AddItem(ProfileManager.Profiles[i].Name, i);

        profileDropdown.Select(ProfileManager.CurrentProfileIndex);
    }

    private static void OnProfileSelected(long index)
    {
        ApplyProfileSelection((int)index, snapshotCurrentProfile: true);
    }

    private static void ApplyProfileSelection(int index, bool snapshotCurrentProfile)
    {
        var profile = ModdingScreenProfileOps.ApplyProfileSelection(index, snapshotCurrentProfile);
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
