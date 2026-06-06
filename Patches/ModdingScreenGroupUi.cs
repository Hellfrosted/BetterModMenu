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
        SortRows(rows, modOrder);
        var assignedGroups = ModdingScreenStateOps.BuildAssignedGroupLookup(rows.Select(row => row.Mod?.manifest?.id ?? ""));
        var groups = GroupRows(rows, assignedGroups);
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

    private static void SortRows(List<NModMenuRow> rows, IReadOnlyDictionary<string, int> modOrder)
    {
        rows.Sort((left, right) =>
        {
            int leftIndex = GetModIndex(left);
            int rightIndex = GetModIndex(right);
            return leftIndex.CompareTo(rightIndex);
            
            int GetModIndex(NModMenuRow row)
            {
                string modId = row.Mod?.manifest?.id ?? "";
                return !string.IsNullOrEmpty(modId) && modOrder.TryGetValue(modId, out int index)
                    ? index
                    : int.MaxValue;
            }
        });
    }

    private static Dictionary<string, List<NModMenuRow>> GroupRows(List<NModMenuRow> rows, IReadOnlyDictionary<string, string> assignedGroups)
    {
        var groups = new Dictionary<string, List<NModMenuRow>>
        {
            [ModdingScreenConstants.UnassignedGroup] = new()
        };

        foreach (var groupName in ProfileManager.CustomGroups)
            groups[groupName] = new();

        foreach (var row in rows)
        {
            string modId = row.Mod?.manifest?.id ?? "";
            if (string.IsNullOrEmpty(modId))
                continue;

            string groupName = ModdingScreenConstants.UnassignedGroup;
            if (assignedGroups.TryGetValue(modId, out string? assignedGroup) && assignedGroup != null)
                groupName = assignedGroup;

            groups[groupName].Add(row);

            var dropdown = row.GetNodeOrNull<OptionButton>(ModdingScreenConstants.GroupDropdownPath);
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
        var groupCounts = groups.ToDictionary(entry => entry.Key, entry => entry.Value.Count);
        foreach (var groupName in ProfileStateRules.BuildVisibleGroupOrder(groupCounts, ProfileManager.CustomGroups, ModdingScreenConstants.UnassignedGroup))
        {
            if (!groups.TryGetValue(groupName, out var groupRows))
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
        var header = new HBoxContainer
        {
            Name = "ModGroupHeader_" + groupName,
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
        };
        ModdingScreenVanillaStyle.ApplyGroupHeader(header);

        var collapseBtn = new Button
        {
            Text = isCollapsed ? "► " + groupName : "▼ " + groupName,
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
        var toggleAllBtn = new Button { Text = allEnabled ? "Disable All" : "Enable All", Disabled = !hasRows };
        ModdingScreenVanillaStyle.ApplyButton(toggleAllBtn);
        toggleAllBtn.Pressed += () => toggleAllInGroup(groupName, !allEnabled);
        header.AddChild(toggleAllBtn);

        if (groupName == ModdingScreenConstants.UnassignedGroup)
        {
            AddHeaderScrollbarSpacer(header);
            return header;
        }

        var renameBtn = new Button { Text = "Rename" };
        ModdingScreenVanillaStyle.ApplyButton(renameBtn);
        renameBtn.Pressed += () => renameGroup(groupName);
        header.AddChild(renameBtn);

        header.AddChild(new Control { CustomMinimumSize = new Vector2(10, 0) });

        var upBtn = new Button { Text = "^" };
        ModdingScreenVanillaStyle.ApplySmallButton(upBtn);
        upBtn.Pressed += () => moveGroup(groupName, -1);
        header.AddChild(upBtn);

        header.AddChild(new Control { CustomMinimumSize = new Vector2(10, 0) });

        var downBtn = new Button { Text = "v" };
        ModdingScreenVanillaStyle.ApplySmallButton(downBtn);
        downBtn.Pressed += () => moveGroup(groupName, 1);
        header.AddChild(downBtn);

        header.AddChild(new Control { CustomMinimumSize = new Vector2(10, 0) });

        var deleteBtn = new Button { Text = "Del" };
        ModdingScreenVanillaStyle.ApplyButton(deleteBtn);
        deleteBtn.Pressed += () =>
        {
            if (ModdingScreenStateOps.DeleteGroup(groupName))
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
