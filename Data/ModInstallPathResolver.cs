using System.IO;

namespace BetterModMenu.Data;

internal static class ModInstallPathResolver
{
    public static bool TryGetDirectoryFromPath(string? path, out string directory)
    {
        directory = string.Empty;
        if (string.IsNullOrWhiteSpace(path))
            return false;

        if (Directory.Exists(path))
        {
            directory = Path.GetFullPath(path);
            return true;
        }

        if (File.Exists(path))
        {
            string? parentDirectory = Path.GetDirectoryName(path);
            if (!string.IsNullOrWhiteSpace(parentDirectory) && Directory.Exists(parentDirectory))
            {
                directory = Path.GetFullPath(parentDirectory);
                return true;
            }
        }

        return false;
    }
}
