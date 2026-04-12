using Godot;
using BetterModMenu.Data;

namespace BetterModMenu.Patches;

internal static class ModdingScreenStateOps
{
    public static bool SetPortableMode(bool isPortable)
    {
        return isPortable
            ? EnablePortableMode()
            : DisablePortableMode();
    }

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
            if (string.IsNullOrWhiteSpace(modId))
                continue;

            assignedGroups[modId] = GetAssignedGroup(modId, validGroups);
        }

        return assignedGroups;
    }

    public static void SyncGroupDropdown(OptionButton dropdown, string assignedGroup)
    {
        string currentSelection = (dropdown.ItemCount > 0 && dropdown.Selected >= 0)
            ? dropdown.GetItemText(dropdown.Selected)
            : "";

        if (currentSelection == assignedGroup && dropdown.ItemCount == ProfileManager.CustomGroups.Count + 1)
            return;

        dropdown.Clear();
        dropdown.AddItem(ModdingScreenConstants.UnassignedGroup, 0);
        for (int i = 0; i < ProfileManager.CustomGroups.Count; i++)
            dropdown.AddItem(ProfileManager.CustomGroups[i], i + 1);

        int selectedIndex = assignedGroup == ModdingScreenConstants.UnassignedGroup
            ? 0
            : ProfileManager.CustomGroups.IndexOf(assignedGroup) + 1;
        dropdown.Select(selectedIndex);
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

        var validation = ModdingGroupRules.ValidateRename(ProfileManager.CustomGroups, oldName, newName, out string trimmedName);
        if (validation == GroupNameValidationResult.Unchanged)
            return true;

        if (validation != GroupNameValidationResult.Valid)
            return false;

        var previousGroups = new List<string>(ProfileManager.CustomGroups);
        var previousModGroups = new Dictionary<string, string>(ProfileManager.ModGroups, StringComparer.Ordinal);
        var previousCollapsedGroups = new HashSet<string>(ProfileManager.CollapsedGroups, StringComparer.Ordinal);

        ProfileManager.CustomGroups[index] = trimmedName;

        var modIds = ProfileManager.ModGroups.Where(x => x.Value == oldName).Select(x => x.Key).ToList();
        foreach (var modId in modIds)
            ProfileManager.ModGroups[modId] = trimmedName;

        if (ProfileManager.CollapsedGroups.Contains(oldName))
        {
            ProfileManager.CollapsedGroups.Remove(oldName);
            ProfileManager.CollapsedGroups.Add(trimmedName);
        }

        if (ProfileManager.SaveInMemoryState())
            return true;

        ProfileManager.CustomGroups = previousGroups;
        ProfileManager.ModGroups = previousModGroups;
        ProfileManager.CollapsedGroups = previousCollapsedGroups;
        return false;
    }

    public static bool DeleteGroup(string groupName)
    {
        var previousGroups = new List<string>(ProfileManager.CustomGroups);
        var previousModGroups = new Dictionary<string, string>(ProfileManager.ModGroups, StringComparer.Ordinal);
        var previousCollapsedGroups = new HashSet<string>(ProfileManager.CollapsedGroups, StringComparer.Ordinal);

        if (!ProfileManager.CustomGroups.Remove(groupName))
            return false;

        var modIds = ProfileManager.ModGroups.Where(x => x.Value == groupName).Select(x => x.Key).ToList();
        foreach (var modId in modIds)
            ProfileManager.ModGroups.Remove(modId);

        ProfileManager.CollapsedGroups.Remove(groupName);

        if (ProfileManager.SaveInMemoryState())
            return true;

        ProfileManager.CustomGroups = previousGroups;
        ProfileManager.ModGroups = previousModGroups;
        ProfileManager.CollapsedGroups = previousCollapsedGroups;
        return false;
    }

    private static bool EnablePortableMode()
    {
        string sourcePath = ProfileManager.SavePath;
        if (!ProfileManager.TryGetPortableConfigPathForExtension(System.IO.Path.GetExtension(sourcePath), out string targetPath))
        {
            ProfileManager.ModLogger.Error("Failed to enable portable mode: could not resolve the portable config directory.");
            return false;
        }

        return CopyOrWriteConfig(sourcePath, targetPath, deleteSourceAfterCopy: false);
    }

    private static bool DisablePortableMode()
    {
        if (!ProfileManager.TryGetPortableConfigPath(out string sourcePath))
        {
            ProfileManager.ModLogger.Error("Failed to disable portable mode: portable mode is not available for the current mod path.");
            return false;
        }

        string targetPath = ProfileManager.GetUserConfigPathForExtension(System.IO.Path.GetExtension(sourcePath));
        return CopyOrWriteConfig(sourcePath, targetPath, deleteSourceAfterCopy: true);
    }

    private static bool CopyOrWriteConfig(string sourcePath, string targetPath, bool deleteSourceAfterCopy)
    {
        if (string.IsNullOrWhiteSpace(targetPath))
            return false;

        string tempTargetPath = targetPath + ".tmp";
        try
        {
            bool canCopy = System.IO.File.Exists(sourcePath) &&
                !sourcePath.Equals(targetPath, System.StringComparison.OrdinalIgnoreCase);

            string? targetDirectory = System.IO.Path.GetDirectoryName(targetPath);
            if (!string.IsNullOrEmpty(targetDirectory) && !System.IO.Directory.Exists(targetDirectory))
                System.IO.Directory.CreateDirectory(targetDirectory);

            if (System.IO.File.Exists(tempTargetPath))
                System.IO.File.Delete(tempTargetPath);

            if (canCopy)
            {
                System.IO.File.Copy(sourcePath, tempTargetPath, true);
            }
            else if (!ProfileManager.SaveCurrentStateToPath(tempTargetPath))
            {
                return false;
            }

            System.IO.File.Move(tempTargetPath, targetPath, true);
            ProfileManager.DeleteOtherConfigVariants(targetPath);

            if (deleteSourceAfterCopy &&
                !sourcePath.Equals(targetPath, System.StringComparison.OrdinalIgnoreCase) &&
                System.IO.File.Exists(sourcePath))
            {
                System.IO.File.Delete(sourcePath);
            }

            return true;
        }
        catch (Exception ex)
        {
            if (System.IO.File.Exists(tempTargetPath))
                System.IO.File.Delete(tempTargetPath);
            ProfileManager.ModLogger.Error($"Failed to copy config from '{sourcePath}' to '{targetPath}':\n{ex}");
            return false;
        }
    }
}
