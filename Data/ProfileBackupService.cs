using System;
using System.IO;

namespace BetterModMenu.Data;

internal enum ProfileBackupReason
{
    RunStart,
    Resume,
    Manual
}

internal readonly record struct ProfileBackupEntry(string Path, string Label);

internal static class ProfileBackupService
{
    public static bool TryListBackups(
        string savePath,
        IReadOnlyCollection<string> configExtensions,
        out IReadOnlyList<ProfileBackupEntry> backups,
        out string? error)
    {
        backups = Array.Empty<ProfileBackupEntry>();
        error = null;

        try
        {
            string? directory = Path.GetDirectoryName(savePath);
            if (string.IsNullOrWhiteSpace(directory))
                return false;

            string backupDirectory = Path.Combine(directory, "backups");
            if (!Directory.Exists(backupDirectory))
                return false;

            string baseName = Path.GetFileNameWithoutExtension(savePath);
            var extensions = configExtensions
                .Where(extension => !string.IsNullOrWhiteSpace(extension))
                .Select(NormalizeExtension)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            backups = Directory
                .EnumerateFiles(backupDirectory, baseName + ".*")
                .Select(path => new FileInfo(path))
                .Where(file => extensions.Contains(file.Extension))
                .OrderByDescending(file => file.LastWriteTimeUtc)
                .ThenByDescending(file => file.Name, StringComparer.Ordinal)
                .Select(file => new ProfileBackupEntry(file.FullName, BuildBackupLabel(file)))
                .ToList();

            return backups.Count > 0;
        }
        catch (Exception ex)
        {
            error = ex.Message;
            backups = Array.Empty<ProfileBackupEntry>();
            return false;
        }
    }

    public static bool TryBackupExistingSave(string savePath, ProfileBackupReason reason, DateTimeOffset timestamp, out string backupPath, out string? error)
    {
        backupPath = string.Empty;
        error = null;

        try
        {
            if (string.IsNullOrWhiteSpace(savePath) || !File.Exists(savePath))
                return false;

            string? directory = Path.GetDirectoryName(savePath);
            if (string.IsNullOrWhiteSpace(directory))
                return false;

            string backupDirectory = Path.Combine(directory, "backups");
            Directory.CreateDirectory(backupDirectory);

            backupPath = GetUniqueBackupPath(backupDirectory, savePath, reason, timestamp);
            File.Copy(savePath, backupPath);
            return true;
        }
        catch (Exception ex)
        {
            error = ex.Message;
            backupPath = string.Empty;
            return false;
        }
    }

    public static string BuildBackupFileName(string savePath, ProfileBackupReason reason, DateTimeOffset timestamp)
    {
        string baseName = Path.GetFileNameWithoutExtension(savePath);
        string extension = Path.GetExtension(savePath);
        string reasonSlug = reason.ToString().ToLowerInvariant();
        return $"{baseName}.{timestamp.UtcDateTime:yyyyMMdd-HHmmss}.{reasonSlug}{extension}";
    }

    private static string GetUniqueBackupPath(string backupDirectory, string savePath, ProfileBackupReason reason, DateTimeOffset timestamp)
    {
        string fileName = BuildBackupFileName(savePath, reason, timestamp);
        string candidate = Path.Combine(backupDirectory, fileName);
        if (!File.Exists(candidate))
            return candidate;

        string baseName = Path.GetFileNameWithoutExtension(fileName);
        string extension = Path.GetExtension(fileName);
        for (int i = 2; ; i++)
        {
            candidate = Path.Combine(backupDirectory, $"{baseName}-{i}{extension}");
            if (!File.Exists(candidate))
                return candidate;
        }
    }

    private static string NormalizeExtension(string extension)
    {
        return extension.StartsWith(".", StringComparison.Ordinal) ? extension : "." + extension;
    }

    private static string BuildBackupLabel(FileInfo file)
    {
        return $"{file.LastWriteTime:yyyy-MM-dd HH:mm} - {GetReasonLabel(file.Name)}";
    }

    private static string GetReasonLabel(string fileName)
    {
        if (fileName.Contains(".manual.", StringComparison.OrdinalIgnoreCase))
            return "Manual backup";

        if (fileName.Contains(".resume.", StringComparison.OrdinalIgnoreCase))
            return "Auto backup";

        if (fileName.Contains(".runstart.", StringComparison.OrdinalIgnoreCase))
            return "Startup backup";

        return "Backup";
    }
}
