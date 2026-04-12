using Godot;

namespace BetterModMenu.Patches;

internal static class ModdingScreenStateOps
{
    public static bool SetPortableMode(bool isPortable)
    {
        return ModdingPortableModeOps.SetPortableMode(isPortable);
    }

    public static bool TryAddGroup(string groupName)
    {
        return ModdingGroupStateOps.TryAddGroup(groupName);
    }

    public static string GetAssignedGroup(string modId, ISet<string>? validGroups = null)
    {
        return ModdingGroupStateOps.GetAssignedGroup(modId, validGroups);
    }

    public static Dictionary<string, string> BuildAssignedGroupLookup(IEnumerable<string> modIds)
    {
        return ModdingGroupStateOps.BuildAssignedGroupLookup(modIds);
    }

    public static void SyncGroupDropdown(OptionButton dropdown, string assignedGroup)
    {
        ModdingGroupStateOps.SyncGroupDropdown(dropdown, assignedGroup);
    }

    public static bool TryMoveGroup(string groupName, int direction)
    {
        return ModdingGroupStateOps.TryMoveGroup(groupName, direction);
    }

    public static bool TryRenameGroup(string oldName, string newName)
    {
        return ModdingGroupStateOps.TryRenameGroup(oldName, newName);
    }

    public static bool DeleteGroup(string groupName)
    {
        return ModdingGroupStateOps.DeleteGroup(groupName);
    }
}
