using System.Text.Json;

namespace BetterModMenu.Data;

internal sealed class ModSettingsBackupRow
{
    public string ModId { get; init; } = string.Empty;
    public bool Enabled { get; init; }
    public string WorkshopUrl { get; init; } = string.Empty;
}

internal static class ModSettingsBackupService
{
    public const string ErrorNoBackupDirectory = "No backup directory was provided.";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    public static bool TryWriteSnapshot(
        string directory,
        IEnumerable<InstalledModExportInput> mods,
        ProfileBackupReason reason,
        DateTimeOffset timestamp,
        out string backupPath,
        out string? error)
    {
        backupPath = string.Empty;
        error = null;

        try
        {
            if (string.IsNullOrWhiteSpace(directory))
                throw new InvalidOperationException(ErrorNoBackupDirectory);

            var rows = mods
                .Where(mod => !string.IsNullOrWhiteSpace(mod.ModId))
                .Select(mod => new ModSettingsBackupRow
                {
                    ModId = mod.ModId,
                    Enabled = mod.Enabled,
                    WorkshopUrl = mod.WorkshopUrl
                })
                .OrderBy(row => row.ModId, StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (rows.Count == 0)
                return false;

            Directory.CreateDirectory(directory);
            backupPath = GetUniqueBackupPath(directory, reason, timestamp);
            File.WriteAllText(backupPath, JsonSerializer.Serialize(rows, JsonOptions));
            return true;
        }
        catch (Exception ex)
        {
            backupPath = string.Empty;
            error = ex.Message;
            return false;
        }
    }

    public static string BuildBackupFileName(ProfileBackupReason reason, DateTimeOffset timestamp)
    {
        string reasonSlug = reason.ToString().ToLowerInvariant();
        return $"mod_settings.{timestamp.UtcDateTime:yyyyMMdd-HHmmss}.{reasonSlug}.json";
    }

    public static bool TryPruneAutomaticBackups(string directory, int retentionCount, out string? error)
    {
        error = null;

        try
        {
            ArgumentOutOfRangeException.ThrowIfNegative(retentionCount);

            if (string.IsNullOrWhiteSpace(directory) || !Directory.Exists(directory))
                return true;

            var expiredBackups = Directory
                .EnumerateFiles(directory, "mod_settings.*.json")
                .Select(path => new FileInfo(path))
                .Where(file =>
                    ProfileBackupService.TryGetGeneratedBackupReason(file.Name, "mod_settings", out var reason) &&
                    reason is ProfileBackupReason.RunStart or ProfileBackupReason.Resume)
                .OrderByDescending(file => file.LastWriteTimeUtc)
                .ThenByDescending(file => file.Name, StringComparer.Ordinal)
                .Skip(retentionCount)
                .ToList();

            foreach (var backup in expiredBackups)
                backup.Delete();

            return true;
        }
        catch (Exception ex)
        {
            error = ex.Message;
            return false;
        }
    }

    private static string GetUniqueBackupPath(string directory, ProfileBackupReason reason, DateTimeOffset timestamp)
    {
        string fileName = BuildBackupFileName(reason, timestamp);
        return FileNameCollisionRules.GetUniquePath(directory, fileName);
    }
}
