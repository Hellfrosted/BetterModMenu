namespace BetterModMenu.Data;

internal static class BackupTriggerRules
{
    public static bool ShouldCreateAutomaticBackup(ISet<ProfileBackupReason> completedReasons, ProfileBackupReason reason)
    {
        if (reason == ProfileBackupReason.Manual)
            return false;

        return !completedReasons.Contains(reason);
    }

    public static void MarkAutomaticBackupCreated(ISet<ProfileBackupReason> completedReasons, ProfileBackupReason reason)
    {
        if (reason != ProfileBackupReason.Manual)
            completedReasons.Add(reason);
    }
}
