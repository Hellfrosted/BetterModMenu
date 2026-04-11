using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace BetterModMenu.Data;

internal static class ManifestScanner
{
    public static string? FindManifestPath(string directory, string modId, IEnumerable<string> extensions)
    {
        if (string.IsNullOrWhiteSpace(directory) || string.IsNullOrWhiteSpace(modId))
            return null;

        foreach (var extension in extensions)
        {
            string normalizedExtension = extension.StartsWith(".") ? extension : "." + extension;
            string candidate = Path.Combine(directory, modId + normalizedExtension);
            if (File.Exists(candidate))
                return candidate;
        }

        return null;
    }

    public static bool TryReadAffectsGameplay(string manifestPath, string expectedId, out bool affectsGameplay)
    {
        affectsGameplay = false;

        string content = File.ReadAllText(manifestPath);
        var docOpts = new JsonDocumentOptions
        {
            CommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true
        };

        using var doc = JsonDocument.Parse(content, docOpts);
        if (!doc.RootElement.TryGetProperty("id", out var idProp) || idProp.ValueKind != JsonValueKind.String)
            return false;

        string manifestId = idProp.GetString() ?? "";
        if (!string.Equals(manifestId, expectedId, StringComparison.OrdinalIgnoreCase))
            return false;

        if (!doc.RootElement.TryGetProperty("affects_gameplay", out var gameplayProp))
            return true;

        affectsGameplay = gameplayProp.ValueKind switch
        {
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            _ => false
        };

        return true;
    }
}
