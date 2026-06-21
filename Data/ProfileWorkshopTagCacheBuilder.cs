using MegaCrit.Sts2.Core.Logging;

namespace BetterModMenu.Data;

internal static class ProfileWorkshopTagCacheBuilder
{
    public static bool Rebuild(Dictionary<string, List<string>> targetCache, Logger logger)
    {
        targetCache.Clear();
        var mods = MegaCrit.Sts2.Core.Modding.ModManager.Mods;
        if (mods == null)
            return false;

        var modIdsByPublishedFileId = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var mod in mods)
        {
            string modId = mod.manifest?.id ?? string.Empty;
            if (string.IsNullOrWhiteSpace(modId))
                continue;

            if (SteamWorkshopLinkResolver.TryGetPublishedFileId(mod.path, out string publishedFileId))
                modIdsByPublishedFileId[publishedFileId] = modId;
        }

        if (modIdsByPublishedFileId.Count == 0)
            return true;

        try
        {
            var tagsByFileId = SteamWorkshopTagService.FetchTagsByPublishedFileId(modIdsByPublishedFileId.Keys);
            foreach (var entry in tagsByFileId)
            {
                if (modIdsByPublishedFileId.TryGetValue(entry.Key, out string? modId))
                    targetCache[modId] = entry.Value;
            }
        }
        catch (Exception ex)
        {
            logger.Error($"Failed to read Steam Workshop tags: {ex.Message}");
        }

        return true;
    }
}
