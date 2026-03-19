using HarmonyLib;
using Godot;
using MegaCrit.Sts2.Core.Nodes.Screens.ModdingScreen;
using MegaCrit.Sts2.Core.Modding;
using BettermodmanagerUI.Data;
using System.Collections.Generic;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.addons.mega_text;

namespace BettermodmanagerUI.Patches;

[HarmonyPatch(typeof(NModdingScreen))]
public static class NModdingScreenPatch
{
    private static NModdingScreen _currentScreen;
    private static Control _customUIRoot;
    private static OptionButton _profileDropdown;
    private static VBoxContainer _groupsContainer;

    [HarmonyPatch(nameof(NModdingScreen._Ready))]
    [HarmonyPostfix]
    public static void Postfix_Ready(NModdingScreen __instance)
    {
        _currentScreen = __instance;

        Control modRowContainer = __instance.GetNode<Control>("%ModsScrollContainer/Mask/Content");
        Control scrollContainer = __instance.GetNode<Control>("%ModsScrollContainer");

        // Inject our custom UI above the scroll container
        if (_customUIRoot == null || !GodotObject.IsInstanceValid(_customUIRoot))
        {
            _customUIRoot = new VBoxContainer();
            scrollContainer.GetParent().AddChild(_customUIRoot);
            // Move it above the scroll container
            scrollContainer.GetParent().MoveChild(_customUIRoot, scrollContainer.GetIndex());

            var topBar = new HBoxContainer();
            _customUIRoot.AddChild(topBar);

            var profileLabel = new Label();
            profileLabel.Text = "Profile: ";
            topBar.AddChild(profileLabel);

            _profileDropdown = new OptionButton();
            topBar.AddChild(_profileDropdown);
            _profileDropdown.ItemSelected += OnProfileSelected;

            var newProfileBtn = new Button();
            newProfileBtn.Text = "New Profile";
            newProfileBtn.Pressed += OnNewProfilePressed;
            topBar.AddChild(newProfileBtn);

            var saveProfileBtn = new Button();
            saveProfileBtn.Text = "Save Profile";
            saveProfileBtn.Pressed += OnSaveProfilePressed;
            topBar.AddChild(saveProfileBtn);

            // Group container will hold group headers
            _groupsContainer = new VBoxContainer();
            modRowContainer.AddChild(_groupsContainer);
        }

        RefreshProfileDropdown();
        RebuildGroupControls();
        RebuildModList(modRowContainer);
    }

    public static void RefreshProfileDropdown()
    {
        if (_profileDropdown == null || !GodotObject.IsInstanceValid(_profileDropdown)) return;
        
        _profileDropdown.Clear();
        for (int i = 0; i < ProfileManager.Profiles.Count; i++)
        {
            _profileDropdown.AddItem(ProfileManager.Profiles[i].Name, i);
        }
        _profileDropdown.Select(ProfileManager.CurrentProfileIndex);
    }

    public static void RebuildModList(Control modRowContainer)
    {
        // For now, we just let the base game handle the rows, we will move them under group headers soon.
        // The original _Ready already added all NModMenuRow instances.
    }

    private static void OnProfileSelected(long index)
    {
        ProfileManager.CurrentProfileIndex = (int)index;
        // Apply profile
        var profile = ProfileManager.CurrentProfile;
        
        var options = SaveManager.Instance.SettingsSave.ModSettings;
        if (options != null)
        {
            foreach (var mod in options.ModList)
            {
                mod.IsEnabled = !profile.DisabledMods.Contains(mod.Id);
            }
        }
        
        // Refresh UI
        if (_currentScreen != null && GodotObject.IsInstanceValid(_currentScreen))
        {
            _currentScreen.OnModEnabledOrDisabled();
            // Actually, we need to toggle the tickboxes visually too.
            var modRowContainer = _currentScreen.GetNode<Control>("%ModsScrollContainer/Mask/Content");
            foreach (Node child in modRowContainer.GetChildren())
            {
                if (child is NModMenuRow row && row.Mod != null)
                {
                    var tickbox = row.GetNode<MegaCrit.Sts2.Core.Nodes.CommonUi.NTickbox>("Tickbox");
                    if (tickbox != null)
                    {
                        tickbox.IsTicked = !profile.DisabledMods.Contains(row.Mod.manifest.id);
                    }
                }
            }
        }
    }

    private static void OnNewProfilePressed()
    {
        var newProfile = new ModProfile { Name = "Profile " + (ProfileManager.Profiles.Count + 1) };
        ProfileManager.Profiles.Add(newProfile);
        ProfileManager.CurrentProfileIndex = ProfileManager.Profiles.Count - 1;
        RefreshProfileDropdown();
        ProfileManager.SaveProfiles();
    }

    private static void OnSaveProfilePressed()
    {
        var profile = ProfileManager.CurrentProfile;
        var options = SaveManager.Instance.SettingsSave.ModSettings;
        if (options != null)
        {
            profile.DisabledMods.Clear();
            foreach (var mod in options.ModList)
            {
                if (!mod.IsEnabled && mod.Id != null)
                {
                    profile.DisabledMods.Add(mod.Id);
                }
            }
        }
        ProfileManager.SaveProfiles();
    }

    private static void RebuildGroupControls()
    {
        if (_groupsContainer == null || !GodotObject.IsInstanceValid(_groupsContainer)) return;
        
        foreach (Node child in _groupsContainer.GetChildren()) 
        {
            _groupsContainer.RemoveChild(child);
            child.QueueFree();
        }

        var title = new Label { Text = "Mod Groups (Toggle All):" };
        _groupsContainer.AddChild(title);

        var groupsBox = new HBoxContainer();
        _groupsContainer.AddChild(groupsBox);

        string[] groups = { "Gameplay", "QoL", "Libraries" };
        foreach (var grp in groups)
        {
            var btn = new CheckBox { Text = grp };
            btn.ButtonPressed = true; // Default to checked
            btn.Toggled += (toggledOn) => OnGroupToggled(grp, toggledOn);
            groupsBox.AddChild(btn);
        }
    }

    private static void OnGroupToggled(string groupName, bool isToggled)
    {
        var profile = ProfileManager.CurrentProfile;
        var options = SaveManager.Instance.SettingsSave.ModSettings;
        if (options == null) return;

        bool changed = false;
        foreach (var mod in options.ModList)
        {
            if (mod.Id != null && profile.ModGroups.TryGetValue(mod.Id, out string grp))
            {
                if (grp == groupName)
                {
                    mod.IsEnabled = isToggled;
                    if (isToggled) profile.DisabledMods.Remove(mod.Id);
                    else profile.DisabledMods.Add(mod.Id);
                    changed = true;
                }
            }
        }

        if (changed)
        {
            ProfileManager.SaveProfiles();
            SaveManager.Instance.SaveSettings();
            
            if (_currentScreen != null && GodotObject.IsInstanceValid(_currentScreen))
            {
                _currentScreen.OnModEnabledOrDisabled();
                
                var modRowContainer = _currentScreen.GetNode<Control>("%ModsScrollContainer/Mask/Content");
                foreach (Node child in modRowContainer.GetChildren())
                {
                    if (child is NModMenuRow row && row.Mod != null)
                    {
                        if (profile.ModGroups.TryGetValue(row.Mod.manifest.id, out string rowGrp) && rowGrp == groupName)
                        {
                            var tickbox = row.GetNode<MegaCrit.Sts2.Core.Nodes.CommonUi.NTickbox>("Tickbox");
                            if (tickbox != null) tickbox.IsTicked = isToggled;
                        }
                    }
                }
            }
        }
    }
}
