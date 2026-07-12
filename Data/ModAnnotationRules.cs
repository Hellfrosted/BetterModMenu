namespace BetterModMenu.Data;

internal static class ModAnnotationRules
{
    public const int MaxAliasLength = 120;
    public const int MaxNotesLength = 4000;

    public static bool TryNormalize(string? alias, string? notes, out ModAnnotation annotation, out string? error)
    {
        string normalizedAlias = (alias ?? string.Empty).Trim();
        string normalizedNotes = (notes ?? string.Empty)
            .Replace("\r\n", "\n", StringComparison.Ordinal)
            .Replace('\r', '\n')
            .Trim();

        if (normalizedAlias.Length > MaxAliasLength)
        {
            annotation = new ModAnnotation();
            error = $"Alias must be {MaxAliasLength} characters or fewer.";
            return false;
        }

        if (normalizedNotes.Length > MaxNotesLength)
        {
            annotation = new ModAnnotation();
            error = $"Notes must be {MaxNotesLength} characters or fewer.";
            return false;
        }

        annotation = new ModAnnotation { Alias = normalizedAlias, Notes = normalizedNotes };
        error = null;
        return true;
    }

    public static Dictionary<string, ModAnnotation> NormalizeDictionary(Dictionary<string, ModAnnotation>? annotations)
    {
        var normalized = new Dictionary<string, ModAnnotation>(StringComparer.Ordinal);
        if (annotations == null)
            return normalized;

        foreach ((string modId, ModAnnotation? annotation) in annotations)
        {
            if (string.IsNullOrWhiteSpace(modId) || annotation == null)
                continue;

            var value = new ModAnnotation
            {
                Alias = (annotation.Alias ?? string.Empty).Trim(),
                Notes = (annotation.Notes ?? string.Empty)
                    .Replace("\r\n", "\n", StringComparison.Ordinal)
                    .Replace('\r', '\n')
                    .Trim()
            };

            if (!string.IsNullOrEmpty(value.Alias) || !string.IsNullOrEmpty(value.Notes))
                normalized[modId.Trim()] = value;
        }

        return normalized;
    }
}
