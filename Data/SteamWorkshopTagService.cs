using System.Net.Http;
using System.Text.Json;

namespace BetterModMenu.Data;

internal static class SteamWorkshopTagService
{
    private const string PublishedFileDetailsUrl = "https://api.steampowered.com/ISteamRemoteStorage/GetPublishedFileDetails/v1/";
    private const int MaxBatchSize = 100;

    public static Dictionary<string, List<string>> FetchTagsByPublishedFileId(IEnumerable<string> publishedFileIds)
    {
        var distinctIds = publishedFileIds
            .Where(id => !string.IsNullOrWhiteSpace(id) && id.All(char.IsDigit))
            .Distinct(StringComparer.Ordinal)
            .ToList();

        var tagsByFileId = new Dictionary<string, List<string>>(StringComparer.Ordinal);
        if (distinctIds.Count == 0)
            return tagsByFileId;

        using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(3) };
        for (int start = 0; start < distinctIds.Count; start += MaxBatchSize)
        {
            var batch = distinctIds.Skip(start).Take(MaxBatchSize).ToList();
            using var content = new FormUrlEncodedContent(BuildRequestFields(batch));
            using var response = client.PostAsync(PublishedFileDetailsUrl, content).GetAwaiter().GetResult();
            response.EnsureSuccessStatusCode();
            string json = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
            foreach (var entry in ParseTagsByPublishedFileId(json))
                tagsByFileId[entry.Key] = entry.Value;
        }

        return tagsByFileId;
    }

    public static Dictionary<string, List<string>> ParseTagsByPublishedFileId(string json)
    {
        var tagsByFileId = new Dictionary<string, List<string>>(StringComparer.Ordinal);
        if (string.IsNullOrWhiteSpace(json))
            return tagsByFileId;

        using var doc = JsonDocument.Parse(json);
        if (!doc.RootElement.TryGetProperty("response", out var response) ||
            !response.TryGetProperty("publishedfiledetails", out var details) ||
            details.ValueKind != JsonValueKind.Array)
        {
            return tagsByFileId;
        }

        foreach (var detail in details.EnumerateArray())
        {
            string fileId = ReadString(detail, "publishedfileid");
            if (string.IsNullOrWhiteSpace(fileId))
                continue;

            var tags = new List<string>();
            if (detail.TryGetProperty("tags", out var tagArray) && tagArray.ValueKind == JsonValueKind.Array)
            {
                foreach (var tagEntry in tagArray.EnumerateArray())
                {
                    string tag = ReadString(tagEntry, "tag");
                    if (!string.IsNullOrWhiteSpace(tag))
                        tags.Add(tag);
                }
            }

            tagsByFileId[fileId] = tags;
        }

        return tagsByFileId;
    }

    private static IEnumerable<KeyValuePair<string, string>> BuildRequestFields(IReadOnlyList<string> publishedFileIds)
    {
        yield return new KeyValuePair<string, string>("itemcount", publishedFileIds.Count.ToString());
        for (int i = 0; i < publishedFileIds.Count; i++)
            yield return new KeyValuePair<string, string>($"publishedfileids[{i}]", publishedFileIds[i]);
    }

    private static string ReadString(JsonElement element, string propertyName)
    {
        return element.TryGetProperty(propertyName, out var property) && property.ValueKind == JsonValueKind.String
            ? property.GetString() ?? string.Empty
            : string.Empty;
    }
}
