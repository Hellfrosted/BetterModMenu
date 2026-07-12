using BetterModMenu.Data;

namespace BetterModMenu.Patches;

internal readonly record struct TopBarButtonPresentation(string Text, string TooltipKey);

internal readonly record struct TopBarPresentation(
    TopBarButtonPresentation NewProfile,
    TopBarButtonPresentation RenameProfile,
    TopBarButtonPresentation DeleteProfile,
    float ButtonWidth);

internal readonly record struct VisibleRowSpan(float Left, float Right)
{
    public float Width => Math.Max(0f, Right - Left);
}

internal static class ModdingScreenLayoutRules
{
    public static VisibleRowSpan IntersectVisibleRowSpan(
        VisibleRowSpan current,
        float rowGlobalLeft,
        float clipGlobalLeft,
        float clipWidth)
    {
        float clipLeftInRow = clipGlobalLeft - rowGlobalLeft;
        float clipRightInRow = clipLeftInRow + Math.Max(0f, clipWidth);
        float left = Math.Max(current.Left, clipLeftInRow);
        float right = Math.Min(current.Right, clipRightInRow);
        if (right < left)
            right = left;

        return new VisibleRowSpan(left, right);
    }

    public static bool ShouldStackTopBar(float availableInlineWidth, float requiredWidth)
    {
        return availableInlineWidth < requiredWidth;
    }

    public static bool ShouldShowRowMoveButtons(float rowWidth, float controlsWidth)
    {
        float trailingInset = ModdingScreenConstants.RowControlsRightPadding + ModdingScreenConstants.RowNativeTickboxReserveWidth;
        return rowWidth <= 0 ||
               rowWidth - trailingInset - controlsWidth >= ModdingScreenConstants.RowMinimumCompactLeftContentWidth;
    }

    public static TopBarPresentation GetTopBarPresentation(bool isCompact)
    {
        return new TopBarPresentation(
            new TopBarButtonPresentation(string.Empty, BmmText.NewProfileTooltip),
            new TopBarButtonPresentation(string.Empty, BmmText.RenameProfileTooltip),
            new TopBarButtonPresentation(string.Empty, BmmText.DeleteProfileTooltip),
            ModdingScreenConstants.TopBarButtonCompactWidth);
    }
}
