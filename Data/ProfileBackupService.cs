using System;
using System.IO;

namespace BetterModMenu.Data;

internal enum ProfileBackupReason
{
    RunStart,
    Resume,
    Manual
}

internal static class ProfileBackupService
{
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
}
