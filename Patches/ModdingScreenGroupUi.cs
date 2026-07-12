using System;
using System.Collections.Generic;
using Godot;
using BetterModMenu.Data;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.Screens.ModdingScreen;
using MegaCrit.Sts2.Core.Saves;

namespace BetterModMenu.Patches;

internal static class ModdingScreenGroupUi
{
    public static void RefreshGroupsUI(
        Control modRowContainer,
        List<Node> generatedGroupNodes,
        string searchQuery,
        Dictionary<string, ModSearchResult> searchResults,
        Action refreshGroupsUI,
        Action<string> renameGroup,
        Action<string, int> moveGroup,
        Action<string, bool> toggleAllInGroup)
    {
        var rows = CollectRowsAndClearGeneratedNodes(modRowContainer, generatedGroupNodes);
        var modOrder = BuildModOrderLookup();
        var assignedGroups = ModdingGroupStateOps.BuildAssignedGroupLookup(rows.Select(row => row.Mod?.manifest?.id ?? ""));
        if (!string.IsNullOrWhiteSpace(searchQuery))
        {
            ApplySearchLayout(modRowContainer, rows, assignedGroups, searchQuery, searchResults);
            return;
        }

        searchResults.Clear();
        var layout = ModdingScreenGroupLayoutBuilder.Build(
            rows.Select(row => new ModdingScreenGroupLayoutRow<NModMenuRow>(row, row.Mod?.manifest?.id ?? "")),
            assignedGroups,
            ProfileManager.CustomGroups,
            ProfileManager.CollapsedGroups,
            modOrder,
            ModdingScreenConstants.UnassignedGroup);
        SyncGroupDropdowns(layout);
        BuildGroupHeaders(modRowContainer, generatedGroupNodes, layout.Groups, refreshGroupsUI, renameGroup, moveGroup, toggleAllInGroup);
    }

    private static void ApplySearchLayout(
        Control container,
        IReadOnlyList<NModMenuRow> rows,
        IReadOnlyDictionary<string, string> assignedGroups,
        string searchQuery,
        Dictionary<string, ModSearchResult> searchResults)
    {
        searchResults.Clear();
        var rowsByModId = new Dictionary<string, NModMenuRow>(StringComparer.OrdinalIgnoreCase);
        foreach (var row in rows)
        {
            string modId = row.Mod?.manifest?.id ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(modId) && !rowsByModId.ContainsKey(modId))
                rowsByModId[modId] = row;
        }
        var documents = rowsByModId.Values.Select(row => BuildSearchDocument(row, assignedGroups));
        var results = ModSearchRules.Search(documents, searchQuery);

        int index = 0;
        foreach (var result in results)
        {
            if (!rowsByModId.TryGetValue(result.ModId, out var row))
                continue;

            searchResults[result.ModId] = result;
            row.Visible = true;
            container.MoveChild(row, index++);
            SyncSearchModeGroupDropdown(row, assignedGroups);
        }

