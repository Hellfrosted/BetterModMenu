using System.Globalization;
using System.Text;

namespace BetterModMenu.Data;

internal sealed class ModSearchDocument(string modId, string name)
{
    public string ModId { get; } = modId;
    public string Name { get; } = name;
    public string Author { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string Version { get; init; } = string.Empty;
    public IReadOnlyList<string> Dependencies { get; init; } = Array.Empty<string>();
    public string Group { get; init; } = string.Empty;
    public bool Enabled { get; init; } = true;
    public string WorkshopId { get; init; } = string.Empty;
    public string WorkshopUrl { get; init; } = string.Empty;
}

internal sealed class ModSearchResult
{
    public string ModId { get; init; } = string.Empty;
    public int Score { get; init; }
    public string MatchReason { get; init; } = string.Empty;
}

internal static class ModSearchRules
{
    private const int ExactIdOrNameScore = 1200;
    private const int PrefixIdOrNameScore = 1100;
    private const int FuzzyNameScore = 780;
    private const int FuzzyIdScore = 740;
    private const int DependencyScore = 620;
    private const int WorkshopScore = 580;
    private const int MetadataScore = 450;
    private const int DescriptionScore = 260;

    public static List<ModSearchResult> Search(IEnumerable<ModSearchDocument> documents, string query)
    {
        string normalizedQuery = Normalize(query);
        if (string.IsNullOrWhiteSpace(normalizedQuery))
            return new List<ModSearchResult>();

        var tokens = Tokenize(normalizedQuery);
        if (tokens.Count == 0)
            return new List<ModSearchResult>();

        return documents
            .Select(document => ScoreDocument(document, normalizedQuery, tokens))
            .Where(result => result.Score > 0)
            .OrderByDescending(result => result.Score)
            .ThenBy(result => result.ModId, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public static string PickSelectedModId(string currentModId, IReadOnlyList<ModSearchResult> results)
    {
        if (!string.IsNullOrWhiteSpace(currentModId) &&
            results.Any(result => string.Equals(result.ModId, currentModId, StringComparison.OrdinalIgnoreCase)))
        {
            return currentModId;
        }

        return results.Count == 0 ? string.Empty : results[0].ModId;
    }

    private static ModSearchResult ScoreDocument(
        ModSearchDocument document,
        string normalizedQuery,
        IReadOnlyList<string> tokens)
    {
        var fields = BuildFields(document);
        int score = 0;
        string reason = string.Empty;

        foreach (var field in fields)
        {
            int fieldScore = ScoreField(field, normalizedQuery, tokens);
            if (fieldScore <= score)
                continue;

            score = fieldScore;
            reason = BuildReason(field);
        }

        return new ModSearchResult
        {
            ModId = document.ModId,
            Score = score,
            MatchReason = reason
        };
    }

    private static IReadOnlyList<SearchField> BuildFields(ModSearchDocument document)
    {
        var fields = new List<SearchField>
        {
            new("id", document.ModId, ExactIdOrNameScore, PrefixIdOrNameScore, FuzzyIdScore, IncludeContainsForShortQuery: true),
            new("name", document.Name, ExactIdOrNameScore, PrefixIdOrNameScore, FuzzyNameScore, IncludeContainsForShortQuery: true),
            new("author", document.Author, MetadataScore, MetadataScore - 40, MetadataScore - 90, IncludeContainsForShortQuery: false),
            new("version", document.Version, MetadataScore, MetadataScore - 40, 0, IncludeContainsForShortQuery: false),
            new("group", document.Group, MetadataScore, MetadataScore - 40, MetadataScore - 120, IncludeContainsForShortQuery: false),
            new("state", document.Enabled ? "enabled" : "disabled", MetadataScore, MetadataScore - 40, 0, IncludeContainsForShortQuery: false),
            new("Steam Workshop id", document.WorkshopId, WorkshopScore, WorkshopScore - 40, 0, IncludeContainsForShortQuery: true),
            new("Steam Workshop link", document.WorkshopUrl, WorkshopScore - 80, WorkshopScore - 100, 0, IncludeContainsForShortQuery: true),
            new("description", document.Description, DescriptionScore, DescriptionScore - 40, DescriptionScore - 80, IncludeContainsForShortQuery: false)
        };

        foreach (string dependency in document.Dependencies)
            fields.Add(new SearchField("dependency", dependency, DependencyScore, DependencyScore - 40, DependencyScore - 90, IncludeContainsForShortQuery: false));

        return fields;
    }

    private static int ScoreField(SearchField field, string normalizedQuery, IReadOnlyList<string> queryTokens)
    {
        if (string.IsNullOrWhiteSpace(field.Value))
            return 0;

        string normalizedValue = Normalize(field.Value);
        if (normalizedValue.Length == 0)
            return 0;

        bool shortQuery = normalizedQuery.Length <= 2;
        if (normalizedValue.Equals(normalizedQuery, StringComparison.Ordinal))
            return field.ExactScore;

        if (shortQuery && !field.IncludeContainsForShortQuery)
            return 0;

        if (normalizedValue.StartsWith(normalizedQuery, StringComparison.Ordinal))
            return field.PrefixScore;

        var valueTokens = Tokenize(normalizedValue);
        if (valueTokens.Any(token => token.Equals(normalizedQuery, StringComparison.Ordinal)))
            return field.ExactScore - 40;

        if (valueTokens.Any(token => token.StartsWith(normalizedQuery, StringComparison.Ordinal)))
            return field.PrefixScore - 40;

        if (shortQuery)
            return 0;

        int tokenScore = ScoreTokens(field, queryTokens, valueTokens, normalizedValue);
        if (tokenScore > 0)
            return tokenScore;

        return normalizedValue.Contains(normalizedQuery, StringComparison.Ordinal)
            ? Math.Max(1, field.FuzzyScore - 90)
            : 0;
    }

    private static int ScoreTokens(
        SearchField field,
        IReadOnlyList<string> queryTokens,
        IReadOnlyList<string> valueTokens,
        string normalizedValue)
    {
        int score = 0;
        int matched = 0;
        foreach (string queryToken in queryTokens)
        {
            int tokenScore = ScoreOneToken(field, queryToken, valueTokens, normalizedValue);
            if (tokenScore <= 0)
                continue;

            matched++;
            score += tokenScore;
        }

        if (matched == 0)
            return 0;

        int average = score / matched;
        int unmatchedPenalty = (queryTokens.Count - matched) * 35;
        return Math.Max(1, average - unmatchedPenalty);
    }

    private static int ScoreOneToken(
        SearchField field,
        string queryToken,
        IReadOnlyList<string> valueTokens,
        string normalizedValue)
    {
        if (valueTokens.Any(token => token.Equals(queryToken, StringComparison.Ordinal)))
            return field.ExactScore - 60;

        if (valueTokens.Any(token => token.StartsWith(queryToken, StringComparison.Ordinal)))
            return field.PrefixScore - 60;

        if (normalizedValue.Contains(queryToken, StringComparison.Ordinal))
            return Math.Max(1, field.FuzzyScore - 80);

        if (field.FuzzyScore <= 0)
            return 0;

        int allowedDistance = GetAllowedDistance(queryToken.Length);
        foreach (string token in valueTokens)
        {
            if (DamerauLevenshteinDistance(queryToken, token, allowedDistance) <= allowedDistance)
                return field.FuzzyScore;
        }

        string compactValue = string.Concat(valueTokens);
        return DamerauLevenshteinDistance(queryToken, compactValue, allowedDistance) <= allowedDistance
            ? field.FuzzyScore - 20
            : 0;
    }

    private static int GetAllowedDistance(int length)
    {
        return length switch
        {
            <= 4 => 1,
            <= 8 => 2,
            _ => 3
        };
    }

    private static string BuildReason(SearchField field)
    {
        return field.Name switch
        {
            "id" => "Matched mod id",
            "name" => "Matched mod name",
            "Steam Workshop id" => "Matched Steam Workshop id",
            "Steam Workshop link" => "Matched Steam Workshop link",
            _ => "Matched " + field.Name
        };
    }

    private static IReadOnlyList<string> Tokenize(string normalized)
    {
        return normalized
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Distinct(StringComparer.Ordinal)
            .ToArray();
    }

    public static string Normalize(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        var builder = new StringBuilder(value.Length);
        bool previousWasSpace = true;
        foreach (char raw in value.Normalize(NormalizationForm.FormD))
        {
            if (CharUnicodeInfo.GetUnicodeCategory(raw) == UnicodeCategory.NonSpacingMark)
                continue;

            char character = NormalizeLookalike(char.ToLowerInvariant(raw));
            if (char.IsLetterOrDigit(character))
            {
                builder.Append(character);
                previousWasSpace = false;
                continue;
            }

            if (previousWasSpace)
                continue;

            builder.Append(' ');
            previousWasSpace = true;
        }

        return builder.ToString().Trim();
    }

    private static char NormalizeLookalike(char character)
    {
        return character switch
        {
            '0' => 'o',
            '1' => 'l',
            '3' => 'e',
            '4' => 'a',
            '5' => 's',
            '7' => 't',
            '@' => 'a',
            '$' => 's',
            _ => character
        };
    }

    private static int DamerauLevenshteinDistance(string source, string target, int maxDistance)
    {
        if (Math.Abs(source.Length - target.Length) > maxDistance)
            return maxDistance + 1;

        var distances = new int[source.Length + 1, target.Length + 1];
        for (int i = 0; i <= source.Length; i++)
            distances[i, 0] = i;
        for (int j = 0; j <= target.Length; j++)
            distances[0, j] = j;

        for (int i = 1; i <= source.Length; i++)
        {
            int bestInRow = int.MaxValue;
            for (int j = 1; j <= target.Length; j++)
            {
                int cost = source[i - 1] == target[j - 1] ? 0 : 1;
                int value = Math.Min(
                    Math.Min(distances[i - 1, j] + 1, distances[i, j - 1] + 1),
                    distances[i - 1, j - 1] + cost);

                if (i > 1 &&
                    j > 1 &&
                    source[i - 1] == target[j - 2] &&
                    source[i - 2] == target[j - 1])
                {
                    value = Math.Min(value, distances[i - 2, j - 2] + 1);
                }

                distances[i, j] = value;
                bestInRow = Math.Min(bestInRow, value);
            }

            if (bestInRow > maxDistance)
                return maxDistance + 1;
        }

        return distances[source.Length, target.Length];
    }

    private sealed record SearchField(
        string Name,
        string Value,
        int ExactScore,
        int PrefixScore,
        int FuzzyScore,
        bool IncludeContainsForShortQuery);
}
