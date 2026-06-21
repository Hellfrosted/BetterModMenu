namespace BetterModMenu.Patches;

internal static class ModdingScreenConstants
{
    public const string ChromeRootName = "BetterModMenuChrome";
    public const string GroupDropdownPath = "RowCustomControls/GroupDropdown";
    public const string ModsScrollContentPath = "%ModsScrollContainer/Mask/Content";
    public const string TickboxPath = "Tickbox";
    public const string UnassignedGroup = "Unassigned";

    public const float TopBarGap = 10f;
    public const float TopBarTrailingPadding = 30f;
    public const float TopBarCompactThreshold = 470f;
    public const float TopBarFallbackX = 300f;
    public const float TopBarFallbackY = 55f;
    public const float TopBarFallbackWidth = 360f;
    public const float TopBarButtonMinWidth = 62f;
    public const float TopBarButtonCompactWidth = 52f;
    public const float ToolbarControlHeight = 30f;

    public const float GroupBarCompactThreshold = 980f;
    public const float GroupBarFallbackX = 550f;
    public const float GroupBarFallbackY = 30f;
    public const float GroupBarFallbackWidth = 400f;
    public const float GroupBarWideHeight = 28f;
    public const float GroupBarCompactHeight = 58f;
    public const float GroupBarListGap = 4f;
    public const float SearchInputWidth = 220f;
    public const float SearchBarHeight = 30f;
    public const float SearchBarListGap = 4f;
    public const float GroupInputWideWidth = 140f;
    public const float GroupInputCompactWidth = 120f;

    public const float RowButtonSize = 32f;
    public const float RowDropdownWidth = 140f;
    public const float RowDropdownCompactWidth = 112f;
    public const float RowControlsRightPadding = 12f;
    public const float RowNativeTickboxReserveWidth = 84f;
    public const float RowMinimumLeftContentWidth = 480f;
    public const float RowGameplayIndicatorSlotWidth = 22f;
    public const float RowGameplayIndicatorSize = 14f;
    public const float GroupHeaderScrollbarReserveWidth = 52f;
    public const float ScrollFitTolerance = 1f;

    public const float DetailActionHorizontalInset = 28f;
    public const float DetailActionBottomInset = 18f;
    public const int DetailActionGap = 10;
    public const float DetailStatusLineHeight = 32f;
    public const float DetailConfigButtonHeight = 126f;
    public const float DetailDescriptionActionGap = 12f;
    public const float DetailActionContentHeight =
        DetailStatusLineHeight + DetailConfigButtonHeight + DetailActionGap;
    public const float DetailActionPanelHeight =
        DetailActionContentHeight + DetailActionBottomInset;
    public const float DetailDescriptionBottomInset =
        DetailActionPanelHeight + DetailDescriptionActionGap;
    public const int DetailConfigButtonFontSize = 34;
    public const int DetailConfigBadgeFontSize = 16;
}
