namespace BetterModMenu.Data;

internal static class SteamWorkshopLinkResolver
{
    private const string Sts2SteamAppId = "2868840";
    private const string WorkshopUrlPrefix = "https://steamcommunity.com/sharedfiles/filedetails/?id=";

    public static bool TryGetWorkshopUrl(string? path, out string workshopUrl)
    {
        workshopUrl = string.Empty;
        if (string.IsNullOrWhiteSpace(path))
            return false;

        string[] parts = path
            .Replace('\\', '/')
            .Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        for (int i = 0; i <= parts.Length - 4; i++)
        {
            if (!parts[i].Equals("workshop", StringComparison.OrdinalIgnoreCase) ||
                !parts[i + 1].Equals("content", StringComparison.OrdinalIgnoreCase) ||
                !parts[i + 2].Equals(Sts2SteamAppId, StringComparison.Ordinal))
            {
                continue;
            }

            string publishedFileId = parts[i + 3];
            if (!IsPublishedFileId(publishedFileId))
                return false;

            workshopUrl = WorkshopUrlPrefix + publishedFileId;
            return true;
        }

        return false;
    }

    private static bool IsPublishedFileId(string value)
    {
        return value.Length > 0 && value.All(char.IsDigit);
    }
}
