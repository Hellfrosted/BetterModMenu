using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using BetterModMenu.Data;

namespace BetterModMenu.Patches;

internal static class ModdingGroupStateOps
{
    public static bool TryAddGroup(string groupName)
    {
        if (!ModdingGroupRules.CanAdd(ProfileManager.CustomGroups, groupName, out string trimmedName))
            return false;

        ProfileManager.CustomGroups.Add(trimmedName);
        if (ProfileManager.SaveInMemoryState())
            return true;

        ProfileManager.CustomGroups.Remove(trimmedName);
        return false;
    }

    public static string GetAssignedGroup(string modId, ISet<string>? validGroups = null)
    {
        if (!string.IsNullOrEmpty(modId) &&
            ProfileManager.ModGroups.TryGetValue(modId, out string? assignedGroup) &&
            assignedGroup != null &&
            (validGroups?.Contains(assignedGroup) ?? ProfileManager.CustomGroups.Contains(assignedGroup)))
        {
            return assignedGroup;
        }

        return ModdingScreenConstants.UnassignedGroup;
    }

    public static Dictionary<string, string> BuildAssignedGroupLookup(IEnumerable<string> modIds)
    {
        var validGroups = new HashSet<string>(ProfileManager.CustomGroups, StringComparer.Ordinal);
        var assignedGroups = new Dictionary<string, string>(StringComparer.Ordinal);

        foreach (string modId in modIds)
        {
            if (!string.IsNullOrWhiteSpace(modId))
                assignedGroups[modId] = GetAssignedGroup(modId, validGroups);
        }

        return assignedGroups;
    }

    public static void SyncGroupDropdown(OptionButton dropdown, string assignedGroup)
    {
        string currentSelection = (dropdown.ItemCount > 0 && dropdown.Selected >= 0)
            ? GetDropdownGroupValue(dropdown, dropdown.Selected)
            : string.Empty;

        if (currentSelection == assignedGroup && DropdownItemsMatchGroups(dropdown))
            return;

        dropdown.Clear();
        dropdown.AddItem(GetDisplayGroupName(ModdingScreenConstants.UnassignedGroup), 0);
        for (int i = 0; i < ProfileManager.CustomGroups.Count; i++)
            dropdown.AddItem(ProfileManager.CustomGroups[i], i + 1);

        int selectedIndex = assignedGroup == ModdingScreenConstants.UnassignedGroup
            ? 0
            : ProfileManager.CustomGroups.IndexOf(assignedGroup) + 1;
        dropdown.Select(selectedIndex);
    }

    public static string GetDisplayGroupName(string groupName)
    {
        return groupName == ModdingScreenConstants.UnassignedGroup
            ? ModdingScreenText.Get(BmmText.GroupUnassigned, "Unassigned")
            : groupName;
    }

    public static string GetDropdownGroupValue(OptionButton dropdown, int itemIndex)
    {
        return itemIndex <= 0
            ? ModdingScreenConstants.UnassignedGroup
            : dropdown.GetItemText(itemIndex);
    }

    private static bool DropdownItemsMatchGroups(OptionButton dropdown)
    {
        if (dropdown.ItemCount != ProfileManager.CustomGroups.Count + 1)
            return false;

        if (dropdown.GetItemText(0) != GetDisplayGroupName(ModdingScreenConstants.UnassignedGroup))
            return false;

        for (int i = 0; i < ProfileManager.CustomGroups.Count; i++)
        {
            if (dropdown.GetItemText(i + 1) != ProfileManager.CustomGroups[i])
                return false;
        }

        return true;
    }

    public static bool TryMoveGroup(string groupName, int direction)
    {
        int currentIndex = ProfileManager.CustomGroups.IndexOf(groupName);
        if (currentIndex == -1)
            return false;

        int newIndex = currentIndex + direction;
        if (newIndex < 0 || newIndex >= ProfileManager.CustomGroups.Count)
            return false;

        var previousGroups = new List<string>(ProfileManager.CustomGroups);
        ProfileManager.CustomGroups.RemoveAt(currentIndex);
        ProfileManager.CustomGroups.Insert(newIndex, groupName);
        if (ProfileManager.SaveInMemoryState())
            return true;

        ProfileManager.CustomGroups = previousGroups;
        return false;
    }

    public static bool TryRenameGroup(string oldName, string newName)
    {
        int index = ProfileManager.CustomGroups.IndexOf(oldName);
        if (index == -1)
            return false;

        if (!ModdingGroupRules.CanRename(ProfileManager.CustomGroups, oldName, newName, out string trimmedName, out bool unchanged))
            return false;

        if (unchanged)
            return true;

        var previousState = CaptureState();
        ProfileManager.CustomGroups[index] = trimmedName;

        var modIds = ProfileManager.ModGroups.Where(entry => entry.Value == oldName).Select(entry => entry.Key).ToList();
        foreach (string modId in modIds)
            ProfileManager.ModGroups[modId] = trimmedName;

        if (ProfileManager.CollapsedGroups.Contains(oldName))
        {
            ProfileManager.CollapsedGroups.Remove(oldName);
            ProfileManager.CollapsedGroups.Add(trimmedName);
        }

        if (ProfileManager.SaveInMemoryState())
            return true;

        RestoreState(previousState);
        return false;
    }

    public static bool DeleteGroup(string groupName)
    {
        var previousState = CaptureState();
        if (!ProfileManager.CustomGroups.Remove(groupName))
            return false;

        var modIds = ProfileManager.ModGroups.Where(entry => entry.Value == groupName).Select(entry => entry.Key).ToList();
        foreach (string modId in modIds)
            ProfileManager.ModGroups.Remove(modId);

        ProfileManager.CollapsedGroups.Remove(groupName);

        if (ProfileManager.SaveInMemoryState())
            return true;

        RestoreState(previousState);
        return false;
    }

    private static GroupStateSnapshot CaptureState()
    {
        return new GroupStateSnapshot(
            new List<string>(ProfileManager.CustomGroups),
            new Dictionary<string, string>(ProfileManager.ModGroups, StringComparer.Ordinal),
            new HashSet<string>(ProfileManager.CollapsedGroups, StringComparer.Ordinal));
    }

    private static void RestoreState(GroupStateSnapshot snapshot)
    {
        ProfileManager.CustomGroups = snapshot.CustomGroups;
        ProfileManager.ModGroups = snapshot.ModGroups;
        ProfileManager.CollapsedGroups = snapshot.CollapsedGroups;
    }

    private sealed record GroupStateSnapshot(
        List<string> CustomGroups,
        Dictionary<string, string> ModGroups,
        HashSet<string> CollapsedGroups);
}
