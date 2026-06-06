namespace BetterModMenu.Data;

internal static class CloudBackupSettingsRules
{
    public static CloudBackupSettings WithDirectory(CloudBackupSettings current, string directory)
    {
        string trimmedDirectory = (directory ?? string.Empty).Trim();
        return new CloudBackupSettings
        {
            Enabled = !string.IsNullOrWhiteSpace(trimmedDirectory),
            Directory = trimmedDirectory,
            MirrorProfileBackups = current.MirrorProfileBackups,
            MirrorModSettingsBackups = current.MirrorModSettingsBackups,
            MirrorModListExports = current.MirrorModListExports
        };
    }
}
