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
        var mods = MegaCrit.Sts2.Core.Modding.ModManager.Mods;
        if (mods == null)
            return;

        foreach (var mod in mods)
        {
            string modId = mod.manifest?.id ?? string.Empty;
            if (string.IsNullOrEmpty(modId) || string.IsNullOrEmpty(mod.path))
                continue;

            string? directory = Directory.Exists(mod.path) ? mod.path : Path.GetDirectoryName(mod.path);
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
