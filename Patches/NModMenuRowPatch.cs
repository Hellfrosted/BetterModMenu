using HarmonyLib;
using Godot;
using BetterModMenu.Data;
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

        ApplyColoredModName(__instance, modId);
        Callable.From(() => ApplyColoredModName(__instance, modId)).CallDeferred();

        if (__instance.GetNodeOrNull<HBoxContainer>("RowCustomControls") != null) return;

        var container = new HBoxContainer { Name = "RowCustomControls" };
        __instance.AddChild(container);

        if (ProfileManager.ModGameplayImpactCache.TryGetValue(modId, out bool affectsGameplay) && affectsGameplay)
            AddGameplayImpactIndicator(container);

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
            string previousVisibleGroup = ModdingGroupStateOps.GetAssignedGroup(modId);
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

                ModdingGroupStateOps.SyncGroupDropdown(groupDropdown, previousVisibleGroup);
                return;
            }

            QueueRefreshGroupsUI(__instance);
        };

        container.AddChild(groupDropdown);

        __instance.Resized += () => UpdateCustomControlsLayout(__instance, container, groupDropdown);
        Callable.From(() => UpdateCustomControlsLayout(__instance, container, groupDropdown)).CallDeferred();
    }

    private static void AddGameplayImpactIndicator(HBoxContainer container)
    {
        var slot = new CenterContainer
        {
            Name = "GameplayImpactIndicator",
            TooltipText = "This mod affects gameplay.",
            CustomMinimumSize = new Vector2(
                ModdingScreenConstants.RowGameplayIndicatorSlotWidth,
                ModdingScreenConstants.RowButtonSize)
        };

        var marker = new ColorRect
        {
            Color = new Color(1f, 0.67f, 0.36f, 0.95f),
            CustomMinimumSize = new Vector2(
                ModdingScreenConstants.RowGameplayIndicatorSize,
                ModdingScreenConstants.RowGameplayIndicatorSize),
            MouseFilter = Control.MouseFilterEnum.Ignore
        };
        slot.AddChild(marker);
        container.AddChild(slot);
    }

    private static void ApplyColoredModName(NModMenuRow row, string modId)
    {
        string displayName = row.Mod?.manifest?.name ?? modId;
        if (string.IsNullOrWhiteSpace(displayName))
            displayName = modId;

        var styleTags = BuildStyleTags(modId);
        if (!ModNameStyleRules.TryBuildBbCode(modId, displayName, styleTags, ProfileManager.ModNameStyles, out string bbCode))
            return;

        var nameNode = FindNameTextNode(row, displayName);
        if (nameNode == null)
            return;

        if (ModNameStyleRules.TryBuildSimpleColor(modId, displayName, styleTags, ProfileManager.ModNameStyles, out string simpleColor) &&
            TryParseColor(simpleColor, out Color labelColor))
        {
            ApplySimpleNameColor(nameNode, displayName, labelColor);
            return;
        }

        if (nameNode is RichTextLabel richNameLabel)
        {
            richNameLabel.BbcodeEnabled = true;
            richNameLabel.ParseBbcode(bbCode);
            return;
        }

        if (FindColoredNameLabel(row) is { } existing)
        {
            existing.BbcodeEnabled = true;
            existing.ParseBbcode(bbCode);
            return;
        }

        if (nameNode.GetParent() is not Control parent)
            return;

        var richLabel = new RichTextLabel
        {
            Name = "BetterModMenuColoredName",
            BbcodeEnabled = true,
            FitContent = true,
            ScrollActive = false,
            AutowrapMode = TextServer.AutowrapMode.Off,
            MouseFilter = Control.MouseFilterEnum.Ignore,
            CustomMinimumSize = nameNode.GetCombinedMinimumSize(),
            SizeFlagsHorizontal = nameNode.SizeFlagsHorizontal,
            SizeFlagsVertical = nameNode.SizeFlagsVertical,
            TooltipText = nameNode.TooltipText
        };
        ApplyRichNameTheme(nameNode, richLabel);
        richLabel.ParseBbcode(bbCode);

        parent.AddChild(richLabel);
        parent.MoveChild(richLabel, nameNode.GetIndex());
        nameNode.Visible = false;
    }

    private static void ApplySimpleNameColor(Control nameNode, string displayName, Color color)
    {
        if (nameNode is Label label)
        {
            label.Text = displayName;
            label.AddThemeColorOverride("font_color", color);
        }
        else if (nameNode is RichTextLabel richTextLabel)
        {
            richTextLabel.BbcodeEnabled = false;
            richTextLabel.Text = displayName;
            richTextLabel.AddThemeColorOverride("default_color", color);
        }
    }

    private static void ApplyRichNameTheme(Control source, RichTextLabel target)
    {
        if (source is Label label)
        {
            target.AddThemeFontOverride("normal_font", label.GetThemeFont("font"));
            target.AddThemeFontSizeOverride("normal_font_size", label.GetThemeFontSize("font_size"));
            target.AddThemeColorOverride("default_color", label.GetThemeColor("font_color"));
            target.AddThemeColorOverride("font_shadow_color", label.GetThemeColor("font_shadow_color"));
            target.AddThemeConstantOverride("shadow_offset_x", label.GetThemeConstant("shadow_offset_x"));
            target.AddThemeConstantOverride("shadow_offset_y", label.GetThemeConstant("shadow_offset_y"));
            target.AddThemeConstantOverride("shadow_outline_size", label.GetThemeConstant("shadow_outline_size"));
        }
        else if (source is RichTextLabel richTextLabel)
        {
            target.AddThemeFontOverride("normal_font", richTextLabel.GetThemeFont("normal_font"));
            target.AddThemeFontSizeOverride("normal_font_size", richTextLabel.GetThemeFontSize("normal_font_size"));
            target.AddThemeColorOverride("default_color", richTextLabel.GetThemeColor("default_color"));
            target.AddThemeColorOverride("font_shadow_color", richTextLabel.GetThemeColor("font_shadow_color"));
            target.AddThemeConstantOverride("shadow_offset_x", richTextLabel.GetThemeConstant("shadow_offset_x"));
            target.AddThemeConstantOverride("shadow_offset_y", richTextLabel.GetThemeConstant("shadow_offset_y"));
            target.AddThemeConstantOverride("shadow_outline_size", richTextLabel.GetThemeConstant("shadow_outline_size"));
        }
    }

    private static IEnumerable<string> BuildStyleTags(string modId)
    {
        ProfileManager.BuildWorkshopTagCacheIfNeeded();
        if (!ProfileManager.ModWorkshopTagsCache.TryGetValue(modId, out var tags))
            yield break;

        foreach (string tag in tags)
            yield return tag;
    }

    private static bool TryParseColor(string value, out Color color)
    {
        string normalized = value.StartsWith("#", StringComparison.Ordinal) ? value : "#" + value;
        if (Color.HtmlIsValid(normalized))
        {
            color = Color.FromHtml(normalized);
            return true;
        }

        color = default;
        return false;
    }

    private static RichTextLabel? FindColoredNameLabel(Node root)
    {
        foreach (Node child in root.GetChildren())
        {
            if (child is RichTextLabel richTextLabel &&
                string.Equals(richTextLabel.Name.ToString(), "BetterModMenuColoredName", StringComparison.Ordinal))
                return richTextLabel;

            var nested = FindColoredNameLabel(child);
            if (nested != null)
                return nested;
        }

        return null;
    }

    private static Control? FindNameTextNode(Node root, string displayName)
    {
        foreach (Node child in root.GetChildren())
        {
            if (child is Label label && string.Equals(label.Text, displayName, StringComparison.Ordinal))
                return label;

            if (child is RichTextLabel richTextLabel && string.Equals(richTextLabel.Text, displayName, StringComparison.Ordinal))
                return richTextLabel;

            var nested = FindNameTextNode(child, displayName);
            if (nested != null)
                return nested;
        }

        return null;
    }

    private static void UpdateCustomControlsLayout(NModMenuRow row, HBoxContainer container, OptionButton groupDropdown)
    {
        if (!GodotObject.IsInstanceValid(row) || !GodotObject.IsInstanceValid(container))
            return;

        groupDropdown.CustomMinimumSize = new Vector2(ModdingScreenConstants.RowDropdownWidth, ModdingScreenConstants.ToolbarControlHeight);

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
