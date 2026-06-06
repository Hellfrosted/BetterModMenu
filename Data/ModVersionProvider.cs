using System.IO;
using System.Reflection;

namespace BetterModMenu.Data;

internal static class ModVersionProvider
{
    public static string CurrentVersion => ReadVersionFromLoadedModManifest("BetterModMenu");

    public static string ReadVersionFromLoadedModManifest(string modId)
    {
        var mod = MegaCrit.Sts2.Core.Modding.ModManager.Mods.FirstOrDefault(candidate => candidate.manifest?.id == modId);
        string? manifestPath = null;
        if (!string.IsNullOrWhiteSpace(mod?.path))
        {
            string? directory = Directory.Exists(mod.path) ? mod.path : Path.GetDirectoryName(mod.path);
            if (!string.IsNullOrWhiteSpace(directory))
                manifestPath = Path.Combine(directory, modId + ".json");
        }

        if (string.IsNullOrWhiteSpace(manifestPath) || !File.Exists(manifestPath))
        {
            string assemblyDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? string.Empty;
            manifestPath = Path.Combine(assemblyDirectory, modId + ".json");
        }

        return ManifestScanner.TryReadVersion(manifestPath, modId, out string version)
            ? version
            : string.Empty;
    }
}
