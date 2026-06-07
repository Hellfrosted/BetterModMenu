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
        return IsPathWithinDirectory(fullDirectory, fullCandidate) &&
            File.Exists(fullCandidate) &&
            TryReadManifestId(fullCandidate, out string manifestId) &&
            string.Equals(manifestId, expectedId, StringComparison.OrdinalIgnoreCase);
    }

    private static bool TryReadManifestId(string manifestPath, out string manifestId)
    {
        manifestId = string.Empty;
        try
        {
            string content = File.ReadAllText(manifestPath);
            var docOpts = new JsonDocumentOptions
            {
                CommentHandling = JsonCommentHandling.Skip,
                AllowTrailingCommas = true
            };

            using var doc = JsonDocument.Parse(content, docOpts);
            manifestId = ReadString(doc.RootElement, "id");
            return !string.IsNullOrWhiteSpace(manifestId);
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

    public static bool TryReadManifestInfo(string manifestPath, string expectedId, out ModManifestInfo manifestInfo)
    {
        manifestInfo = new ModManifestInfo();

        string content = File.ReadAllText(manifestPath);
        var docOpts = new JsonDocumentOptions
        {
            CommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true
        };

        using var doc = JsonDocument.Parse(content, docOpts);
        if (!doc.RootElement.TryGetProperty("id", out var idProp) || idProp.ValueKind != JsonValueKind.String)
            return false;

        string manifestId = idProp.GetString() ?? string.Empty;
        if (!string.Equals(manifestId, expectedId, StringComparison.OrdinalIgnoreCase))
            return false;

        manifestInfo = new ModManifestInfo
        {
            Id = manifestId,
            Name = ReadString(doc.RootElement, "name"),
            Version = ReadString(doc.RootElement, "version")
        };
        return true;
    }

    public static bool TryReadVersion(string manifestPath, string expectedId, out string version)
    {
        version = string.Empty;
        if (string.IsNullOrWhiteSpace(manifestPath) || !File.Exists(manifestPath))
            return false;

        string content = File.ReadAllText(manifestPath);
        var docOpts = new JsonDocumentOptions
        {
            CommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true
        };

        using var doc = JsonDocument.Parse(content, docOpts);
        if (!doc.RootElement.TryGetProperty("id", out var idProp) ||
            idProp.ValueKind != JsonValueKind.String ||
            !string.Equals(idProp.GetString(), expectedId, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        version = ReadString(doc.RootElement, "version");
        return !string.IsNullOrWhiteSpace(version);
    }

    private static string ReadString(JsonElement element, string propertyName)
    {
        return element.TryGetProperty(propertyName, out var property) && property.ValueKind == JsonValueKind.String
            ? property.GetString() ?? string.Empty
            : string.Empty;
    }
}
