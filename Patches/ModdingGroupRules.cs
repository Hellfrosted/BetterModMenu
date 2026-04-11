using System;
using System.Collections.Generic;

namespace BetterModMenu.Patches;

internal enum GroupNameValidationResult
{
    Invalid,
    Duplicate,
    Unchanged,
    Valid
}

internal static class ModdingGroupRules
{
    public static bool CanAdd(IReadOnlyCollection<string> existingGroups, string groupName, out string trimmedName)
    {
        trimmedName = groupName.Trim();
        return !string.IsNullOrEmpty(trimmedName) &&
            !string.Equals(trimmedName, ModdingScreenConstants.UnassignedGroup, StringComparison.Ordinal) &&
            !existingGroups.Contains(trimmedName);
    }

    public static GroupNameValidationResult ValidateRename(IReadOnlyCollection<string> existingGroups, string oldName, string newName, out string trimmedName)
    {
        trimmedName = newName.Trim();
        if (string.IsNullOrEmpty(trimmedName) || string.Equals(trimmedName, ModdingScreenConstants.UnassignedGroup, StringComparison.Ordinal))
            return GroupNameValidationResult.Invalid;

        if (string.Equals(trimmedName, oldName, StringComparison.Ordinal))
            return GroupNameValidationResult.Unchanged;

        return existingGroups.Contains(trimmedName)
            ? GroupNameValidationResult.Duplicate
            : GroupNameValidationResult.Valid;
    }
}
