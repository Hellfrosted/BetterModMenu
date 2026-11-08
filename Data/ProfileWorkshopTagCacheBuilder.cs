using MegaCrit.Sts2.Core.Logging;

namespace BetterModMenu.Data;

internal static class ProfileWorkshopTagCacheBuilder
{
    public static bool Rebuild(Dictionary<string, List<string>> targetCache, Logger logger)
    {
        targetCache.Clear();
        var installations = GetInstallations();
        if (installations == null)
            return false;

        if (installations.Count == 0)
            return true;

        try
        {
            var tagsByFileId = SteamWorkshopTagService.FetchTagsByPublishedFileId(installations.Select(item => item.PublishedFileId));
            foreach (var entry in SteamWorkshopTagService.MapTagsToMods(installations, tagsByFileId))
                targetCache[entry.Key] = entry.Value;
        }
        catch (Exception ex)
        {
            logger.Error($"Failed to read Steam Workshop tags: {ex.Message}");
            return false;
        }

        return true;
    }

    public static List<(string ModId, string PublishedFileId)>? GetInstallations()
    {
        var mods = MegaCrit.Sts2.Core.Modding.ModManager.Mods;
        if (mods == null)
            return null;

        var installations = new List<(string ModId, string PublishedFileId)>();
        foreach (var mod in mods)
        {
            string modId = mod.manifest?.id ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(modId) && SteamWorkshopLinkResolver.TryGetPublishedFileId(mod.path, out string publishedFileId))
                installations.Add((modId, publishedFileId));
        }
        return installations;
    }
}
