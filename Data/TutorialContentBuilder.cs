namespace BetterModMenu.Data;

internal static class TutorialContentBuilder
{
    public static string BuildBody()
    {
        return BuildBody((_, fallback) => fallback);
    }

    public static string BuildBody(Func<string, string, string> text)
    {
        return string.Join("\n\n",
            text(BmmText.TutorialIntro, "Better Mod Menu helps you keep different mod setups without rebuilding your list by hand every time."),
            text(BmmText.TutorialOpen, "Open it from Slay the Spire 2's built-in Modding screen. Better Mod Menu adds its Profile, Group, Backup, CSV, Logs, Style, and Help controls to that screen."),
            text(BmmText.TutorialProfiles, "Profiles are saved mod setups. Switch profiles to turn a saved set of mods on or off. New copies what is enabled right now into a new profile; Rename changes only the profile name; Del removes the selected profile."),
            text(BmmText.TutorialGroups, "Groups are labels for organizing the list. Type a group name, press Add, then use each mod row's group picker to move mods into that group. Group headers can collapse the section, move the group, rename it, delete it, or turn every mod in the group on or off together."),
            text(BmmText.TutorialPortable, "Portable Mode stores Better Mod Menu's save file beside the mod files. Leave it off for the normal game save location; turn it on when you want this mod setup to travel with a copied game or mod folder."),
            text(BmmText.TutorialBackup, "Backup saves copies of your Better Mod Menu profiles, groups, and the game's current enabled-mod settings, including Steam Workshop links when available. Load lets you choose a profile and group backup to restore. CSV creates a spreadsheet-friendly list of installed mods, versions, enabled state, group names, and Steam Workshop links when available."),
            text(BmmText.TutorialStyle, "Style customizes mod name colors inside the game. You can use the default Steam Workshop tag colors, override one supported tag, disable a tag color, or set a color for one specific mod id or displayed mod name."),
            text(BmmText.TutorialLogs, "The Logs button opens full Better Mod Menu log output with warnings and errors highlighted when you need to see what happened. Use its level toggles to show or hide debug, info, warning, error, and unclassified lines. Use Open Folder in the log viewer to open the folder that contains the log file."),
            text(BmmText.TutorialCloud, "Cloud-capable builds can mirror backups and CSV exports to a synced folder, but cloud behavior stays opt-in."));
    }
}
