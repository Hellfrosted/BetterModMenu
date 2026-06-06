namespace BetterModMenu.Data;

public sealed class CloudBackupSettings
{
    public bool Enabled { get; set; }
    public string Directory { get; set; } = string.Empty;
    public bool MirrorProfileBackups { get; set; } = true;
    public bool MirrorModSettingsBackups { get; set; } = true;
    public bool MirrorModListExports { get; set; } = true;
}

internal enum CloudBackupKind
{
    ProfileSettings,
    ModSettings,
    ModList
}

internal static class CloudBackupService
{
    public const string ProfileSettingsCategory = "profile-settings";
    public const string ModSettingsCategory = "mod-settings";
    public const string ModListCategory = "mod-lists";

    public static bool ShouldMirror(CloudBackupSettings settings, CloudBackupKind kind)
    {
        if (!settings.Enabled || string.IsNullOrWhiteSpace(settings.Directory))
            return false;

        return kind switch
        {
            CloudBackupKind.ProfileSettings => settings.MirrorProfileBackups,
            CloudBackupKind.ModSettings => settings.MirrorModSettingsBackups,
            CloudBackupKind.ModList => settings.MirrorModListExports,
            _ => false
        };
    }

    public static bool TryMirrorFile(
        CloudBackupSettings settings,
        CloudBackupKind kind,
        string sourcePath,
        out string mirroredPath,
        out string? error)
    {
        mirroredPath = string.Empty;
        error = null;

        try
        {
            if (!ShouldMirror(settings, kind))
                return false;

            if (string.IsNullOrWhiteSpace(sourcePath) || !File.Exists(sourcePath))
                return false;

            string targetDirectory = Path.Combine(settings.Directory, GetCategory(kind));
            Directory.CreateDirectory(targetDirectory);

            string fileName = Path.GetFileName(sourcePath);
            mirroredPath = GetUniqueMirrorPath(targetDirectory, fileName);
            File.Copy(sourcePath, mirroredPath);
            return true;
        }
        catch (Exception ex)
        {
            mirroredPath = string.Empty;
            error = ex.Message;
            return false;
        }
    }

    private static string GetCategory(CloudBackupKind kind)
    {
        return kind switch
        {
            CloudBackupKind.ProfileSettings => ProfileSettingsCategory,
            CloudBackupKind.ModSettings => ModSettingsCategory,
            CloudBackupKind.ModList => ModListCategory,
            _ => "other"
        };
    }

    private static string GetUniqueMirrorPath(string directory, string fileName)
    {
        string candidate = Path.Combine(directory, fileName);
        if (!File.Exists(candidate))
            return candidate;

        string baseName = Path.GetFileNameWithoutExtension(fileName);
        string extension = Path.GetExtension(fileName);
        for (int i = 2; ; i++)
        {
            candidate = Path.Combine(directory, $"{baseName}-{i}{extension}");
            if (!File.Exists(candidate))
                return candidate;
        }
    }
}
