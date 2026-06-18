using System;
using System.Collections.Generic;
using MegaCrit.Sts2.Core.Logging;

namespace BetterModMenu.Data;

internal static class ProfileManifestCacheBuilder
{
    public static void Rebuild(Dictionary<string, bool> targetCache, Logger logger, IEnumerable<string> configExtensions)
    {
        targetCache.Clear();
        var mods = MegaCrit.Sts2.Core.Modding.ModManager.Mods;
        if (mods == null)
            return;

        foreach (var mod in mods)
        {
            string modId = mod.manifest?.id ?? string.Empty;
            if (string.IsNullOrEmpty(modId))
                continue;

            if (!ModInstallPathResolver.TryGetDirectoryFromPath(mod.path, out string directory))
                continue;

            string? manifestPath = ManifestScanner.FindManifestPath(directory, modId, configExtensions);
            if (string.IsNullOrEmpty(manifestPath))
                continue;

            try
            {
                if (ManifestScanner.TryReadManifestInfo(manifestPath, modId, out var manifestInfo))
                    targetCache[modId] = manifestInfo.AffectsGameplay;
            }
            catch (Exception ex)
            {
                logger.Error($"Failed to read manifest {manifestPath}: {ex.Message}");
            }
        }
    }
}
