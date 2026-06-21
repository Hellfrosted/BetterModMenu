namespace BetterModMenu.Data;

internal static class ModNameStyleRules
{
    public static string BuildBbCode(
        string modId,
        string displayName,
        IEnumerable<string> workshopTags,
        ModNameStyleSettings settings)
    {
        if (!settings.Enabled || string.IsNullOrWhiteSpace(displayName))
            return EscapeBbCode(displayName);

        return TryBuildBbCode(modId, displayName, workshopTags, settings, out string bbCode)
            ? bbCode
            : EscapeBbCode(displayName);
    }

    public static bool TryBuildBbCode(
        string modId,
        string displayName,
        IEnumerable<string> styleTags,
        ModNameStyleSettings settings,
        out string bbCode)
    {
        bbCode = EscapeBbCode(displayName);
        if (!settings.Enabled || string.IsNullOrWhiteSpace(displayName))
            return false;

        if (!TryBuildFormat(modId, displayName, styleTags, settings, out string? format) ||
            format == null)
            return false;

        bbCode = ApplyFormat(format, displayName);
        return true;
    }

    public static bool TryBuildSimpleColor(
        string modId,
        string displayName,
        IEnumerable<string> styleTags,
        ModNameStyleSettings settings,
        out string color)
    {
        color = string.Empty;
        if (!settings.Enabled || string.IsNullOrWhiteSpace(displayName))
            return false;

        if (!TryBuildFormat(modId, displayName, styleTags, settings, out string? format) ||
            format == null)
            return false;

        return TryExtractSimpleColor(format, displayName, out color);
    }

    public static IReadOnlyDictionary<string, string> GetDefaultTagFormats()
    {
        return ModNameStyleCatalog.GetDefaultTagFormats();
    }

    public static bool RequiresWorkshopTags(ModNameStyleSettings settings)
    {
        return settings.Enabled && (settings.UseDefaultTagFormats || (settings.TagFormats?.Count ?? 0) > 0);
    }

    private static bool TryBuildFormat(
        string modId,
        string displayName,
        IEnumerable<string> styleTags,
        ModNameStyleSettings settings,
        out string? format)
    {
        var modFormats = BuildLookup(settings.ModFormats);
        if ((modFormats.TryGetValue(modId, out string? modFormat) ||
            modFormats.TryGetValue(displayName, out modFormat)) &&
            modFormat != null)
        {
            format = modFormat;
            return true;
        }

        var tagFormats = BuildTagFormats(settings);
        if (TryPickTagFormat(styleTags, tagFormats, ModNameStyleCatalog.BuildTagPriority(settings.TagPriority), out string? tagFormat) &&
            tagFormat != null)
        {
            format = tagFormat;
            return true;
        }

        format = null;
        return false;
    }

    private static Dictionary<string, string> BuildTagFormats(ModNameStyleSettings settings)
    {
        var disabledTags = ModNameStyleCatalog.BuildSupportedSet(settings.DisabledTags);
        var formats = settings.UseDefaultTagFormats
            ? ModNameStyleCatalog.CreateDefaultTagFormatLookup()
            : new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var entry in settings.TagFormats ?? Enumerable.Empty<KeyValuePair<string, string>>())
        {
            if (!string.IsNullOrWhiteSpace(entry.Key) &&
                !string.IsNullOrWhiteSpace(entry.Value) &&
                ModNameStyleCatalog.TryGetSupportedTagName(entry.Key, out string supportedTag) &&
                !disabledTags.Contains(supportedTag))
            {
                formats[supportedTag] = entry.Value;
            }
        }

        foreach (string tag in disabledTags)
            formats.Remove(tag);

        return formats;
    }

    private static Dictionary<string, string> BuildLookup(Dictionary<string, string>? source)
    {
        var lookup = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var entry in source ?? Enumerable.Empty<KeyValuePair<string, string>>())
        {
            if (!string.IsNullOrWhiteSpace(entry.Key) && !string.IsNullOrWhiteSpace(entry.Value))
                lookup[entry.Key] = entry.Value;
        }

        return lookup;
    }

    private static bool TryPickTagFormat(
        IEnumerable<string> workshopTags,
        Dictionary<string, string> formats,
        IReadOnlyList<string> priority,
        out string? format)
    {
        var tags = workshopTags
            .Where(tag => !string.IsNullOrWhiteSpace(tag))
            .Select(tag => ModNameStyleCatalog.TryGetSupportedTagName(tag, out string supportedTag) ? supportedTag : tag)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (string tag in priority)
        {
            if (tags.Contains(tag) && formats.TryGetValue(tag, out format))
                return true;
        }

        foreach (string tag in tags)
        {
            if (formats.TryGetValue(tag, out format))
                return true;
        }

        format = null;
        return false;
    }

    private static string ApplyFormat(string format, string displayName)
    {
        string escapedName = EscapeBbCode(displayName);
        if (format.Contains("{name}", StringComparison.Ordinal))
            return format.Replace("{name}", escapedName, StringComparison.Ordinal);

        if (LooksLikeColor(format))
            return $"[color={format}]{escapedName}[/color]";

        return format + escapedName;
    }

    private static bool LooksLikeColor(string value)
    {
        string color = value.StartsWith("#", StringComparison.Ordinal) ? value[1..] : value;
        return color.Length is 3 or 6 or 8 && color.All(Uri.IsHexDigit);
    }

    private static bool TryExtractSimpleColor(string format, string displayName, out string color)
    {
        color = string.Empty;
        string trimmed = format.Trim();
        if (LooksLikeColor(trimmed))
        {
            color = trimmed;
            return true;
        }

        string escapedName = EscapeBbCode(displayName);
        const string colorPrefix = "[color=";
        const string colorSuffix = "[/color]";
        if (!trimmed.StartsWith(colorPrefix, StringComparison.OrdinalIgnoreCase) ||
            !trimmed.EndsWith(colorSuffix, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        int closeBracketIndex = trimmed.IndexOf(']', colorPrefix.Length);
        if (closeBracketIndex < 0)
            return false;

        string innerText = trimmed[(closeBracketIndex + 1)..^colorSuffix.Length];
        if (!string.Equals(innerText, "{name}", StringComparison.Ordinal) &&
            !string.Equals(innerText, escapedName, StringComparison.Ordinal))
        {
            return false;
        }

        string parsedColor = trimmed[colorPrefix.Length..closeBracketIndex].Trim();
        if (!LooksLikeColor(parsedColor))
            return false;

        color = parsedColor;
        return true;
    }

    private static string EscapeBbCode(string value)
    {
        return string.Concat(value.Select(character => character switch
        {
            '[' => "[lb]",
            ']' => "[rb]",
            _ => character.ToString()
        }));
    }
}
