namespace BetterModMenu.Patches;

internal readonly record struct TopBarButtonPresentation(string Text, string TooltipText);

internal readonly record struct TopBarPresentation(
    TopBarButtonPresentation NewProfile,
    TopBarButtonPresentation RenameProfile,
    TopBarButtonPresentation DeleteProfile,
    float ButtonWidth);

internal static class ModdingScreenLayoutRules
{
    public static TopBarPresentation GetTopBarPresentation(bool isCompact)
    {
        return new TopBarPresentation(
            new TopBarButtonPresentation(isCompact ? "New" : "+ New", "New profile: copy the current enabled/disabled mods into a separate saved setup."),
            new TopBarButtonPresentation(isCompact ? "Edit" : "Rename", "Rename profile: change the selected profile's name without changing its mods."),
            new TopBarButtonPresentation("Del", "Delete profile: remove the selected saved setup. Your installed mod files stay installed."),
            isCompact ? ModdingScreenConstants.TopBarButtonCompactWidth : ModdingScreenConstants.TopBarButtonMinWidth);
    }
}
