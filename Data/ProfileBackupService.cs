using System;
using System.Globalization;
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
                    ProfileBackupReason? reason = TryGetGeneratedBackupReason(file.Name, baseName, out var generatedReason)
                        ? generatedReason
                        : null;
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

    public static bool TryPruneAutomaticBackups(
        string savePath,
        IReadOnlyCollection<string> configExtensions,
        int retentionCount,
        out string? error)
    {
        error = null;

        try
        {
            ArgumentOutOfRangeException.ThrowIfNegative(retentionCount);

            string? directory = Path.GetDirectoryName(savePath);
            if (string.IsNullOrWhiteSpace(directory))
                return true;

            string backupDirectory = Path.Combine(directory, "backups");
            if (!Directory.Exists(backupDirectory))
                return true;

            string baseName = Path.GetFileNameWithoutExtension(savePath);
            var extensions = configExtensions
                .Where(extension => !string.IsNullOrWhiteSpace(extension))
                .Select(NormalizeExtension)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            var expiredBackups = Directory
                .EnumerateFiles(backupDirectory, baseName + ".*")
                .Select(path => new FileInfo(path))
                .Where(file => extensions.Contains(file.Extension))
                .Where(file =>
                    TryGetGeneratedBackupReason(file.Name, baseName, out var reason) &&
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

    internal static bool TryGetGeneratedBackupReason(string fileName, string expectedBaseName, out ProfileBackupReason reason)
    {
        reason = default;
        string stem = Path.GetFileNameWithoutExtension(fileName);
        string prefix = expectedBaseName + ".";
        if (!stem.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            return false;

        string generatedSuffix = stem[prefix.Length..];
        int separatorIndex = generatedSuffix.IndexOf('.');
        if (separatorIndex < 0 ||
            !DateTime.TryParseExact(
                generatedSuffix[..separatorIndex],
                "yyyyMMdd-HHmmss",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out _))
        {
            return false;
        }

        string reasonSlug = generatedSuffix[(separatorIndex + 1)..];
        int collisionSuffixIndex = reasonSlug.LastIndexOf('-');
        if (collisionSuffixIndex > 0 &&
            int.TryParse(reasonSlug[(collisionSuffixIndex + 1)..], NumberStyles.None, CultureInfo.InvariantCulture, out int collisionNumber) &&
            collisionNumber > 0)
        {
            reasonSlug = reasonSlug[..collisionSuffixIndex];
        }

        if (reasonSlug.Equals("runstart", StringComparison.OrdinalIgnoreCase))
            reason = ProfileBackupReason.RunStart;
        else if (reasonSlug.Equals("resume", StringComparison.OrdinalIgnoreCase))
            reason = ProfileBackupReason.Resume;
        else if (reasonSlug.Equals("manual", StringComparison.OrdinalIgnoreCase))
            reason = ProfileBackupReason.Manual;
        else
            return false;

        return true;
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
