using HarmonyLib;
using Godot;
using MegaCrit.Sts2.Core.Nodes.Screens.ModdingScreen;

namespace BetterModMenu.Patches;

[HarmonyPatch(typeof(NModMenuRow))]
public static class NModMenuRowPatch
{
    [HarmonyPatch("OnTickboxToggled")]
    [HarmonyPrefix]
    public static bool Prefix_OnTickboxToggled(NModMenuRow __instance)
    {
        var screen = ModdingScreenNodeOps.FindOwningScreen(__instance);
        return screen == null || !NModdingScreenPatch.IsTickboxHandlerSuppressed(screen);
    }

    [HarmonyPatch(nameof(NModMenuRow._Ready))]
    [HarmonyPostfix]
    public static void Postfix_Ready(NModMenuRow __instance)
    {
        if (__instance.Mod == null || __instance.Mod.manifest == null) return;

        var modId = __instance.Mod.manifest.id ?? "";
        if (string.IsNullOrEmpty(modId)) return;

        if (__instance.GetNodeOrNull<HBoxContainer>("RowCustomControls") != null) return;

        var container = new HBoxContainer { Name = "RowCustomControls" };
        __instance.AddChild(container);

        if (Data.ProfileManager.ModGameplayImpactCache.TryGetValue(modId, out bool affectsGameplay) && affectsGameplay)
        {
            var warningLabel = new Label 
            { 
                Text = "[Gameplay]", 
                TooltipText = "This mod affects gameplay.",
                Modulate = new Color(1f, 0.5f, 0.3f),
                VerticalAlignment = VerticalAlignment.Center
            };
            container.AddChild(warningLabel);
            
            var spacer = new Control { CustomMinimumSize = new Vector2(10, 0) };
            container.AddChild(spacer);
        }

        var upBtn = new Button { Text = "^", CustomMinimumSize = new Vector2(40, 40) };
        upBtn.Pressed += () => QueueMoveModOrder(__instance, modId, -1);
        container.AddChild(upBtn);

        var downBtn = new Button { Text = "v", CustomMinimumSize = new Vector2(40, 40) };
        downBtn.Pressed += () => QueueMoveModOrder(__instance, modId, 1);
        container.AddChild(downBtn);

        var groupDropdown = new OptionButton { Name = "GroupDropdown", CustomMinimumSize = new Vector2(180, 0) };

        groupDropdown.ItemSelected += (idx) =>
        {
            var selectedText = groupDropdown.GetItemText((int)idx);

            if (selectedText == ModdingScreenConstants.UnassignedGroup)
                Data.ProfileManager.ModGroups.Remove(modId);
            else
                Data.ProfileManager.ModGroups[modId] = selectedText;

            Data.ProfileManager.SaveInMemoryState();
            QueueRefreshGroupsUI(__instance);
        };

        container.AddChild(groupDropdown);

        container.SetAnchorsPreset(Control.LayoutPreset.CenterRight);
        container.OffsetRight = -150;
        container.GrowHorizontal = Control.GrowDirection.Begin;
    }

    private static void QueueMoveModOrder(NModMenuRow row, string modId, int direction)
    {
        var screen = ModdingScreenNodeOps.FindOwningScreen(row);
        if (screen == null)
            return;

        Callable.From(() =>
        {
            if (NModdingScreenPatch.IsCurrentScreen(screen))
                NModdingScreenPatch.MoveModOrder(modId, direction);
        }).CallDeferred();
    }

    private static void QueueRefreshGroupsUI(NModMenuRow row)
    {
        var screen = ModdingScreenNodeOps.FindOwningScreen(row);
        if (screen == null)
            return;

        Callable.From(() =>
        {
            if (NModdingScreenPatch.IsCurrentScreen(screen))
                NModdingScreenPatch.RefreshGroupsUI();
        }).CallDeferred();
    }
}
