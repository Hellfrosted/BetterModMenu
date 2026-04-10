using HarmonyLib;
using Godot;
using MegaCrit.Sts2.Core.Nodes.Screens.ModdingScreen;
using BetterModMenu.Data;
using System.Collections.Generic;
using MegaCrit.Sts2.Core.Saves;

namespace BetterModMenu.Patches;

[HarmonyPatch(typeof(NModdingScreen))]
public static class NModdingScreenPatch
{
    private static NModdingScreen? _currentScreen;
    private static HBoxContainer? _topBar;
    private static OptionButton? _profileDropdown;
    private static HBoxContainer? _groupBar;
    private static bool _suppressAutoSave = false;
    private static readonly List<Node> _generatedGroupNodes = new();

    [HarmonyPatch(nameof(NModdingScreen.OnModEnabledOrDisabled))]
    [HarmonyPostfix]
    public static void Postfix_OnModEnabledOrDisabled()
    {
        if (!_suppressAutoSave)
            ProfileManager.SnapshotCurrentStateAndSave();
    }

    [HarmonyPatch(nameof(NModdingScreen._Ready))]
    [HarmonyPostfix]
    public static void Postfix_Ready(NModdingScreen __instance)
    {
        _currentScreen = __instance;

        // Clip only the Mask so the ScrollContainer's scrollbar remains visible
        var scrollContainer = __instance.GetNodeOrNull<Control>("%ModsScrollContainer");
        if (scrollContainer != null)
        {
            var mask = scrollContainer.GetNodeOrNull<Control>("Mask");
            if (mask != null)
                mask.ClipContents = true;
        }

        if (_topBar == null || !GodotObject.IsInstanceValid(_topBar))
        {
            var titleNode = __instance.GetNodeOrNull<Control>("%InstalledModsTitle");
            var modInfoPanel = __instance.GetNodeOrNull<Control>("%ModInfoContainer");

            BuildTopBar(__instance, titleNode, scrollContainer);
            BuildGroupBar(__instance, modInfoPanel);
        }

        RefreshProfileDropdown();
        RefreshGroupsUI();
    }

    private static void BuildTopBar(NModdingScreen __instance, Control? titleNode, Control? scrollContainer)
    {
        var builtTopBar = ModdingScreenBars.CreateTopBar(
            titleNode,
            scrollContainer,
            OnProfileSelected,
            OnNewProfilePressed,
            OnRenameProfilePressed,
            OnDelProfilePressed);

        _topBar = builtTopBar.Bar;
        _profileDropdown = builtTopBar.ProfileDropdown;
        __instance.AddChild(_topBar);
    }

    private static void BuildGroupBar(NModdingScreen __instance, Control? modInfoPanel)
    {
        _groupBar = ModdingScreenBars.CreateGroupBar(
            modInfoPanel,
            System.IO.File.Exists(ProfileManager.PortableConfigPath),
            OnPortableModeToggled,
            OnAddGroupRequested);
        __instance.AddChild(_groupBar);
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
        if (_currentScreen == null || !GodotObject.IsInstanceValid(_currentScreen)) return;
        Control modRowContainer = _currentScreen.GetNode<Control>("%ModsScrollContainer/Mask/Content");
        ModdingScreenGroupUi.RefreshGroupsUI(modRowContainer, _generatedGroupNodes, RefreshGroupsUI, RenameGroup, MoveGroup, ToggleAllInGroup);
    }

    private static void MoveGroup(string grpName, int direction)
    {
        if (ModdingScreenStateOps.TryMoveGroup(grpName, direction))
            RefreshGroupsUI();
    }

    private static void RenameGroup(string oldName)
    {
        if (_currentScreen == null || !GodotObject.IsInstanceValid(_currentScreen)) return;

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

        _currentScreen.AddChild(popup);
        popup.PopupCentered(new Vector2I(300, 100));
    }

    public static void MoveModOrder(string modId, int direction)
    {
        if (ModdingScreenListOps.TryMoveModOrder(modId, direction))
            RefreshGroupsUI();
    }

    private static void ToggleAllInGroup(string groupName, bool isToggled)
    {
        if (_currentScreen == null)
            return;

        Control modRowContainer = _currentScreen.GetNode<Control>("%ModsScrollContainer/Mask/Content");
        if (ModdingScreenListOps.ApplyToggleAllInGroup(modRowContainer, groupName, isToggled))
        {
            _suppressAutoSave = true;
            try
            {
                ProfileManager.SaveInMemoryState();
                SaveManager.Instance.SaveSettings();
                _currentScreen.OnModEnabledOrDisabled();
            }
            finally
            {
                _suppressAutoSave = false;
            }
        }
    }

    private static void RefreshProfileDropdown()
    {
        if (_profileDropdown == null || !GodotObject.IsInstanceValid(_profileDropdown)) return;

        ProfileManager.NormalizeProfileIndex();
        _profileDropdown.Clear();
        for (int i = 0; i < ProfileManager.Profiles.Count; i++)
            _profileDropdown.AddItem(ProfileManager.Profiles[i].Name, i);

        _profileDropdown.Select(ProfileManager.CurrentProfileIndex);
    }

    private static void OnProfileSelected(long index)
    {
        ApplyProfileSelection((int)index, snapshotCurrentProfile: true);
    }

    private static void ApplyProfileSelection(int index, bool snapshotCurrentProfile)
    {
        var profile = ModdingScreenProfileOps.ApplyProfileSelection(index, snapshotCurrentProfile);
        RefreshProfileDropdown();

        if (_currentScreen != null && GodotObject.IsInstanceValid(_currentScreen))
        {
            // Block BOTH the auto-save hook AND the vanilla tickbox handler
            _suppressAutoSave = true;
            NModMenuRowPatch.SuppressTickboxHandler = true;
            try
            {
                _currentScreen.OnModEnabledOrDisabled();

                // Set tickboxes SYNCHRONOUSLY (not deferred) while the vanilla handler is blocked.
                // The vanilla _Ready does the same (line 100 of NModMenuRow.cs) so this is safe.
                var modRowContainer = _currentScreen.GetNode<Control>("%ModsScrollContainer/Mask/Content");
                ModdingScreenProfileOps.SyncTickboxesForProfile(modRowContainer, profile);
                RefreshGroupsUI();
            }
            finally
            {
                NModMenuRowPatch.SuppressTickboxHandler = false;
                _suppressAutoSave = false;
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
        if (_currentScreen == null || !GodotObject.IsInstanceValid(_currentScreen)) return;

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

        _currentScreen.AddChild(popup);
        popup.PopupCentered(new Vector2I(300, 100));
    }

    private static void OnDelProfilePressed()
    {
        int? replacementIndex = ModdingScreenProfileOps.DeleteCurrentProfile();
        if (replacementIndex.HasValue)
            ApplyProfileSelection(replacementIndex.Value, snapshotCurrentProfile: false);
    }
}
