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
    private const string UnassignedGroup = "Unassigned";

    public static void RefreshGroupsUI(
        Control modRowContainer,
        List<Node> generatedGroupNodes,
        Action refreshGroupsUI,
        Action<string> renameGroup,
        Action<string, int> moveGroup,
        Action<string, bool> toggleAllInGroup)
    {
        var rows = CollectRowsAndClearGeneratedNodes(modRowContainer, generatedGroupNodes);
        SortRows(rows);
        var groups = GroupRows(rows);
        BuildGroupHeaders(modRowContainer, generatedGroupNodes, groups, refreshGroupsUI, renameGroup, moveGroup, toggleAllInGroup);
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

    private static void SortRows(List<NModMenuRow> rows)
    {
        var options = SaveManager.Instance.SettingsSave.ModSettings;
        if (options == null || options.ModList == null)
            return;

        rows.Sort((left, right) =>
        {
            int leftIndex = GetModIndex(left);
            int rightIndex = GetModIndex(right);
            return leftIndex.CompareTo(rightIndex);
            
            int GetModIndex(NModMenuRow row)
            {
                int index = row.Mod?.manifest?.id != null
                    ? options.ModList.FindIndex(mod => mod.Id == row.Mod.manifest.id)
                    : 9999;
                return index == -1 ? 9999 : index;
            }
        });
    }

    private static Dictionary<string, List<NModMenuRow>> GroupRows(List<NModMenuRow> rows)
    {
        var groups = new Dictionary<string, List<NModMenuRow>>
        {
            [UnassignedGroup] = new()
        };

        foreach (var groupName in ProfileManager.CustomGroups)
            groups[groupName] = new();

        foreach (var row in rows)
        {
            string groupName = UnassignedGroup;
            if (row.Mod?.manifest != null)
            {
                string modId = row.Mod.manifest.id ?? "";
                groupName = ModdingScreenStateOps.GetAssignedGroup(modId);
            }

            groups[groupName].Add(row);

            var dropdown = row.GetNodeOrNull<OptionButton>("RowCustomControls/GroupDropdown");
            if (dropdown != null)
                ModdingScreenStateOps.SyncGroupDropdown(dropdown, groupName);
        }

        return groups;
    }

    private static void BuildGroupHeaders(
        Control container,
        List<Node> generatedGroupNodes,
        Dictionary<string, List<NModMenuRow>> groups,
        Action refreshGroupsUI,
        Action<string> renameGroup,
        Action<string, int> moveGroup,
        Action<string, bool> toggleAllInGroup)
    {
        int index = 0;
        var orderedGroups = new List<string> { UnassignedGroup };
        orderedGroups.AddRange(ProfileManager.CustomGroups);

        foreach (var groupName in orderedGroups)
        {
            if (!groups.TryGetValue(groupName, out var groupRows))
                continue;

            if (groupName == UnassignedGroup && groupRows.Count == 0)
                continue;

            bool isCollapsed = ProfileManager.CollapsedGroups.Contains(groupName);

            var separator = new ColorRect
            {
                Name = "ModGroupSep_" + groupName,
                CustomMinimumSize = new Vector2(0, 4),
                Color = new Color(0, 0, 0, 0)
            };
            container.AddChild(separator);
            container.MoveChild(separator, index++);
            generatedGroupNodes.Add(separator);

            var header = BuildGroupHeader(groupName, groupRows, isCollapsed, refreshGroupsUI, renameGroup, moveGroup, toggleAllInGroup);
            generatedGroupNodes.Add(header);
            container.AddChild(header);
            container.MoveChild(header, index++);

            foreach (var row in groupRows)
            {
                container.MoveChild(row, index++);
                row.Visible = !isCollapsed;
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
        var header = new HBoxContainer { Name = "ModGroupHeader_" + groupName };

        var collapseBtn = new Button
        {
            Text = isCollapsed ? "► " + groupName : "▼ " + groupName,
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
        };
        collapseBtn.Pressed += () =>
        {
            ToggleCollapsedGroup(groupName);
            refreshGroupsUI();
        };
        header.AddChild(collapseBtn);

        bool hasRows = groupRows.Count > 0;
        bool allEnabled = AreAllRowsEnabled(groupRows);
        var toggleAllBtn = new Button { Text = allEnabled ? "Disable All" : "Enable All", Disabled = !hasRows };
        toggleAllBtn.Pressed += () => toggleAllInGroup(groupName, !allEnabled);
        header.AddChild(toggleAllBtn);

        if (groupName == UnassignedGroup)
            return header;

        var renameBtn = new Button { Text = "Rename" };
        renameBtn.Pressed += () => renameGroup(groupName);
        header.AddChild(renameBtn);

        header.AddChild(new Control { CustomMinimumSize = new Vector2(10, 0) });

        var upBtn = new Button { Text = "^" };
        upBtn.Pressed += () => moveGroup(groupName, -1);
        header.AddChild(upBtn);

        header.AddChild(new Control { CustomMinimumSize = new Vector2(10, 0) });

        var downBtn = new Button { Text = "v" };
        downBtn.Pressed += () => moveGroup(groupName, 1);
        header.AddChild(downBtn);

        header.AddChild(new Control { CustomMinimumSize = new Vector2(10, 0) });

        var deleteBtn = new Button { Text = "Del" };
        deleteBtn.Pressed += () =>
        {
            if (ModdingScreenStateOps.DeleteGroup(groupName))
                refreshGroupsUI();
        };
        header.AddChild(deleteBtn);

        return header;
    }

    private static bool AreAllRowsEnabled(List<NModMenuRow> groupRows)
    {
        if (groupRows.Count == 0)
            return false;

        foreach (var row in groupRows)
        {
            var tick = row.GetNodeOrNull<NTickbox>("Tickbox");
            if (tick != null && !(bool)tick.Get("IsTicked"))
                return false;
        }

        return true;
    }

    private static void ToggleCollapsedGroup(string groupName)
    {
        if (ProfileManager.CollapsedGroups.Contains(groupName))
            ProfileManager.CollapsedGroups.Remove(groupName);
        else
            ProfileManager.CollapsedGroups.Add(groupName);

        ProfileManager.SaveInMemoryState();
    }
}
