using HarmonyLib;
using Godot;
using MegaCrit.Sts2.Core.Nodes.Screens.ModdingScreen;
using System;

namespace BetterModMenu.Patches;

[HarmonyPatch(typeof(NModMenuRow))]
public static class NModMenuRowPatch
{
    // When true, the vanilla OnTickboxToggled handler is completely blocked.
    // This prevents deferred IsTicked changes from writing back mod.IsEnabled during profile switches.
    public static bool SuppressTickboxHandler = false;

    [HarmonyPatch("OnTickboxToggled")]
    [HarmonyPrefix]
    public static bool Prefix_OnTickboxToggled()
    {
        // Return false = skip the original method entirely
        return !SuppressTickboxHandler;
    }

    [HarmonyPatch(nameof(NModMenuRow._Ready))]
    [HarmonyPostfix]
    public static void Postfix_Ready(NModMenuRow __instance)
    {
        if (__instance.Mod == null || __instance.Mod.manifest == null) return;

        var container = new HBoxContainer { Name = "RowCustomControls" };
        __instance.AddChild(container);

        var modId = __instance.Mod.manifest.id ?? "";
        if (string.IsNullOrEmpty(modId)) return;

        var upBtn = new Button { Text = "^", CustomMinimumSize = new Vector2(40, 40) };
        upBtn.Pressed += () => Callable.From(() => NModdingScreenPatch.MoveModOrder(modId, -1, __instance)).CallDeferred();
        container.AddChild(upBtn);

        var downBtn = new Button { Text = "v", CustomMinimumSize = new Vector2(40, 40) };
        downBtn.Pressed += () => Callable.From(() => NModdingScreenPatch.MoveModOrder(modId, 1, __instance)).CallDeferred();
        container.AddChild(downBtn);

        var groupDropdown = new OptionButton { Name = "GroupDropdown", CustomMinimumSize = new Vector2(180, 0) };

        groupDropdown.ItemSelected += (idx) =>
        {
            var selectedText = groupDropdown.GetItemText((int)idx);

            if (selectedText == "Unassigned")
                Data.ProfileManager.ModGroups.Remove(modId);
            else
                Data.ProfileManager.ModGroups[modId] = selectedText;

            Data.ProfileManager.SaveProfiles();
            Callable.From(() => NModdingScreenPatch.RefreshGroupsUI()).CallDeferred();
        };

        container.AddChild(groupDropdown);

        container.SetAnchorsPreset(Control.LayoutPreset.CenterRight);
        container.OffsetRight = -150;
        container.GrowHorizontal = Control.GrowDirection.Begin;
    }
}