        foreach (var row in rows)
        {
            string modId = row.Mod?.manifest?.id ?? string.Empty;
            row.Visible = !string.IsNullOrWhiteSpace(modId) && searchResults.ContainsKey(modId);
        }
    }

    private static ModSearchDocument BuildSearchDocument(
        NModMenuRow row,
        IReadOnlyDictionary<string, string> assignedGroups)
    {
        var manifest = row.Mod?.manifest;
        string modId = manifest?.id ?? string.Empty;
        ModAnnotation annotation = ProfileManager.GetModAnnotation(modId);
        SteamWorkshopLinkResolver.TryGetPublishedFileId(row.Mod?.path, out string workshopId);
        SteamWorkshopLinkResolver.TryGetWorkshopUrl(row.Mod?.path, out string workshopUrl);
        return new ModSearchDocument(modId, manifest?.name ?? modId)
        {
            Author = manifest?.author ?? string.Empty,
            Description = manifest?.description ?? string.Empty,
            Alias = annotation.Alias,
            Notes = annotation.Notes,
            Version = manifest?.version ?? string.Empty,
            Dependencies = manifest?.dependencies?.Select(dependency => dependency.id).Where(id => !string.IsNullOrWhiteSpace(id)).ToArray() ?? Array.Empty<string>(),
            Group = assignedGroups.TryGetValue(modId, out string? group) && !string.IsNullOrWhiteSpace(group)
                ? GetSearchGroupName(group)
                : GetSearchGroupName(ModdingScreenConstants.UnassignedGroup),
            Enabled = IsRowEnabled(row),
            WorkshopId = workshopId,
            WorkshopUrl = workshopUrl
        };
    }

    private static string GetSearchGroupName(string groupName)
    {
        return ModdingGroupStateOps.GetDisplayGroupName(groupName);
    }

    private static bool IsRowEnabled(NModMenuRow row)
    {
        var tick = row.GetNodeOrNull<NTickbox>(ModdingScreenConstants.TickboxPath);
        return tick == null || (bool)tick.Get("IsTicked");
    }

    private static void SyncSearchModeGroupDropdown(NModMenuRow row, IReadOnlyDictionary<string, string> assignedGroups)
    {
        var dropdown = row.GetNodeOrNull<OptionButton>(ModdingScreenConstants.GroupDropdownPath);
        string modId = row.Mod?.manifest?.id ?? string.Empty;
        if (dropdown != null && assignedGroups.TryGetValue(modId, out string? group))
            ModdingGroupStateOps.SyncGroupDropdown(dropdown, group ?? ModdingScreenConstants.UnassignedGroup);
    }

    private static List<NModMenuRow> CollectRowsAndClearGeneratedNodes(Control container, List<Node> generatedGroupNodes)
    {
        var rows = new List<NModMenuRow>();
        foreach (Node child in container.GetChildren())
        {
            if (child is NModMenuRow row)
            {
                rows.Add(row);
            }
            else if (child.Name.ToString().StartsWith("ModGroup") && !generatedGroupNodes.Contains(child))
            {
                generatedGroupNodes.Add(child);
            }
        }

        foreach (var child in generatedGroupNodes)
        {
            if (!GodotObject.IsInstanceValid(child) || child.GetParent() != container)
                continue;

            container.RemoveChild(child);
            child.QueueFree();
        }
        generatedGroupNodes.Clear();
        return rows;
    }

    private static Dictionary<string, int> BuildModOrderLookup()
    {
        var options = SaveManager.Instance?.SettingsSave?.ModSettings;
        var modOrder = new Dictionary<string, int>(StringComparer.Ordinal);
        if (options?.ModList == null)
            return modOrder;

        for (int index = 0; index < options.ModList.Count; index++)
        {
            string modId = options.ModList[index].Id ?? "";
            if (!string.IsNullOrWhiteSpace(modId) && !modOrder.ContainsKey(modId))
                modOrder[modId] = index;
        }

        return modOrder;
    }

    private static void SyncGroupDropdowns(ModdingScreenGroupLayout<NModMenuRow> layout)
    {
        foreach (var group in layout.Groups)
        {
            foreach (var row in group.Rows)
            {
                var dropdown = row.Item.GetNodeOrNull<OptionButton>(ModdingScreenConstants.GroupDropdownPath);
                if (dropdown != null)
                    ModdingGroupStateOps.SyncGroupDropdown(dropdown, group.Name);
            }
        }
    }

    private static void BuildGroupHeaders(
        Control container,
        List<Node> generatedGroupNodes,
        IReadOnlyList<ModdingScreenGroupLayoutGroup<NModMenuRow>> groups,
        Action refreshGroupsUI,
        Action<string> renameGroup,
        Action<string, int> moveGroup,
        Action<string, bool> toggleAllInGroup)
    {
        int index = 0;
        foreach (var group in groups)
        {
            var groupRows = group.Rows.Select(row => row.Item).ToList();

            var separator = new ColorRect
            {
                Name = "ModGroupSep_" + group.Name,
                CustomMinimumSize = new Vector2(0, 4),
                Color = new Color(0, 0, 0, 0)
            };
            container.AddChild(separator);
            container.MoveChild(separator, index++);
            generatedGroupNodes.Add(separator);

            var header = BuildGroupHeader(group.Name, groupRows, group.IsCollapsed, refreshGroupsUI, renameGroup, moveGroup, toggleAllInGroup);
            generatedGroupNodes.Add(header);
            container.AddChild(header);
            container.MoveChild(header, index++);

            foreach (var row in groupRows)
            {
                container.MoveChild(row, index++);
                row.Visible = !group.IsCollapsed;
            }
        }
    }

    private static HBoxContainer BuildGroupHeader(
        string groupName,
        List<NModMenuRow> groupRows,
        bool isCollapsed,
        Action refreshGroupsUI,
        Action<string> renameGroup,
        Action<string, int> moveGroup,
        Action<string, bool> toggleAllInGroup)
    {
        var header = new HBoxContainer
        {
            Name = "ModGroupHeader_" + groupName,
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
        };
        ModdingScreenVanillaStyle.ApplyGroupHeader(header);

        var collapseBtn = BuildCollapseButton(groupName, isCollapsed);
        collapseBtn.Pressed += () =>
        {
            ToggleCollapsedGroup(groupName);
            refreshGroupsUI();
        };
        header.AddChild(collapseBtn);

        bool hasRows = groupRows.Count > 0;
        var enableAllBtn = BuildIconButton(
            ModdingScreenIcon.ListChecks,
            ModdingScreenText.Get(BmmText.GroupEnableAllTooltip, "Turn on every mod in this group."),
            () => toggleAllInGroup(groupName, true),
            !hasRows);
        header.AddChild(enableAllBtn);

        var disableAllBtn = BuildIconButton(
            ModdingScreenIcon.ListX,
            ModdingScreenText.Get(BmmText.GroupDisableAllTooltip, "Turn off every mod in this group."),
            () => toggleAllInGroup(groupName, false),
            !hasRows);
        header.AddChild(disableAllBtn);

        if (groupName == ModdingScreenConstants.UnassignedGroup)
        {
            AddHeaderScrollbarSpacer(header);
            return header;
        }

        var renameBtn = BuildIconButton(
            ModdingScreenIcon.PencilLine,
            ModdingScreenText.Get(BmmText.GroupRenameTooltip, "Rename this group. Mods already in it stay in it."),
            () => renameGroup(groupName));
        header.AddChild(renameBtn);

        var upBtn = BuildIconButton(
            ModdingScreenIcon.ChevronUp,
            ModdingScreenText.Get(BmmText.GroupMoveUpTooltip, "Move this group higher in the mod list."),
            () => moveGroup(groupName, -1));
        header.AddChild(upBtn);

        var downBtn = BuildIconButton(
            ModdingScreenIcon.ChevronDown,
            ModdingScreenText.Get(BmmText.GroupMoveDownTooltip, "Move this group lower in the mod list."),
            () => moveGroup(groupName, 1));
        header.AddChild(downBtn);

        var deleteBtn = BuildIconButton(
            ModdingScreenIcon.Trash,
            ModdingScreenText.Get(BmmText.GroupDeleteTooltip, "Delete this group label. The mods stay installed and move back to Unassigned."),
            () =>
        {
            if (ModdingGroupStateOps.DeleteGroup(groupName))
                refreshGroupsUI();
        });
        header.AddChild(deleteBtn);

        AddHeaderScrollbarSpacer(header);

        return header;
    }

    private static Button BuildCollapseButton(string groupName, bool isCollapsed)
    {
        var button = new Button
        {
            TooltipText = isCollapsed
                ? ModdingScreenText.Get(BmmText.GroupShowTooltip, "Show the mods in this group.")
                : ModdingScreenText.Get(BmmText.GroupHideTooltip, "Hide the mods in this group without changing whether they are enabled."),
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
        };
        ModdingScreenVanillaStyle.ApplyButton(button);

        var content = new HBoxContainer
        {
            MouseFilter = Control.MouseFilterEnum.Ignore
        };
        content.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        content.OffsetLeft = 8;
        content.OffsetRight = -8;
        content.AddThemeConstantOverride("separation", 4);

        var icon = new TextureRect
        {
            Texture = ModdingScreenIcons.Get(isCollapsed ? ModdingScreenIcon.ListChevronsUpDown : ModdingScreenIcon.ListChevronsDownUp),
            CustomMinimumSize = new Vector2(ModdingScreenConstants.GroupHeaderIconSize, ModdingScreenConstants.GroupHeaderIconSize),
            StretchMode = TextureRect.StretchModeEnum.KeepCentered,
            MouseFilter = Control.MouseFilterEnum.Ignore
        };
        content.AddChild(icon);

        var label = new Label
        {
            Text = ModdingGroupStateOps.GetDisplayGroupName(groupName),
            ClipText = true,
            TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis,
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            MouseFilter = Control.MouseFilterEnum.Ignore
        };
        ModdingScreenVanillaStyle.ApplyLabel(label);
        content.AddChild(label);

        button.AddChild(content);
        return button;
    }

    private static Button BuildIconButton(ModdingScreenIcon icon, string tooltip, Action pressed, bool disabled = false)
    {
        var button = new Button
        {
            Icon = ModdingScreenIcons.Get(icon),
            TooltipText = tooltip,
            Disabled = disabled
        };
        ModdingScreenVanillaStyle.ApplyIconButton(button);
        button.Pressed += pressed;
        return button;
    }

    private static void AddHeaderScrollbarSpacer(HBoxContainer header)
    {
        header.AddChild(new Control
        {
            CustomMinimumSize = new Vector2(ModdingScreenConstants.GroupHeaderTrailingPadding, 0),
            MouseFilter = Control.MouseFilterEnum.Ignore
        });
    }

    private static void ToggleCollapsedGroup(string groupName)
    {
        bool wasCollapsed = ProfileManager.CollapsedGroups.Contains(groupName);
        if (wasCollapsed)
            ProfileManager.CollapsedGroups.Remove(groupName);
        else
            ProfileManager.CollapsedGroups.Add(groupName);

        if (ProfileManager.SaveInMemoryState())
            return;

        if (wasCollapsed)
            ProfileManager.CollapsedGroups.Add(groupName);
        else
            ProfileManager.CollapsedGroups.Remove(groupName);
    }
}
