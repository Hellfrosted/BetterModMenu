using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace BetterModMenu.Data;

internal sealed class ModManifestInfo
{
    public string Id { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Version { get; init; } = string.Empty;
    public bool AffectsGameplay { get; init; }
}

internal static class ManifestScanner
{
    private const string LegacyManifestBaseName = "mod_manifest";

    private static bool IsSafeManifestId(string modId)
    {
        return !string.IsNullOrWhiteSpace(modId) &&
            !Path.IsPathRooted(modId) &&
            modId.IndexOfAny(Path.GetInvalidFileNameChars()) == -1 &&
            !modId.Contains(Path.DirectorySeparatorChar) &&
            !modId.Contains(Path.AltDirectorySeparatorChar) &&
            Path.GetFileName(modId) == modId;
    }

    private static bool IsPathWithinDirectory(string directory, string path)
    {
        string normalizedDirectory = directory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        string expectedPrefix = normalizedDirectory + Path.DirectorySeparatorChar;
        return path.StartsWith(expectedPrefix, StringComparison.OrdinalIgnoreCase);
    }

    public static string? FindManifestPath(string directory, string modId, IEnumerable<string> extensions)
    {
        if (string.IsNullOrWhiteSpace(directory) || string.IsNullOrWhiteSpace(modId))
            return null;

        if (!IsSafeManifestId(modId))
            return null;

        string fullDirectory = Path.GetFullPath(directory);
        foreach (var extension in extensions)
        {
            string normalizedExtension = extension.StartsWith(".") ? extension : "." + extension;
            string candidate = Path.Combine(directory, modId + normalizedExtension);
            string fullCandidate = Path.GetFullPath(candidate);
            if (!IsPathWithinDirectory(fullDirectory, fullCandidate))
                continue;

            if (File.Exists(fullCandidate))
                return fullCandidate;
        }

        foreach (var extension in extensions)
        {
            string normalizedExtension = extension.StartsWith(".") ? extension : "." + extension;
            string candidate = Path.Combine(directory, LegacyManifestBaseName + normalizedExtension);
            if (IsMatchingManifestPath(fullDirectory, candidate, modId))
                return Path.GetFullPath(candidate);
        }

        foreach (var extension in extensions)
        {
            string normalizedExtension = extension.StartsWith(".") ? extension : "." + extension;
            foreach (string candidate in Directory.EnumerateFiles(fullDirectory, "*" + normalizedExtension, SearchOption.TopDirectoryOnly))
            {
                if (IsMatchingManifestPath(fullDirectory, candidate, modId))
                    return Path.GetFullPath(candidate);
            }
        }

        return null;
    }

    private static bool IsMatchingManifestPath(string fullDirectory, string candidate, string expectedId)
    {
        string fullCandidate = Path.GetFullPath(candidate);
        if (!IsPathWithinDirectory(fullDirectory, fullCandidate) || !File.Exists(fullCandidate))
            return false;

        try
        {
            return TryReadManifestInfo(fullCandidate, expectedId, out _);
        }
        catch (JsonException)
        {
            return false;
        }
        catch (IOException)
        {
            return false;
        }
    }

    public static bool TryReadManifestInfo(string manifestPath, string expectedId, out ModManifestInfo manifestInfo)
    {
        manifestInfo = new ModManifestInfo();

        if (string.IsNullOrWhiteSpace(manifestPath) || !File.Exists(manifestPath))
            return false;

        string content = File.ReadAllText(manifestPath);
        var docOpts = new JsonDocumentOptions
        {
            CommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true
        };

        using var doc = JsonDocument.Parse(content, docOpts);
        string manifestId = ReadString(doc.RootElement, "id");
        if (string.IsNullOrWhiteSpace(manifestId) ||
            !string.Equals(manifestId, expectedId, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        manifestInfo = new ModManifestInfo
        {
            Id = manifestId,
            Name = ReadString(doc.RootElement, "name"),
            Version = ReadString(doc.RootElement, "version"),
            AffectsGameplay = ReadBoolean(doc.RootElement, "affects_gameplay")
        };
        return true;
    }

    private static string ReadString(JsonElement element, string propertyName)
    {
        return element.TryGetProperty(propertyName, out var property) && property.ValueKind == JsonValueKind.String
            ? property.GetString() ?? string.Empty
            : string.Empty;
    }

    private static bool ReadBoolean(JsonElement element, string propertyName)
    {
        return element.TryGetProperty(propertyName, out var property) && property.ValueKind == JsonValueKind.True;
    }
}
