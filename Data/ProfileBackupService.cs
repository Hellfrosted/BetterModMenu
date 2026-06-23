using System;
using System.IO;

namespace BetterModMenu.Data;

internal enum ProfileBackupReason
{
    RunStart,
    Resume,
    Manual
}

internal readonly record struct ProfileBackupEntry(string Path, string Label, ProfileBackupReason? Reason);

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
                .Select(file =>
                {
                    ProfileBackupReason? reason = GetReason(file.Name);
                    return new ProfileBackupEntry(file.FullName, BuildBackupLabel(file, reason), reason);
                })
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
        return FileNameCollisionRules.GetUniquePath(backupDirectory, fileName);
    }

    private static string NormalizeExtension(string extension)
    {
        return extension.StartsWith(".", StringComparison.Ordinal) ? extension : "." + extension;
    }

    private static string BuildBackupLabel(FileInfo file, ProfileBackupReason? reason)
    {
        return $"{file.LastWriteTime:yyyy-MM-dd HH:mm} - {GetReasonLabel(reason)}";
    }

    private static ProfileBackupReason? GetReason(string fileName)
    {
        if (fileName.Contains(".manual.", StringComparison.OrdinalIgnoreCase))
            return ProfileBackupReason.Manual;

        if (fileName.Contains(".resume.", StringComparison.OrdinalIgnoreCase))
            return ProfileBackupReason.Resume;

        if (fileName.Contains(".runstart.", StringComparison.OrdinalIgnoreCase))
            return ProfileBackupReason.RunStart;

        return null;
    }

    private static string GetReasonLabel(ProfileBackupReason? reason)
    {
        return reason switch
        {
            ProfileBackupReason.Manual => "Manual backup",
            ProfileBackupReason.Resume => "Auto backup",
            ProfileBackupReason.RunStart => "Startup backup",
            _ => "Backup"
        };
    }
}
