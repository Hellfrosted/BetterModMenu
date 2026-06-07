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

        var platformIcon = __instance.GetNodeOrNull<TextureRect>("PlatformIcon");
        if (platformIcon != null)
            platformIcon.Visible = false;

        if (__instance.GetNodeOrNull<HBoxContainer>("RowCustomControls") != null) return;

        var container = new HBoxContainer { Name = "RowCustomControls" };
        __instance.AddChild(container);

        Label? warningLabel = null;
        if (Data.ProfileManager.ModGameplayImpactCache.TryGetValue(modId, out bool affectsGameplay) && affectsGameplay)
        {
            warningLabel = new Label 
            { 
                Text = "Gameplay",
                TooltipText = "This mod affects gameplay.",
                VerticalAlignment = VerticalAlignment.Center
            };
            warningLabel.AddThemeColorOverride("font_color", new Color(1f, 0.67f, 0.36f));
            container.AddChild(warningLabel);
            
            var spacer = new Control { CustomMinimumSize = new Vector2(10, 0) };
            container.AddChild(spacer);
        }

        const string orderTooltip = "Move this mod in the saved load order for the next launch. Dependency rules may still move it during startup.";

        var upBtn = new Button
        {
            Text = "^",
            CustomMinimumSize = new Vector2(ModdingScreenConstants.RowButtonSize, ModdingScreenConstants.RowButtonSize),
            TooltipText = orderTooltip
        };
        ModdingScreenVanillaStyle.ApplySmallButton(upBtn);
        upBtn.Pressed += () => QueueMoveModOrder(__instance, modId, -1);
        container.AddChild(upBtn);

        var downBtn = new Button
        {
            Text = "v",
            CustomMinimumSize = new Vector2(ModdingScreenConstants.RowButtonSize, ModdingScreenConstants.RowButtonSize),
            TooltipText = orderTooltip
        };
        ModdingScreenVanillaStyle.ApplySmallButton(downBtn);
        downBtn.Pressed += () => QueueMoveModOrder(__instance, modId, 1);
        container.AddChild(downBtn);

        var groupDropdown = new OptionButton
        {
            Name = "GroupDropdown",
            TooltipText = "Choose which custom group this mod appears under. This does not enable, disable, install, or uninstall the mod.",
            CustomMinimumSize = new Vector2(ModdingScreenConstants.RowDropdownWidth, 0)
        };
        ModdingScreenVanillaStyle.ApplyOptionButton(groupDropdown);

        groupDropdown.ItemSelected += (idx) =>
        {
            bool hadPreviousAssignment = Data.ProfileManager.ModGroups.TryGetValue(modId, out string? previousAssignedGroup);
            string previousVisibleGroup = ModdingScreenStateOps.GetAssignedGroup(modId);
            var selectedText = groupDropdown.GetItemText((int)idx);

            if (selectedText == ModdingScreenConstants.UnassignedGroup)
                Data.ProfileManager.ModGroups.Remove(modId);
            else
                Data.ProfileManager.ModGroups[modId] = selectedText;

            if (!Data.ProfileManager.SaveInMemoryState())
            {
                if (hadPreviousAssignment && previousAssignedGroup != null)
                    Data.ProfileManager.ModGroups[modId] = previousAssignedGroup;
                else
                    Data.ProfileManager.ModGroups.Remove(modId);

                ModdingScreenStateOps.SyncGroupDropdown(groupDropdown, previousVisibleGroup);
                return;
            }

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

        groupDropdown.CustomMinimumSize = new Vector2(ModdingScreenConstants.RowDropdownWidth, ModdingScreenConstants.ToolbarControlHeight);
        if (warningLabel != null)
            warningLabel.Text = "Gameplay";

        float preferredWidth = container.GetCombinedMinimumSize().X;
        bool isCompact = row.Size.X > 0 && row.Size.X - preferredWidth < ModdingScreenConstants.RowMinimumLeftContentWidth;
        if (isCompact)
        {
            groupDropdown.CustomMinimumSize = new Vector2(ModdingScreenConstants.RowDropdownCompactWidth, ModdingScreenConstants.ToolbarControlHeight);
        }

        float width = container.GetCombinedMinimumSize().X;
        container.SetAnchorsPreset(Control.LayoutPreset.CenterRight);
        container.GrowHorizontal = Control.GrowDirection.Begin;
        container.OffsetRight = -(ModdingScreenConstants.RowControlsRightPadding + ModdingScreenConstants.RowNativeTickboxReserveWidth);
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
