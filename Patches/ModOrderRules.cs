using System;
using System.Collections.Generic;

namespace BetterModMenu.Patches;

internal readonly record struct ModOrderMove(int FromIndex, int InsertIndex);

internal static class ModOrderRules
{
    private const string UnassignedGroup = "Unassigned";

    public static bool TryBuildMove(
        IReadOnlyList<string> modIds,
        IReadOnlyDictionary<string, string> assignedGroups,
        string modId,
        int direction,
        out ModOrderMove move)
    {
        move = default;
        if (string.IsNullOrWhiteSpace(modId) || direction == 0)
            return false;

        int fromIndex = IndexOf(modIds, modId);
        if (fromIndex < 0)
            return false;

        string groupName = GetGroupName(assignedGroups, modId);
        int step = Math.Sign(direction);
        for (int index = fromIndex + step; index >= 0 && index < modIds.Count; index += step)
        {
            if (string.Equals(GetGroupName(assignedGroups, modIds[index]), groupName, StringComparison.Ordinal))
            {
                move = new ModOrderMove(fromIndex, index);
                return true;
            }
        }

        return false;
    }

    private static int IndexOf(IReadOnlyList<string> modIds, string modId)
    {
        for (int index = 0; index < modIds.Count; index++)
        {
            if (string.Equals(modIds[index], modId, StringComparison.Ordinal))
                return index;
        }

        return -1;
    }

    private static string GetGroupName(IReadOnlyDictionary<string, string> assignedGroups, string modId)
    {
        return !string.IsNullOrWhiteSpace(modId) &&
            assignedGroups.TryGetValue(modId, out string? groupName) &&
            !string.IsNullOrWhiteSpace(groupName)
                ? groupName
                : UnassignedGroup;
    }
}
