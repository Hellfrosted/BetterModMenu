using System.IO;
using System.Reflection;

namespace BetterModMenu.Data;

internal static class ModVersionProvider
{
    private static readonly string[] ManifestExtensions = [".json", ".jsonc", ".json5"];

    public static string CurrentVersion => ReadVersionFromLoadedModManifest("BetterModMenu");

    public static string ReadVersionFromLoadedModManifest(string modId)
    {
        string? manifestPath = null;
        var mod = MegaCrit.Sts2.Core.Modding.ModManager.Mods.FirstOrDefault(candidate => candidate.manifest?.id == modId);
        if (ModInstallPathResolver.TryGetDirectoryFromPath(mod?.path, out string directory))
            manifestPath = ManifestScanner.FindManifestPath(directory, modId, ManifestExtensions);

        if (string.IsNullOrWhiteSpace(manifestPath))
        {
            string assemblyDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? string.Empty;
            if (ModInstallPathResolver.TryGetDirectoryFromPath(assemblyDirectory, out string fallbackDirectory))
                manifestPath = ManifestScanner.FindManifestPath(fallbackDirectory, modId, ManifestExtensions);
        }

        return !string.IsNullOrWhiteSpace(manifestPath) &&
            ManifestScanner.TryReadVersion(manifestPath, modId, out string version)
            ? version
            : string.Empty;
    }
}
