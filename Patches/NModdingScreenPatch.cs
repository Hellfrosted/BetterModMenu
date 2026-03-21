using HarmonyLib;
using Godot;
using MegaCrit.Sts2.Core.Nodes.Screens.ModdingScreen;
using BetterModMenu.Data;
using System.Collections.Generic;
using MegaCrit.Sts2.Core.Saves;
using System.Linq;

namespace BetterModMenu.Patches;

[HarmonyPatch(typeof(NModdingScreen))]
public static class NModdingScreenPatch
{
    private static NModdingScreen? _currentScreen;
    private static HBoxContainer? _topBar;
    private static OptionButton? _profileDropdown;
    private static HBoxContainer? _groupBar;
    private static bool _suppressAutoSave = false;

    [HarmonyPatch(nameof(NModdingScreen.OnModEnabledOrDisabled))]
    [HarmonyPostfix]
    public static void Postfix_OnModEnabledOrDisabled()
    {
        if (!_suppressAutoSave)
            ProfileManager.SaveProfiles();
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

            _topBar = new HBoxContainer();
            __instance.AddChild(_topBar);

            if (titleNode != null && scrollContainer != null)
            {
                float leftPanelRight = scrollContainer.GlobalPosition.X + scrollContainer.Size.X;
                _topBar.Position = new Vector2(
                    titleNode.GlobalPosition.X + titleNode.Size.X + 10,
                    titleNode.GlobalPosition.Y
                );
                _topBar.Size = new Vector2(
                    leftPanelRight - (titleNode.GlobalPosition.X + titleNode.Size.X + 10) - 30,
                    titleNode.Size.Y
                );
            }
            else
            {
                _topBar.Position = new Vector2(300, 55);
                _topBar.Size = new Vector2(200, 30);
            }

            var profileLabel = new Label { Text = "Profile:" };
            _topBar.AddChild(profileLabel);

            _profileDropdown = new OptionButton { CustomMinimumSize = new Vector2(120, 0) };
            _topBar.AddChild(_profileDropdown);
            _profileDropdown.ItemSelected += OnProfileSelected;

            var newProfileBtn = new Button { Text = "+ New" };
            newProfileBtn.Pressed += OnNewProfilePressed;
            _topBar.AddChild(newProfileBtn);

            var renameProfileBtn = new Button { Text = "Rename" };
            renameProfileBtn.Pressed += OnRenameProfilePressed;
            _topBar.AddChild(renameProfileBtn);

            var delProfileBtn = new Button { Text = "Del" };
            delProfileBtn.Pressed += OnDelProfilePressed;
            _topBar.AddChild(delProfileBtn);

            var modInfoPanel = __instance.GetNodeOrNull<Control>("%ModInfoContainer");
            _groupBar = new HBoxContainer();
            __instance.AddChild(_groupBar);

            if (modInfoPanel != null)
            {
                _groupBar.Position = new Vector2(
                    modInfoPanel.GlobalPosition.X,
                    modInfoPanel.GlobalPosition.Y - 35
                );
                _groupBar.Size = new Vector2(modInfoPanel.Size.X, 28);
            }
            else
            {
                _groupBar.Position = new Vector2(550, 30);
                _groupBar.Size = new Vector2(400, 28);
            }
            _groupBar.Alignment = BoxContainer.AlignmentMode.End;

            var groupLabel = new Label { Text = "Group:" };
            _groupBar.AddChild(groupLabel);

            var newGroupInput = new LineEdit { PlaceholderText = "Name...", CustomMinimumSize = new Vector2(140, 0) };
            _groupBar.AddChild(newGroupInput);

            var newGroupBtn = new Button { Text = "+ Add" };
            newGroupBtn.Pressed += () => {
                var txt = newGroupInput.Text.Trim();
                if (!string.IsNullOrEmpty(txt) && !ProfileManager.CustomGroups.Contains(txt) && txt != "Unassigned")
                {
                    ProfileManager.CustomGroups.Add(txt);
                    ProfileManager.SaveProfiles();
                    newGroupInput.Text = "";
                    RefreshGroupsUI();
                }
            };
            _groupBar.AddChild(newGroupBtn);
        }

