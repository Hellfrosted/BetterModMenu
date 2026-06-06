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
            new TopBarButtonPresentation(isCompact ? "New" : "+ New", "New profile"),
            new TopBarButtonPresentation(isCompact ? "Edit" : "Rename", "Rename profile"),
            new TopBarButtonPresentation("Del", "Delete profile"),
            isCompact ? ModdingScreenConstants.TopBarButtonCompactWidth : ModdingScreenConstants.TopBarButtonMinWidth);
    }
}
