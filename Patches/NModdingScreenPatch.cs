using HarmonyLib;
using Godot;
using MegaCrit.Sts2.Core.Nodes.Screens.ModdingScreen;
using BetterModMenu.Data;
using System.Collections.Generic;
using MegaCrit.Sts2.Core.Saves;
using System.Linq;
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
            var modInfoPanel = __instance.GetNodeOrNull<Control>("%ModInfoContainer");

            BuildTopBar(__instance, titleNode, scrollContainer);
            BuildGroupBar(__instance, modInfoPanel);
        }

        RefreshProfileDropdown();
        RefreshGroupsUI();
    }

    private static void BuildTopBar(NModdingScreen __instance, Control? titleNode, Control? scrollContainer)
    {
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
    }

    private static void BuildGroupBar(NModdingScreen __instance, Control? modInfoPanel)
    {
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
        _groupBar.Alignment = BoxContainer.AlignmentMode.Begin;

        var portableToggle = new CheckButton { Text = "Portable Mode" };
        portableToggle.ButtonPressed = System.IO.File.Exists(ProfileManager.PortableConfigPath);
        portableToggle.Toggled += (isToggled) => {
            if (isToggled) {
                try {
                    if (System.IO.File.Exists(ProfileManager.SavePath) && ProfileManager.SavePath != ProfileManager.PortableConfigPath)
                        System.IO.File.Copy(ProfileManager.SavePath, ProfileManager.PortableConfigPath, true);
                    else
                        ProfileManager.SaveProfiles();
                } catch (System.Exception ex) { ProfileManager.ModLogger.Error("Failed to enable portable mode: " + ex); }
            } else {
                try {
                    string userPath = UserDataPathProvider.GetAccountScopedBasePath("mod_data/BetterModMenu");
                    string absolutePath = Godot.ProjectSettings.GlobalizePath(userPath);
                    if (!System.IO.Directory.Exists(absolutePath)) System.IO.Directory.CreateDirectory(absolutePath);
                    string target = System.IO.Path.Combine(absolutePath, "mod_profiles.json");
                    
                    if (System.IO.File.Exists(ProfileManager.PortableConfigPath))
                        System.IO.File.Copy(ProfileManager.PortableConfigPath, target, true);
                    
                    System.IO.File.Delete(ProfileManager.PortableConfigPath);
                } catch (System.Exception ex) { ProfileManager.ModLogger.Error("Failed to disable portable mode: " + ex); }
            }
        };
        _groupBar.AddChild(portableToggle);

        _groupBar.AddChild(new Control { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill });

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
                if (!string.IsNullOrEmpty(modId) && ProfileManager.ModGroups.TryGetValue(modId, out string? assignedGrp))
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
                ProfileManager.SaveProfiles();
                RefreshGroupsUI();
            };
            header.AddChild(collapseBtn);

            bool allEnabled = true;
            foreach (var r in groupRows)
            {
                var tick = r.GetNodeOrNull<NTickbox>("Tickbox");
                if (tick != null && !(bool)tick.Get("IsTicked")) allEnabled = false;
            }
            var toggleAllBtn = new Button { Text = allEnabled ? "Disable All" : "Enable All" };
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
                    ProfileManager.CustomGroups.Remove(grpName);
                    var grpMods = ProfileManager.ModGroups.Where(x => x.Value == grpName).Select(x => x.Key).ToList();
                    foreach (var m in grpMods) ProfileManager.ModGroups.Remove(m);
                    ProfileManager.SaveProfiles();
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
        int idx = ProfileManager.CustomGroups.IndexOf(grpName);
        if (idx == -1) return;
        int newIdx = idx + direction;
        if (newIdx >= 0 && newIdx < ProfileManager.CustomGroups.Count)
        {
            ProfileManager.CustomGroups.RemoveAt(idx);
            ProfileManager.CustomGroups.Insert(newIdx, grpName);
            ProfileManager.SaveProfiles();
            RefreshGroupsUI();
        }
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
            var newName = input.Text.Trim();
            if (!string.IsNullOrEmpty(newName) && newName != "Unassigned" && !ProfileManager.CustomGroups.Contains(newName))
            {
                int idx = ProfileManager.CustomGroups.IndexOf(oldName);
                if (idx != -1)
                {
                    ProfileManager.CustomGroups[idx] = newName;

                    var grpMods = ProfileManager.ModGroups.Where(x => x.Value == oldName).Select(x => x.Key).ToList();
                    foreach (var m in grpMods) ProfileManager.ModGroups[m] = newName;

                    if (ProfileManager.CollapsedGroups.Contains(oldName))
                    {
                        ProfileManager.CollapsedGroups.Remove(oldName);
                        ProfileManager.CollapsedGroups.Add(newName);
                    }

                    ProfileManager.SaveProfiles();
                    RefreshGroupsUI();
                }
            }
        };

        _currentScreen.AddChild(popup);
        popup.PopupCentered(new Vector2I(300, 100));
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
                string modId = row.Mod.manifest.id ?? "";
                if (string.IsNullOrEmpty(modId)) continue;

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
                    string modId = row.Mod.manifest.id ?? "";
                    bool isOn = !string.IsNullOrEmpty(modId) && !profile.DisabledMods.Contains(modId);
                    var tickbox = row.GetNodeOrNull<NTickbox>("Tickbox");
                    if (tickbox != null)
                    {
                        try { tickbox.IsTicked = isOn; }
                        catch (System.Exception ex) { ProfileManager.ModLogger.Error($"Failed to set tickbox state:\n{ex}"); }
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
