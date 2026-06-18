using System.IO;

namespace BetterModMenu.Data;

internal static class FileNameCollisionRules
{
    public static string GetUniquePath(string directory, string fileName)
    {
        string candidate = Path.Combine(directory, fileName);
        if (!File.Exists(candidate))
            return candidate;

        string baseName = Path.GetFileNameWithoutExtension(fileName);
        string extension = Path.GetExtension(fileName);
        for (int i = 2; ; i++)
        {
            candidate = Path.Combine(directory, $"{baseName}-{i}{extension}");
            if (!File.Exists(candidate))
                return candidate;
        }
    }
}
