namespace BetterModMenu.Data;

internal static class TutorialContentBuilder
{
    public static string BuildBody()
    {
        return string.Join("\n\n",
            "Better Mod Menu helps you keep different mod setups without rebuilding your list by hand every time.",
            "Open it from Slay the Spire 2's built-in Modding screen. Better Mod Menu adds its Profile, Group, Backup, CSV, Logs, and Help controls to that screen.",
            "Profiles are saved mod setups. Switch profiles to turn a saved set of mods on or off. New copies what is enabled right now into a new profile; Rename changes only the profile name; Del removes the selected profile.",
            "Groups are labels for organizing the list. Type a group name, press Add, then use each mod row's group picker to move mods into that group. Group headers can collapse the section, move the group, rename it, delete it, or turn every mod in the group on or off together.",
            "Portable Mode stores Better Mod Menu's save file beside the mod files. Leave it off for the normal game save location; turn it on when you want this mod setup to travel with a copied game or mod folder.",
            "Backup saves copies of your Better Mod Menu profiles, groups, and the game's current enabled-mod settings, including Steam Workshop links when available. Load lets you choose a profile and group backup to restore. CSV creates a spreadsheet-friendly list of installed mods, versions, enabled state, group names, and Steam Workshop links when available.",
            "The Logs button opens full BetterModMenu/TTSMM log output with warnings and errors highlighted when you need to see what happened. Use Open Folder in the log viewer to open the folder that contains the log file.",
            "Cloud-capable builds can mirror backups and CSV exports to a synced folder, but cloud behavior stays opt-in.");
    }
}
