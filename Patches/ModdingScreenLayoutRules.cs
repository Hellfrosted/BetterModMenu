using BetterModMenu.Data;

namespace BetterModMenu.Patches;

internal readonly record struct TopBarButtonPresentation(string Text, string TooltipKey);

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
            new TopBarButtonPresentation(string.Empty, BmmText.NewProfileTooltip),
            new TopBarButtonPresentation(string.Empty, BmmText.RenameProfileTooltip),
            new TopBarButtonPresentation(string.Empty, BmmText.DeleteProfileTooltip),
            ModdingScreenConstants.TopBarButtonCompactWidth);
    }
}
