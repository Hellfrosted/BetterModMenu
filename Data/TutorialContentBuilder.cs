namespace BetterModMenu.Data;

internal readonly record struct TutorialPage(string Title, string Body);

internal static class TutorialContentBuilder
{
    public static IReadOnlyList<TutorialPage> BuildPages(Func<string, string, string> text)
    {
        return
        [
            new(text(BmmText.ProfileLabel, "Profiles"),
                text(BmmText.TutorialProfiles, "Switch profiles to change your enabled mods. New copies your current setup.")),
            new(text(BmmText.GroupLabel, "Groups"), string.Join("\n\n",
                text(BmmText.TutorialGroups, "Add a group, then choose it on a mod's row. Use the group header to collapse or toggle the group."),
                text("TUTORIAL.ORDER_SHORT", "Select a mod and press Up or Down to change its saved order. Dependencies may change the final load order."))),
            new(text(BmmText.Backup, "Backup"),
                text(BmmText.TutorialBackup, "Backup saves your profiles and groups. Load restores a backup; it can also clean up old backups.")),
            new(text("TUTORIAL.MORE_SHORT", "More"), string.Join("\n\n",
                text(BmmText.TutorialPortable, "Portable Mode keeps the menu save beside the mod."),
                text(BmmText.TutorialStyle, "Style changes mod name colors."),
                text(BmmText.TutorialLogs, "Logs shows warnings and errors. CSV exports your mod list."),
                text(BmmText.TutorialCloud, "Cloud builds can copy backups and CSV exports to a synced folder.")))
        ];
    }
}
