namespace BetterModMenu.Data;

public sealed class GameVersionEntry
{
    public string DisplayName { get; set; } = string.Empty;
    public uint AppId { get; set; }
    public uint DepotId { get; set; }
    public ulong ManifestId { get; set; }
    public string BuildId { get; set; } = string.Empty;
}

public sealed class GameVersionDownloadSettings
{
    public bool Enabled { get; set; }
    public string SteamCmdPath { get; set; } = "steamcmd";
    public string InstallRootDirectory { get; set; } = string.Empty;
    public string SelectedVersion { get; set; } = string.Empty;
    public List<GameVersionEntry> Versions { get; set; } = new();
}

internal sealed class GameVersionValidationResult
{
    public bool IsValid { get; init; }
    public string Error { get; init; } = string.Empty;
}

internal sealed class GameVersionDownloadPlan
{
    public GameVersionEntry Version { get; init; } = new();
    public string InstallDirectory { get; init; } = string.Empty;
    public IReadOnlyList<string> Arguments { get; init; } = Array.Empty<string>();
    public string CommandLine { get; init; } = string.Empty;
}

internal static class GameVersionSelectionRules
{
    public static GameVersionValidationResult Validate(GameVersionEntry entry)
    {
        if (string.IsNullOrWhiteSpace(entry.DisplayName))
            return Invalid("Display name is required.");

        if (entry.AppId == 0)
            return Invalid("Steam app id is required.");

        if (entry.DepotId == 0)
            return Invalid("Steam depot id is required.");

        if (entry.ManifestId == 0)
            return Invalid("Steam manifest id is required.");

        return new GameVersionValidationResult { IsValid = true };
    }

    public static bool TrySelectVersion(
        IEnumerable<GameVersionEntry> entries,
        string displayName,
        out GameVersionEntry selected,
        out string? error)
    {
        selected = new GameVersionEntry();
        error = null;

        var match = entries.FirstOrDefault(entry => string.Equals(entry.DisplayName, displayName, StringComparison.OrdinalIgnoreCase));
        if (match == null)
        {
            error = "Requested game version was not found.";
            return false;
        }

        var validation = Validate(match);
        if (!validation.IsValid)
        {
            error = validation.Error;
            return false;
        }

        selected = match;
        return true;
    }

    public static IReadOnlyList<string> BuildSteamCmdDownloadDepotArguments(GameVersionEntry entry, string installDirectory)
    {
        var validation = Validate(entry);
        if (!validation.IsValid)
            throw new ArgumentException(validation.Error, nameof(entry));

        if (string.IsNullOrWhiteSpace(installDirectory))
            throw new ArgumentException("Install directory is required.", nameof(installDirectory));

        return new[]
        {
            "+force_install_dir",
            installDirectory,
            "+login",
            "anonymous",
            "+download_depot",
            entry.AppId.ToString(System.Globalization.CultureInfo.InvariantCulture),
            entry.DepotId.ToString(System.Globalization.CultureInfo.InvariantCulture),
            entry.ManifestId.ToString(System.Globalization.CultureInfo.InvariantCulture),
            "+quit"
        };
    }

    public static bool TryBuildDownloadPlan(GameVersionDownloadSettings settings, out GameVersionDownloadPlan plan, out string? error)
    {
        plan = new GameVersionDownloadPlan();
        error = null;

        if (!settings.Enabled)
        {
            error = "Game version downloads are not enabled.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(settings.SteamCmdPath))
        {
            error = "SteamCMD path is required.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(settings.InstallRootDirectory))
        {
            error = "Install root directory is required.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(settings.SelectedVersion))
        {
            error = "Selected game version is required.";
            return false;
        }

        if (!TrySelectVersion(settings.Versions, settings.SelectedVersion, out var selected, out error))
            return false;

        string installDirectory = CombineInstallDirectory(settings.InstallRootDirectory, SanitizePathSegment(selected.DisplayName));
        var args = BuildSteamCmdDownloadDepotArguments(selected, installDirectory);
        plan = new GameVersionDownloadPlan
        {
            Version = selected,
            InstallDirectory = installDirectory,
            Arguments = args,
            CommandLine = BuildCommandLine(settings.SteamCmdPath, args)
        };
        return true;
    }

    private static GameVersionValidationResult Invalid(string error)
    {
        return new GameVersionValidationResult
        {
            IsValid = false,
            Error = error
        };
    }

    private static string SanitizePathSegment(string value)
    {
        string sanitized = value.Trim();
        foreach (char invalidChar in Path.GetInvalidFileNameChars())
            sanitized = sanitized.Replace(invalidChar, '_');

        return string.IsNullOrWhiteSpace(sanitized) ? "selected-version" : sanitized;
    }

    private static string CombineInstallDirectory(string rootDirectory, string pathSegment)
    {
        string trimmedRoot = rootDirectory.TrimEnd('\\', '/');
        if (UsesWindowsSeparators(trimmedRoot))
            return trimmedRoot + "\\" + pathSegment;

        return Path.Combine(trimmedRoot, pathSegment);
    }

    private static bool UsesWindowsSeparators(string path)
    {
        return path.Contains('\\') ||
            (path.Length >= 2 && char.IsLetter(path[0]) && path[1] == ':');
    }

    private static string BuildCommandLine(string executable, IEnumerable<string> arguments)
    {
        return string.Join(" ", new[] { QuoteArgument(executable) }.Concat(arguments.Select(QuoteArgument)));
    }

    private static string QuoteArgument(string argument)
    {
        if (argument.Length == 0)
            return "\"\"";

        bool needsQuotes = argument.Any(char.IsWhiteSpace) || argument.Contains('"');
        if (!needsQuotes)
            return argument;

        return "\"" + argument.Replace("\"", "\\\"") + "\"";
    }
}
