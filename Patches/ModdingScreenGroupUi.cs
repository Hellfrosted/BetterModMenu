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
        Action refreshGroupsUI,
        Action<string> renameGroup,
        Action<string, int> moveGroup,
        Action<string, bool> toggleAllInGroup)
    {
        var rows = CollectRowsAndClearGeneratedNodes(modRowContainer, generatedGroupNodes);
        var modOrder = BuildModOrderLookup();
        var assignedGroups = ModdingGroupStateOps.BuildAssignedGroupLookup(rows.Select(row => row.Mod?.manifest?.id ?? ""));
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

        var collapseBtn = new Button
        {
            Text = isCollapsed ? "► " + groupName : "▼ " + groupName,
            TooltipText = isCollapsed ? "Show the mods in this group." : "Hide the mods in this group without changing whether they are enabled.",
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
        };
        ModdingScreenVanillaStyle.ApplyButton(collapseBtn);
        collapseBtn.Pressed += () =>
        {
            ToggleCollapsedGroup(groupName);
            refreshGroupsUI();
        };
        header.AddChild(collapseBtn);

        bool hasRows = groupRows.Count > 0;
        bool allEnabled = AreAllRowsEnabled(groupRows);
        var toggleAllBtn = new Button
        {
            Text = allEnabled ? "Disable All" : "Enable All",
            TooltipText = allEnabled ? "Turn off every mod in this group." : "Turn on every mod in this group.",
            Disabled = !hasRows
        };
        ModdingScreenVanillaStyle.ApplyButton(toggleAllBtn);
        toggleAllBtn.Pressed += () => toggleAllInGroup(groupName, !allEnabled);
        header.AddChild(toggleAllBtn);

        if (groupName == ModdingScreenConstants.UnassignedGroup)
        {
            AddHeaderScrollbarSpacer(header);
            return header;
        }

        var renameBtn = new Button
        {
            Text = "Rename",
            TooltipText = "Rename this group. Mods already in it stay in it."
        };
        ModdingScreenVanillaStyle.ApplyButton(renameBtn);
        renameBtn.Pressed += () => renameGroup(groupName);
        header.AddChild(renameBtn);

        header.AddChild(new Control { CustomMinimumSize = new Vector2(10, 0) });

        var upBtn = new Button
        {
            Text = "^",
            TooltipText = "Move this group higher in the mod list."
        };
        ModdingScreenVanillaStyle.ApplySmallButton(upBtn);
        upBtn.Pressed += () => moveGroup(groupName, -1);
        header.AddChild(upBtn);

        header.AddChild(new Control { CustomMinimumSize = new Vector2(10, 0) });

        var downBtn = new Button
        {
            Text = "v",
            TooltipText = "Move this group lower in the mod list."
        };
        ModdingScreenVanillaStyle.ApplySmallButton(downBtn);
        downBtn.Pressed += () => moveGroup(groupName, 1);
        header.AddChild(downBtn);

        header.AddChild(new Control { CustomMinimumSize = new Vector2(10, 0) });

        var deleteBtn = new Button
        {
            Text = "Del",
            TooltipText = "Delete this group label. The mods stay installed and move back to Unassigned."
        };
        ModdingScreenVanillaStyle.ApplyButton(deleteBtn);
        deleteBtn.Pressed += () =>
        {
            if (ModdingGroupStateOps.DeleteGroup(groupName))
                refreshGroupsUI();
        };
        header.AddChild(deleteBtn);

        AddHeaderScrollbarSpacer(header);

        return header;
    }

    private static void AddHeaderScrollbarSpacer(HBoxContainer header)
    {
        header.AddChild(new Control
        {
            CustomMinimumSize = new Vector2(ModdingScreenConstants.GroupHeaderScrollbarReserveWidth, 0),
            MouseFilter = Control.MouseFilterEnum.Ignore
        });
    }

    private static bool AreAllRowsEnabled(List<NModMenuRow> groupRows)
    {
        if (groupRows.Count == 0)
            return false;

        foreach (var row in groupRows)
        {
            var tick = row.GetNodeOrNull<NTickbox>(ModdingScreenConstants.TickboxPath);
            if (tick != null && !(bool)tick.Get("IsTicked"))
                return false;
        }

        return true;
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
