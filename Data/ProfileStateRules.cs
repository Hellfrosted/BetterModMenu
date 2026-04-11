using System;
using System.Collections.Generic;
using System.Linq;

namespace BetterModMenu.Data;

public static class ProfileStateRules
{
    public static bool NormalizeGroups(
        IReadOnlyCollection<string> customGroups,
        Dictionary<string, string> modGroups,
        HashSet<string> collapsedGroups,
        IEnumerable<string> installedModIds,
        string unassignedGroupName)
    {
        var installedMods = new HashSet<string>(
            installedModIds.Where(id => !string.IsNullOrWhiteSpace(id)),
            StringComparer.Ordinal);
        var validGroups = new HashSet<string>(customGroups, StringComparer.Ordinal);
        bool changed = false;

        foreach (var modId in modGroups.Keys.ToList())
        {
            if (!installedMods.Contains(modId) ||
                !modGroups.TryGetValue(modId, out string? groupName) ||
                string.IsNullOrWhiteSpace(groupName) ||
                !validGroups.Contains(groupName))
            {
                modGroups.Remove(modId);
                changed = true;
            }
        }

        if (collapsedGroups.Remove(unassignedGroupName))
            changed = true;

        foreach (var collapsedGroup in collapsedGroups.ToList())
        {
            if (!validGroups.Contains(collapsedGroup))
            {
                collapsedGroups.Remove(collapsedGroup);
                changed = true;
            }
        }

        return changed;
    }

    public static List<string> BuildVisibleGroupOrder(
        IReadOnlyDictionary<string, int> groupCounts,
        IReadOnlyList<string> customGroups,
        string unassignedGroupName)
    {
        var visibleGroups = new List<string>();

        if (groupCounts.TryGetValue(unassignedGroupName, out int unassignedCount) && unassignedCount > 0)
            visibleGroups.Add(unassignedGroupName);

        foreach (var groupName in customGroups)
        {
            if (groupCounts.ContainsKey(groupName))
                visibleGroups.Add(groupName);
        }

        return visibleGroups;
    }
}
