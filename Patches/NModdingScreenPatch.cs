using HarmonyLib;
using Godot;
using MegaCrit.Sts2.Core.Nodes.Screens.ModdingScreen;
using BetterModMenu.Data;
using System.Collections.Generic;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Nodes.CommonUi;

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

        var rows = new List<NModMenuRow>();
        ClearOldGroups(modRowContainer, rows);
        SortModRows(rows);

        var groups = GroupRows(rows);
        BuildGroupHeaders(modRowContainer, groups);
    }

    private static void ClearOldGroups(Control container, List<NModMenuRow> rows)
    {
        foreach (Node child in container.GetChildren())
        {
            if (child is NModMenuRow r)
                rows.Add(r);
            else if (child.Name.ToString().StartsWith("ModGroup"))
            {
                // Fallback for pre-existing leftover nodes (e.g. from script reloads)
                if (!_generatedGroupNodes.Contains(child))
                    _generatedGroupNodes.Add(child);
            }
        }

        foreach (var child in _generatedGroupNodes)
        {
            if (GodotObject.IsInstanceValid(child) && child.GetParent() == container)
            {
                container.RemoveChild(child);
                child.QueueFree();
            }
        }
        _generatedGroupNodes.Clear();
    }

    private static void SortModRows(List<NModMenuRow> rows)
    {
        var options = SaveManager.Instance.SettingsSave.ModSettings;
        if (options != null && options.ModList != null)
        {
            rows.Sort((r1, r2) => {
                int getIdx(NModMenuRow r) => r.Mod?.manifest?.id != null ? options.ModList.FindIndex(m => m.Id == r.Mod.manifest.id) : 9999;
                int idx1 = getIdx(r1);
                int idx2 = getIdx(r2);
                if (idx1 == -1) idx1 = 9999;
                if (idx2 == -1) idx2 = 9999;
                return idx1.CompareTo(idx2);
            });
        }
    }

    private static Dictionary<string, List<NModMenuRow>> GroupRows(List<NModMenuRow> rows)
    {
        var groups = new Dictionary<string, List<NModMenuRow>>();
        groups["Unassigned"] = new List<NModMenuRow>();
        foreach (var grp in ProfileManager.CustomGroups) groups[grp] = new List<NModMenuRow>();

        foreach (var row in rows)
        {
            string grp = "Unassigned";
            if (row.Mod?.manifest != null)
            {
                var modId = row.Mod.manifest.id ?? "";
                grp = ModdingScreenStateOps.GetAssignedGroup(modId);
            }
            groups[grp].Add(row);

            var dropdown = row.GetNodeOrNull<OptionButton>("RowCustomControls/GroupDropdown");
            if (dropdown != null)
                ModdingScreenStateOps.SyncGroupDropdown(dropdown, grp);
        }
        return groups;
    }

    private static void BuildGroupHeaders(Control container, Dictionary<string, List<NModMenuRow>> groups)
    {
        int idx = 0;
        var orderedGroups = new List<string> { "Unassigned" };
        orderedGroups.AddRange(ProfileManager.CustomGroups);

        foreach (var grpName in orderedGroups)
        {
            if (!groups.TryGetValue(grpName, out var groupRows))
                continue;

            if (grpName == "Unassigned" && groupRows.Count == 0)
                continue;

            bool isCollapsed = ProfileManager.CollapsedGroups.Contains(grpName);

            var sep = new ColorRect { Name = "ModGroupSep_" + grpName, CustomMinimumSize = new Vector2(0, 4), Color = new Color(0, 0, 0, 0) };
            container.AddChild(sep);
            container.MoveChild(sep, idx++);
            _generatedGroupNodes.Add(sep);

            var header = new HBoxContainer { Name = "ModGroupHeader_" + grpName };
            _generatedGroupNodes.Add(header);

            var collapseBtn = new Button { Text = isCollapsed ? "► " + grpName : "▼ " + grpName, SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
            collapseBtn.Pressed += () => {
                if (ProfileManager.CollapsedGroups.Contains(grpName))
                    ProfileManager.CollapsedGroups.Remove(grpName);
                else
                    ProfileManager.CollapsedGroups.Add(grpName);
                ProfileManager.SaveInMemoryState();
                RefreshGroupsUI();
            };
            header.AddChild(collapseBtn);

            bool hasRows = groupRows.Count > 0;
            bool allEnabled = hasRows;
            foreach (var r in groupRows)
            {
                var tick = r.GetNodeOrNull<NTickbox>("Tickbox");
                if (tick != null && !(bool)tick.Get("IsTicked")) allEnabled = false;
            }
            var toggleAllBtn = new Button { Text = allEnabled ? "Disable All" : "Enable All", Disabled = !hasRows };
            toggleAllBtn.Pressed += () => ToggleAllInGroup(grpName, !allEnabled);
            header.AddChild(toggleAllBtn);

            if (grpName != "Unassigned")
            {
                var renameBtn = new Button { Text = "Rename" };
                renameBtn.Pressed += () => RenameGroup(grpName);
                header.AddChild(renameBtn);

                header.AddChild(new Control { CustomMinimumSize = new Vector2(10, 0) });

                var upBtn = new Button { Text = "^" };
                upBtn.Pressed += () => MoveGroup(grpName, -1);
                header.AddChild(upBtn);

                header.AddChild(new Control { CustomMinimumSize = new Vector2(10, 0) });

                var downBtn = new Button { Text = "v" };
                downBtn.Pressed += () => MoveGroup(grpName, 1);
                header.AddChild(downBtn);

                header.AddChild(new Control { CustomMinimumSize = new Vector2(10, 0) });

                var deleteBtn = new Button { Text = "Del" };
                deleteBtn.Pressed += () => {
                    if (ModdingScreenStateOps.DeleteGroup(grpName))
                        RefreshGroupsUI();
                };
                header.AddChild(deleteBtn);
            }

            container.AddChild(header);
            container.MoveChild(header, idx++);

            foreach (var row in groupRows)
            {
                container.MoveChild(row, idx++);
                row.Visible = !isCollapsed;
            }
        }
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
        var options = SaveManager.Instance.SettingsSave.ModSettings;
        if (options == null) return;

        var list = options.ModList;
        int index = list.FindIndex(m => m.Id == modId);
        if (index == -1) return;

        int newIndex = index + direction;
        if (newIndex >= 0 && newIndex < list.Count)
        {
            var temp = list[index];
            list[index] = list[newIndex];
            list[newIndex] = temp;
            SaveManager.Instance.SaveSettings();
            ProfileManager.SaveInMemoryState();

            RefreshGroupsUI();
        }
    }

    private static void ToggleAllInGroup(string groupName, bool isToggled)
    {
        var profile = ProfileManager.CurrentProfile;
        var options = SaveManager.Instance.SettingsSave.ModSettings;
        if (options == null || _currentScreen == null) return;

        bool changed = false;
        Control modRowContainer = _currentScreen.GetNode<Control>("%ModsScrollContainer/Mask/Content");

        foreach (Node child in modRowContainer.GetChildren())
        {
            if (child is NModMenuRow row && row.Mod?.manifest != null)
            {
                string modId = row.Mod.manifest.id ?? "";
                if (string.IsNullOrEmpty(modId)) continue;

                string assignedGrp = ModdingScreenStateOps.GetAssignedGroup(modId);

                if (assignedGrp == groupName)
                {
                    var settingsMod = options.ModList.Find(m => m.Id == modId);
                    if (settingsMod != null)
                    {
                        settingsMod.IsEnabled = isToggled;
                        if (isToggled) profile.DisabledMods.Remove(modId);
                        else profile.DisabledMods.Add(modId);
                        changed = true;
                    }
                    var tickbox = row.GetNodeOrNull<NTickbox>("Tickbox");
                    if (tickbox != null)
                    {
                        try
                        {
                            NModMenuRowPatch.SuppressTickboxHandler = true;
                            tickbox.IsTicked = isToggled;
                        }
                        catch (System.Exception ex) 
                        { 
                            ProfileManager.ModLogger.Error($"Failed to toggle tickbox:\n{ex}"); 
                        }
                        finally
                        {
                            NModMenuRowPatch.SuppressTickboxHandler = false; 
                        }
                    }
                }
            }
        }

        if (changed)
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
