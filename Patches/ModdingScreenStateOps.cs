using Godot;
using BetterModMenu.Data;

namespace BetterModMenu.Patches;

internal static class ModdingScreenStateOps
{
    public static void SetPortableMode(bool isPortable)
    {
        if (isPortable)
            EnablePortableMode();
        else
            DisablePortableMode();
    }

    public static bool TryAddGroup(string groupName)
    {
        string trimmedName = groupName.Trim();
        if (string.IsNullOrEmpty(trimmedName) || trimmedName == ModdingScreenConstants.UnassignedGroup || ProfileManager.CustomGroups.Contains(trimmedName))
            return false;

        ProfileManager.CustomGroups.Add(trimmedName);
        ProfileManager.SaveInMemoryState();
        return true;
    }

    public static string GetAssignedGroup(string modId)
    {
        if (!string.IsNullOrEmpty(modId) &&
            ProfileManager.ModGroups.TryGetValue(modId, out string? assignedGroup) &&
            assignedGroup != null &&
            ProfileManager.CustomGroups.Contains(assignedGroup))
        {
            return assignedGroup;
        }

        return ModdingScreenConstants.UnassignedGroup;
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

        ProfileManager.CustomGroups.RemoveAt(currentIndex);
        ProfileManager.CustomGroups.Insert(newIndex, groupName);
        ProfileManager.SaveInMemoryState();
        return true;
    }

    public static bool TryRenameGroup(string oldName, string newName)
    {
        string trimmedName = newName.Trim();
        if (string.IsNullOrEmpty(trimmedName) || trimmedName == ModdingScreenConstants.UnassignedGroup)
            return false;

        int index = ProfileManager.CustomGroups.IndexOf(oldName);
        if (index == -1)
            return false;

        if (trimmedName == oldName)
            return true;

        if (ProfileManager.CustomGroups.Contains(trimmedName))
            return false;

        ProfileManager.CustomGroups[index] = trimmedName;

        var modIds = ProfileManager.ModGroups.Where(x => x.Value == oldName).Select(x => x.Key).ToList();
        foreach (var modId in modIds)
            ProfileManager.ModGroups[modId] = trimmedName;

        if (ProfileManager.CollapsedGroups.Contains(oldName))
        {
            ProfileManager.CollapsedGroups.Remove(oldName);
            ProfileManager.CollapsedGroups.Add(trimmedName);
        }

        ProfileManager.SaveInMemoryState();
        return true;
    }

    public static bool DeleteGroup(string groupName)
    {
        if (!ProfileManager.CustomGroups.Remove(groupName))
            return false;

        var modIds = ProfileManager.ModGroups.Where(x => x.Value == groupName).Select(x => x.Key).ToList();
        foreach (var modId in modIds)
            ProfileManager.ModGroups.Remove(modId);

        ProfileManager.CollapsedGroups.Remove(groupName);

        ProfileManager.SaveInMemoryState();
        return true;
    }

    private static void EnablePortableMode()
    {
        string sourcePath = ProfileManager.SavePath;
        string targetPath = ProfileManager.GetPortableConfigPathForExtension(System.IO.Path.GetExtension(sourcePath));
        CopyOrWriteConfig(sourcePath, targetPath, deleteSourceAfterCopy: false);
    }

    private static void DisablePortableMode()
    {
        string sourcePath = ProfileManager.PortableConfigPath;
        string targetPath = ProfileManager.GetUserConfigPathForExtension(System.IO.Path.GetExtension(sourcePath));
        CopyOrWriteConfig(sourcePath, targetPath, deleteSourceAfterCopy: true);
    }

    private static void CopyOrWriteConfig(string sourcePath, string targetPath, bool deleteSourceAfterCopy)
    {
        bool canCopy = System.IO.File.Exists(sourcePath) &&
            !sourcePath.Equals(targetPath, System.StringComparison.OrdinalIgnoreCase);

        string tempTargetPath = targetPath + ".tmp";
        if (System.IO.File.Exists(tempTargetPath))
            System.IO.File.Delete(tempTargetPath);

        if (canCopy)
        {
            System.IO.File.Copy(sourcePath, tempTargetPath, true);
        }
        else
        {
            ProfileManager.SaveCurrentStateToPath(tempTargetPath);
        }

        System.IO.File.Move(tempTargetPath, targetPath, true);
        ProfileManager.DeleteOtherConfigVariants(targetPath);

        if (deleteSourceAfterCopy &&
            !sourcePath.Equals(targetPath, System.StringComparison.OrdinalIgnoreCase) &&
            System.IO.File.Exists(sourcePath))
        {
            System.IO.File.Delete(sourcePath);
        }
    }
}