        RefreshProfileDropdown();
        RefreshGroupsUI();
    }

    public static void RefreshGroupsUI()
    {
        if (_currentScreen == null || !GodotObject.IsInstanceValid(_currentScreen)) return;
        Control modRowContainer = _currentScreen.GetNode<Control>("%ModsScrollContainer/Mask/Content");

        var rows = new List<NModMenuRow>();
        var toDelete = new List<Node>();
        foreach (Node child in modRowContainer.GetChildren())
        {
            if (child is NModMenuRow r)
                rows.Add(r);
            else if (child.Name.ToString().StartsWith("ModGroup"))
                toDelete.Add(child);
        }

        foreach (var child in toDelete)
        {
            modRowContainer.RemoveChild(child);
            child.QueueFree();
        }

        var options = SaveManager.Instance.SettingsSave.ModSettings;
        if (options != null && options.ModList != null)
        {
            rows = rows.OrderBy(r => {
                if (r.Mod == null || r.Mod.manifest == null) return 9999;
                var idx = options.ModList.FindIndex(m => m.Id == r.Mod.manifest.id);
                return idx == -1 ? 9999 : idx;
            }).ToList();
        }

        var groups = new Dictionary<string, List<NModMenuRow>>();
        groups["Unassigned"] = new List<NModMenuRow>();
        foreach (var grp in ProfileManager.CustomGroups) groups[grp] = new List<NModMenuRow>();

        foreach (var row in rows)
        {
            string grp = "Unassigned";
            if (row.Mod != null && row.Mod.manifest != null)
            {
                if (ProfileManager.ModGroups.TryGetValue(row.Mod.manifest.id, out string? assignedGrp))
                {
                    if (assignedGrp != null && ProfileManager.CustomGroups.Contains(assignedGrp))
                        grp = assignedGrp;
                }
            }
            groups[grp].Add(row);

            var dropdown = row.GetNodeOrNull<OptionButton>("RowCustomControls/GroupDropdown");
            if (dropdown != null)
            {
                string currSelected = (dropdown.ItemCount > 0 && dropdown.Selected >= 0) ? dropdown.GetItemText(dropdown.Selected) : "";
                if (currSelected != grp || dropdown.ItemCount != ProfileManager.CustomGroups.Count + 1)
                {
                    dropdown.Clear();
                    dropdown.AddItem("Unassigned", 0);
                    for (int i = 0; i < ProfileManager.CustomGroups.Count; i++)
                        dropdown.AddItem(ProfileManager.CustomGroups[i], i + 1);

                    int selectIdx = 0;
                    if (grp != "Unassigned")
                        selectIdx = ProfileManager.CustomGroups.IndexOf(grp) + 1;

                    dropdown.Select(selectIdx);
                }
            }
        }

        int idx = 0;
        foreach (var kvp in groups)
        {
            string grpName = kvp.Key;

            if (grpName == "Unassigned" && kvp.Value.Count == 0)
                continue;

            bool isCollapsed = ProfileManager.CollapsedGroups.Contains(grpName);

            var sep = new ColorRect { Name = "ModGroupSep_" + grpName, CustomMinimumSize = new Vector2(0, 4), Color = new Color(0, 0, 0, 0) };
            modRowContainer.AddChild(sep);
            modRowContainer.MoveChild(sep, idx++);

            var header = new HBoxContainer { Name = "ModGroupHeader_" + grpName };

            var collapseBtn = new Button { Text = isCollapsed ? "► " + grpName : "▼ " + grpName, SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
            collapseBtn.Pressed += () => {
                if (ProfileManager.CollapsedGroups.Contains(grpName))
                    ProfileManager.CollapsedGroups.Remove(grpName);
                else
                    ProfileManager.CollapsedGroups.Add(grpName);
                ProfileManager.SaveProfiles();
                RefreshGroupsUI();
            };
            header.AddChild(collapseBtn);

            bool allEnabled = true;
            foreach (var r in kvp.Value)
            {
                var tick = r.GetNodeOrNull<MegaCrit.Sts2.Core.Nodes.CommonUi.NTickbox>("Tickbox");
                if (tick != null && !(bool)tick.Get("IsTicked")) allEnabled = false;
            }
            var toggleAllBtn = new Button { Text = allEnabled ? "Disable All" : "Enable All" };
            toggleAllBtn.Pressed += () => ToggleAllInGroup(grpName, !allEnabled);
            header.AddChild(toggleAllBtn);

            if (grpName != "Unassigned")
            {
                var deleteBtn = new Button { Text = "Del" };
                deleteBtn.Pressed += () => {
                    ProfileManager.CustomGroups.Remove(grpName);
                    var grpMods = ProfileManager.ModGroups.Where(x => x.Value == grpName).Select(x => x.Key).ToList();
                    foreach (var m in grpMods) ProfileManager.ModGroups.Remove(m);
                    ProfileManager.SaveProfiles();
                    RefreshGroupsUI();
                };
                header.AddChild(deleteBtn);
            }

            modRowContainer.AddChild(header);
            modRowContainer.MoveChild(header, idx++);

            foreach (var row in kvp.Value)
            {
                modRowContainer.MoveChild(row, idx++);
                row.Visible = !isCollapsed;
            }
        }
    }

    public static void MoveModOrder(string modId, int direction, NModMenuRow rowNode)
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
            ProfileManager.SaveProfiles();

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
                string modId = row.Mod.manifest.id;
                string assignedGrp = "Unassigned";
                if (ProfileManager.ModGroups.TryGetValue(modId, out string? grpVal) && grpVal != null && ProfileManager.CustomGroups.Contains(grpVal))
                    assignedGrp = grpVal;

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
                    var tickbox = row.GetNodeOrNull<MegaCrit.Sts2.Core.Nodes.CommonUi.NTickbox>("Tickbox");
                    if (tickbox != null)
                    {
                        try
                        {
                            NModMenuRowPatch.SuppressTickboxHandler = true;
                            tickbox.IsTicked = isToggled;
                            NModMenuRowPatch.SuppressTickboxHandler = false;
                        }
                        catch { NModMenuRowPatch.SuppressTickboxHandler = false; }
                    }
                }
            }
        }

        if (changed)
        {
            _suppressAutoSave = true;
            ProfileManager.SaveProfiles();
            SaveManager.Instance.SaveSettings();
            _currentScreen.OnModEnabledOrDisabled();
            _suppressAutoSave = false;
        }
    }

    private static void RefreshProfileDropdown()
    {
        if (_profileDropdown == null || !GodotObject.IsInstanceValid(_profileDropdown)) return;

        _profileDropdown.Clear();
        for (int i = 0; i < ProfileManager.Profiles.Count; i++)
            _profileDropdown.AddItem(ProfileManager.Profiles[i].Name, i);

        _profileDropdown.Select(ProfileManager.CurrentProfileIndex);
    }

    private static void OnProfileSelected(long index)
    {
        ProfileManager.SnapshotIntoProfile(ProfileManager.CurrentProfile);

        ProfileManager.CurrentProfileIndex = (int)index;
        var profile = ProfileManager.CurrentProfile;
        var options = SaveManager.Instance.SettingsSave.ModSettings;
        if (options != null)
        {
            foreach (var mod in options.ModList)
                mod.IsEnabled = !profile.DisabledMods.Contains(mod.Id);
        }

        ProfileManager.SaveToDisk();
        SaveManager.Instance.SaveSettings();

        if (_currentScreen != null && GodotObject.IsInstanceValid(_currentScreen))
        {
            // Block BOTH the auto-save hook AND the vanilla tickbox handler
            _suppressAutoSave = true;
            NModMenuRowPatch.SuppressTickboxHandler = true;

            _currentScreen.OnModEnabledOrDisabled();

            // Set tickboxes SYNCHRONOUSLY (not deferred) while the vanilla handler is blocked.
            // The vanilla _Ready does the same (line 100 of NModMenuRow.cs) so this is safe.
            var modRowContainer = _currentScreen.GetNode<Control>("%ModsScrollContainer/Mask/Content");
            foreach (Node child in modRowContainer.GetChildren())
            {
                if (child is NModMenuRow row && row.Mod?.manifest != null)
                {
                    bool isOn = !profile.DisabledMods.Contains(row.Mod.manifest.id);
                    var tickbox = row.GetNodeOrNull<MegaCrit.Sts2.Core.Nodes.CommonUi.NTickbox>("Tickbox");
                    if (tickbox != null)
                    {
                        try { tickbox.IsTicked = isOn; }
                        catch { }
                    }
                }
            }

            // Un-suppress synchronously — everything is already done, no deferred calls pending
            NModMenuRowPatch.SuppressTickboxHandler = false;
            _suppressAutoSave = false;

            RefreshGroupsUI();
        }
    }

    private static void OnNewProfilePressed()
    {
        ProfileManager.SaveProfiles();
        var newProfile = new ModProfile
        {
            Name = "Profile " + (ProfileManager.Profiles.Count + 1),
            DisabledMods = new HashSet<string>(ProfileManager.CurrentProfile.DisabledMods)
        };
        ProfileManager.Profiles.Add(newProfile);
        ProfileManager.CurrentProfileIndex = ProfileManager.Profiles.Count - 1;
        ProfileManager.SaveProfiles();
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
            var newName = input.Text.Trim();
            if (!string.IsNullOrEmpty(newName))
            {
                ProfileManager.CurrentProfile.Name = newName;
                ProfileManager.SaveProfiles();
                RefreshProfileDropdown();
            }
        };

        _currentScreen.AddChild(popup);
        popup.PopupCentered(new Vector2I(300, 100));
    }

    private static void OnDelProfilePressed()
    {
        if (ProfileManager.Profiles.Count > 1)
        {
            ProfileManager.Profiles.RemoveAt(ProfileManager.CurrentProfileIndex);
            ProfileManager.CurrentProfileIndex = 0;
            ProfileManager.SaveProfiles();
            RefreshProfileDropdown();
            OnProfileSelected(0);
        }
    }
}
