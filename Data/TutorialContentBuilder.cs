namespace BetterModMenu.Data;

internal static class TutorialContentBuilder
{
    public static string BuildBody()
    {
        return string.Join("\n\n",
            "Better Mod Menu adds profiles, custom groups, saved ordering, local backups, CSV mod-list exports, log viewing, and SteamDB-derived game-version command previews.",
            "Use the profile controls above the mod list to switch, create, rename, or delete profiles.",
            "Use Backup to snapshot BMM profile data plus current mod enabled settings. Use CSV to export an Excel-friendly installed-mod list with versions and links when manifests provide them.",
            "Use Logs to inspect recent BetterModMenu/TTSMM output. Use Game to preview a configured SteamCMD download command for a saved game version.",
            "Cloud-capable builds can mirror backups and exports to a configured synced folder, but cloud behavior stays opt-in.");
    }
}
