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

        Label? warningLabel = null;
        if (Data.ProfileManager.ModGameplayImpactCache.TryGetValue(modId, out bool affectsGameplay) && affectsGameplay)
        {
            warningLabel = new Label 
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

        const string orderTooltip = "Reorders the saved mod list for the next launch. Slay the Spire 2 may still override dependency order, so this is not guaranteed multiplayer synchronization.";

        var upBtn = new Button
        {
            Text = "^",
            CustomMinimumSize = new Vector2(ModdingScreenConstants.RowButtonSize, ModdingScreenConstants.RowButtonSize),
            TooltipText = orderTooltip
        };
        upBtn.Pressed += () => QueueMoveModOrder(__instance, modId, -1);
        container.AddChild(upBtn);

        var downBtn = new Button
        {
            Text = "v",
            CustomMinimumSize = new Vector2(ModdingScreenConstants.RowButtonSize, ModdingScreenConstants.RowButtonSize),
            TooltipText = orderTooltip
        };
        downBtn.Pressed += () => QueueMoveModOrder(__instance, modId, 1);
        container.AddChild(downBtn);

        var groupDropdown = new OptionButton { Name = "GroupDropdown", CustomMinimumSize = new Vector2(ModdingScreenConstants.RowDropdownWidth, 0) };

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

        __instance.Resized += () => UpdateCustomControlsLayout(__instance, container, warningLabel, groupDropdown);
        Callable.From(() => UpdateCustomControlsLayout(__instance, container, warningLabel, groupDropdown)).CallDeferred();
    }

    private static void UpdateCustomControlsLayout(NModMenuRow row, HBoxContainer container, Label? warningLabel, OptionButton groupDropdown)
    {
        if (!GodotObject.IsInstanceValid(row) || !GodotObject.IsInstanceValid(container))
            return;

        groupDropdown.CustomMinimumSize = new Vector2(ModdingScreenConstants.RowDropdownWidth, 0);
        if (warningLabel != null)
            warningLabel.Text = "[Gameplay]";

        float preferredWidth = container.GetCombinedMinimumSize().X;
        bool isCompact = row.Size.X > 0 && row.Size.X - preferredWidth < ModdingScreenConstants.RowMinimumLeftContentWidth;
        if (isCompact)
        {
            groupDropdown.CustomMinimumSize = new Vector2(ModdingScreenConstants.RowDropdownCompactWidth, 0);
            if (warningLabel != null)
                warningLabel.Text = "GP";
        }

        float width = container.GetCombinedMinimumSize().X;
        container.SetAnchorsPreset(Control.LayoutPreset.CenterRight);
        container.GrowHorizontal = Control.GrowDirection.Begin;
        container.OffsetRight = -ModdingScreenConstants.RowControlsRightPadding;
        container.OffsetLeft = container.OffsetRight - width;
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
