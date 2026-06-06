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
    int ButtonFontSize);

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
            PopupWidth: 1260,
            PopupHeight: 720,
            ContentWidth: 1140,
            ContentHeight: 590,
            BodyFontSize: 24,
            ButtonFontSize: 24);
    }
}
