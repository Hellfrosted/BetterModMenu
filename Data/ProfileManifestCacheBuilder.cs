using System;
using System.Collections.Generic;
using System.IO;
using MegaCrit.Sts2.Core.Logging;

namespace BetterModMenu.Data;

internal static class ProfileManifestCacheBuilder
{
    public static void Rebuild(Dictionary<string, bool> targetCache, Logger logger, IEnumerable<string> configExtensions)
    {
        targetCache.Clear();
        foreach (var mod in Sts2ModManagerCompat.GetLoadedMods())
        {
            string modId = mod.Id;
            if (string.IsNullOrEmpty(modId) || string.IsNullOrEmpty(mod.Path))
                continue;

            string? directory = Directory.Exists(mod.Path) ? mod.Path : Path.GetDirectoryName(mod.Path);
            if (string.IsNullOrEmpty(directory) || !Directory.Exists(directory))
                continue;

            string? manifestPath = ManifestScanner.FindManifestPath(directory, modId, configExtensions);
            if (string.IsNullOrEmpty(manifestPath))
                continue;

            try
            {
                if (ManifestScanner.TryReadAffectsGameplay(manifestPath, modId, out bool affectsGameplay))
                    targetCache[modId] = affectsGameplay;
            }
            catch (Exception ex)
            {
                logger.Error($"Failed to read manifest {manifestPath}: {ex.Message}");
            }
        }
    }
}
