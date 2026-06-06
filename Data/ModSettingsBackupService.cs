using System.Text.Json;

namespace BetterModMenu.Data;

internal sealed class ModSettingsBackupRow
{
    public string ModId { get; init; } = string.Empty;
    public bool Enabled { get; init; }
}

internal static class ModSettingsBackupService
{
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
                throw new InvalidOperationException("No backup directory was provided.");

            var rows = mods
                .Where(mod => !string.IsNullOrWhiteSpace(mod.ModId))
                .Select(mod => new ModSettingsBackupRow
                {
                    ModId = mod.ModId,
                    Enabled = mod.Enabled
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

    private static string GetUniqueBackupPath(string directory, ProfileBackupReason reason, DateTimeOffset timestamp)
    {
        string fileName = BuildBackupFileName(reason, timestamp);
        string candidate = Path.Combine(directory, fileName);
        if (!File.Exists(candidate))
            return candidate;

        string baseName = Path.GetFileNameWithoutExtension(fileName);
        for (int i = 2; ; i++)
        {
            candidate = Path.Combine(directory, $"{baseName}-{i}.json");
            if (!File.Exists(candidate))
                return candidate;
        }
    }
}
