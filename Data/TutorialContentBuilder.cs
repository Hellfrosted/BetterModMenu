namespace BetterModMenu.Data;

internal static class TutorialContentBuilder
{
    public static string BuildBody()
    {
        return string.Join("\n\n",
            "Better Mod Menu helps you keep different mod setups without rebuilding your list by hand every time.",
            "Profiles are saved mod setups. Switch profiles to turn a saved set of mods on or off. New copies what is enabled right now into a new profile; Rename changes only the profile name; Del removes the selected profile.",
            "Groups are labels for organizing the list. Type a group name, press Add, then use each mod row's group picker to move mods into that group. Group headers can collapse the section, move the group, rename it, delete it, or turn every mod in the group on or off together.",
            "Portable Mode stores Better Mod Menu's save file beside the mod files. Leave it off for the normal game save location; turn it on when you want this mod setup to travel with a copied game or mod folder.",
            "Backup saves copies of your Better Mod Menu profiles, groups, and the game's current enabled-mod settings. Load lets you choose a profile and group backup to restore. CSV creates a spreadsheet-friendly list of installed mods, versions, enabled state, and group names.",
            "The Logs button opens recent BetterModMenu/TTSMM log output when you need to see what happened.",
            "Cloud-capable builds can mirror backups and CSV exports to a synced folder, but cloud behavior stays opt-in.");
    }
}
