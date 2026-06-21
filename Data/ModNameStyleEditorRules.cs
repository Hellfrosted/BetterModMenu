namespace BetterModMenu.Data;

internal static class ModNameStyleEditorRules
{
    public static ModNameStyleSettings Clone(ModNameStyleSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        return new ModNameStyleSettings
        {
            Enabled = settings.Enabled,
            UseDefaultTagFormats = settings.UseDefaultTagFormats,
            TagFormats = new Dictionary<string, string>(settings.TagFormats ?? new(), StringComparer.OrdinalIgnoreCase),
            TagPriority = new List<string>(settings.TagPriority ?? new()),
            DisabledTags = new HashSet<string>(settings.DisabledTags ?? new(), StringComparer.OrdinalIgnoreCase),
            ModFormats = new Dictionary<string, string>(settings.ModFormats ?? new(), StringComparer.OrdinalIgnoreCase)
        };
    }

    public static void ResetToDefaults(ModNameStyleSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        settings.Enabled = true;
        settings.UseDefaultTagFormats = true;
        settings.TagFormats = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        settings.TagPriority = new List<string>();
        settings.DisabledTags = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        settings.ModFormats = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    }

    public static string BuildPreviewBbCode(
        string modId,
        string displayName,
        IEnumerable<string> workshopTags,
        ModNameStyleSettings settings)
    {
        return ModNameStyleRules.BuildBbCode(modId, displayName, workshopTags, settings);
    }

    public static bool TryCanonicalizeTag(string tag, out string supportedTag)
    {
        supportedTag = string.Empty;
        return !string.IsNullOrWhiteSpace(tag) &&
            ModNameStyleCatalog.TryGetSupportedTagName(tag, out supportedTag);
    }

    public static bool TryNormalizeColor(string color, out string normalizedColor)
    {
        normalizedColor = string.Empty;
        if (string.IsNullOrWhiteSpace(color))
            return false;

        string trimmed = color.Trim();
        string hex = trimmed.StartsWith("#", StringComparison.Ordinal) ? trimmed[1..] : trimmed;
        if (hex.Length is not (3 or 6 or 8) || !hex.All(Uri.IsHexDigit))
            return false;

        normalizedColor = "#" + hex.ToLowerInvariant();
        return true;
    }

    public static bool TrySetTagColor(
        ModNameStyleSettings settings,
        string tag,
        string color,
        out string supportedTag,
        out string normalizedColor)
    {
        ArgumentNullException.ThrowIfNull(settings);

        if (!TryCanonicalizeTag(tag, out supportedTag) ||
            !TryNormalizeColor(color, out normalizedColor))
        {
            supportedTag = string.Empty;
            normalizedColor = string.Empty;
            return false;
        }

        settings.TagFormats ??= new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        settings.DisabledTags ??= new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        RemoveMatchingTag(settings.TagFormats, supportedTag);
        RemoveMatchingTag(settings.DisabledTags, supportedTag);
        settings.TagFormats[supportedTag] = normalizedColor;
        return true;
    }

    public static bool TryResetTagColor(ModNameStyleSettings settings, string tag, out string supportedTag)
    {
        ArgumentNullException.ThrowIfNull(settings);

        if (!TryCanonicalizeTag(tag, out supportedTag))
            return false;

        settings.TagFormats ??= new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        settings.DisabledTags ??= new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        RemoveMatchingTag(settings.TagFormats, supportedTag);
        RemoveMatchingTag(settings.DisabledTags, supportedTag);
        return true;
    }

    public static bool TryDisableTagColor(ModNameStyleSettings settings, string tag, out string supportedTag)
    {
        ArgumentNullException.ThrowIfNull(settings);

        if (!TryCanonicalizeTag(tag, out supportedTag))
            return false;

        settings.DisabledTags ??= new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        RemoveMatchingTag(settings.DisabledTags, supportedTag);
        settings.DisabledTags.Add(supportedTag);
        return true;
    }

    public static bool TrySetModColor(
        ModNameStyleSettings settings,
        string modKey,
        string color,
        out string normalizedColor)
    {
        ArgumentNullException.ThrowIfNull(settings);
        normalizedColor = string.Empty;

        if (string.IsNullOrWhiteSpace(modKey) ||
            !TryNormalizeColor(color, out normalizedColor))
        {
            normalizedColor = string.Empty;
            return false;
        }

        string normalizedModKey = modKey.Trim();
        settings.ModFormats ??= new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        RemoveMatchingModKey(settings.ModFormats, normalizedModKey);
        settings.ModFormats[normalizedModKey] = normalizedColor;
        return true;
    }

    public static bool RemoveModColor(ModNameStyleSettings settings, string modKey)
    {
        ArgumentNullException.ThrowIfNull(settings);

        if (string.IsNullOrWhiteSpace(modKey) || settings.ModFormats == null)
            return false;

        int originalCount = settings.ModFormats.Count;
        RemoveMatchingModKey(settings.ModFormats, modKey.Trim());
        return settings.ModFormats.Count != originalCount;
    }

    private static void RemoveMatchingTag(Dictionary<string, string> tagFormats, string supportedTag)
    {
        foreach (string key in tagFormats.Keys.Where(key => IsSameSupportedTag(key, supportedTag)).ToArray())
            tagFormats.Remove(key);
    }

    private static void RemoveMatchingTag(HashSet<string> tags, string supportedTag)
    {
        foreach (string tag in tags.Where(tag => IsSameSupportedTag(tag, supportedTag)).ToArray())
            tags.Remove(tag);
    }

    private static bool IsSameSupportedTag(string tag, string supportedTag)
    {
        return ModNameStyleCatalog.TryGetSupportedTagName(tag, out string canonicalTag) &&
            string.Equals(canonicalTag, supportedTag, StringComparison.OrdinalIgnoreCase);
    }

    private static void RemoveMatchingModKey(Dictionary<string, string> modFormats, string modKey)
    {
        foreach (string key in modFormats.Keys.Where(key => string.Equals(key.Trim(), modKey, StringComparison.OrdinalIgnoreCase)).ToArray())
            modFormats.Remove(key);
    }
}
