using System;
using BetterModMenu.Data;

namespace BetterModMenu.Patches;

internal static class ModdingPortableModeOps
{
    public static bool SetPortableMode(bool isPortable)
    {
        return isPortable
            ? EnablePortableMode()
            : DisablePortableMode();
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
                !sourcePath.Equals(targetPath, StringComparison.OrdinalIgnoreCase);

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
                !sourcePath.Equals(targetPath, StringComparison.OrdinalIgnoreCase) &&
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
