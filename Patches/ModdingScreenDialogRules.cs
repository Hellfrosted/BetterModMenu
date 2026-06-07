using System;

namespace BetterModMenu.Patches;

internal readonly record struct LogDialogLayout(
    int PopupWidth,
    int PopupHeight,
    int ContentWidth,
    int ContentHeight,
    int BodyFontSize,
    int ButtonFontSize);

internal readonly record struct TutorialDialogLayout(
    int PopupWidth,
    int PopupHeight,
    int ContentWidth,
    int ContentHeight,
    int BodyFontSize,
    int ButtonFontSize,
    int HorizontalMargin,
    int VerticalMargin,
    int ContentHorizontalPadding,
    int ContentVerticalPadding);

internal static class ModdingScreenDialogRules
{
    public static LogDialogLayout GetLogDialogLayout()
    {
        return new LogDialogLayout(
            PopupWidth: 1080,
            PopupHeight: 680,
            ContentWidth: 1020,
            ContentHeight: 600,
            BodyFontSize: 22,
            ButtonFontSize: 22);
    }

    public static TutorialDialogLayout GetTutorialDialogLayout()
    {
        return new TutorialDialogLayout(
            PopupWidth: 820,
            PopupHeight: 620,
            ContentWidth: 700,
            ContentHeight: 500,
            BodyFontSize: 22,
            ButtonFontSize: 24,
            HorizontalMargin: 140,
            VerticalMargin: 80,
            ContentHorizontalPadding: 120,
            ContentVerticalPadding: 190);
    }

    public static TutorialDialogLayout FitTutorialDialogToViewport(TutorialDialogLayout preferred, int viewportWidth, int viewportHeight)
    {
        int popupWidth = Math.Max(720, Math.Min(preferred.PopupWidth, viewportWidth - preferred.HorizontalMargin));
        int popupHeight = Math.Max(520, Math.Min(preferred.PopupHeight, viewportHeight - preferred.VerticalMargin));
        return preferred with
        {
            PopupWidth = popupWidth,
            PopupHeight = popupHeight,
            ContentWidth = Math.Max(640, popupWidth - preferred.ContentHorizontalPadding),
            ContentHeight = Math.Max(390, popupHeight - preferred.ContentVerticalPadding)
        };
    }
}
