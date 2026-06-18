using System;
using System.Collections.Generic;

namespace BetterModMenu.Patches;

internal static class ModdingGroupRules
{
    public static bool CanAdd(IReadOnlyCollection<string> existingGroups, string groupName, out string trimmedName)
    {
        trimmedName = groupName.Trim();
        return !string.IsNullOrEmpty(trimmedName) &&
            !string.Equals(trimmedName, ModdingScreenConstants.UnassignedGroup, StringComparison.Ordinal) &&
            !existingGroups.Contains(trimmedName);
    }

    public static bool CanRename(IReadOnlyCollection<string> existingGroups, string oldName, string newName, out string trimmedName, out bool unchanged)
    {
        trimmedName = newName.Trim();
        unchanged = false;
        if (string.IsNullOrEmpty(trimmedName) || string.Equals(trimmedName, ModdingScreenConstants.UnassignedGroup, StringComparison.Ordinal))
            return false;

        if (string.Equals(trimmedName, oldName, StringComparison.Ordinal))
        {
            unchanged = true;
            return true;
        }

        return !existingGroups.Contains(trimmedName);
    }
}
