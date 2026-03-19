using HarmonyLib;
using Godot;
using MegaCrit.Sts2.Core.Nodes.Screens.ModdingScreen;
using MegaCrit.Sts2.Core.Modding;
using MegaCrit.Sts2.Core.Saves;
using BettermodmanagerUI.Data;
using System.Collections.Generic;

namespace BettermodmanagerUI.Patches;

[HarmonyPatch(typeof(NModMenuRow))]
public static class NModMenuRowPatch
{
    [HarmonyPatch(nameof(NModMenuRow._Ready))]
    [HarmonyPostfix]
    public static void Postfix_Ready(NModMenuRow __instance)
    {
        if (__instance.Mod == null || __instance.Mod.manifest == null) return;

        // Container for custom controls on the row
        var container = new HBoxContainer();
        __instance.AddChild(container);
        
        var upBtn = new Button { Text = "^", CustomMinimumSize = new Vector2(40, 40) };
        var modId = __instance.Mod.manifest.id;
        upBtn.Pressed += () => MoveModOrder(modId, -1);
        container.AddChild(upBtn);

        var downBtn = new Button { Text = "v", CustomMinimumSize = new Vector2(40, 40) };
        downBtn.Pressed += () => MoveModOrder(modId, 1);
        container.AddChild(downBtn);

        // Group Assignment Dropdown
        var groupDropdown = new OptionButton();
        groupDropdown.AddItem("No Group", 0);
        groupDropdown.AddItem("Gameplay", 1);
        groupDropdown.AddItem("QoL", 2);
        groupDropdown.AddItem("Libraries", 3);
        
        // Restore currently selected group from Profile
        var profile = ProfileManager.CurrentProfile;
        if (profile.ModGroups.TryGetValue(modId, out string group))
        {
            if (group == "Gameplay") groupDropdown.Select(1);
            else if (group == "QoL") groupDropdown.Select(2);
            else if (group == "Libraries") groupDropdown.Select(3);
        }

        groupDropdown.ItemSelected += (idx) =>
        {
            var p = ProfileManager.CurrentProfile;
            if (idx == 0) p.ModGroups.Remove(modId);
            else if (idx == 1) p.ModGroups[modId] = "Gameplay";
            else if (idx == 2) p.ModGroups[modId] = "QoL";
            else if (idx == 3) p.ModGroups[modId] = "Libraries";
            ProfileManager.SaveProfiles();
        };

        container.AddChild(groupDropdown);

        container.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.CenterRight);
        var pos = container.Position;
        pos.X -= 300;
        container.Position = pos;
    }

    private static void MoveModOrder(string modId, int direction)
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
            
            // To see visual changes without restart, we would need to refresh NModdingScreen list.
        }
    }
}
