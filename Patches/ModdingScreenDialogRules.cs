using System;

namespace BetterModMenu.Patches;

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

internal readonly record struct LogDialogLayout(
    int PopupWidth,
    int PopupHeight,
    int PanelWidth,
    int PanelHeight,
    int ScrollHeight,
    int BodyFontSize,
    int ButtonFontSize,
    int ActionRowHeight,
    int ToolbarGap);

internal readonly record struct StyleEditorDialogLayout(
    int PopupWidth,
    int PopupHeight,
    int PanelWidth,
    int ScrollHeight,
    int RowHeight,
    int LabelWidth,
    int SettingWidth,
    int SwatchSize,
    int PreviewHeight,
    int BodyFontSize,
    int ButtonFontSize,
    int HorizontalMargin,
    int VerticalMargin);

internal readonly record struct BackupSelectionPage(
    int StartIndex,
    int ItemCount,
    int PageIndex,
    int PageCount);

internal static class ModdingScreenDialogRules
{
    public static BackupSelectionPage GetBackupSelectionPage(int backupCount, int requestedPageIndex, int pageSize)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(backupCount);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(pageSize);

        int pageCount = backupCount == 0 ? 0 : 1 + ((backupCount - 1) / pageSize);
        int pageIndex = pageCount == 0 ? 0 : Math.Clamp(requestedPageIndex, 0, pageCount - 1);
        int startIndex = pageIndex * pageSize;
        int itemCount = Math.Min(pageSize, backupCount - startIndex);
        return new BackupSelectionPage(startIndex, itemCount, pageIndex, pageCount);
    }

    public static LogDialogLayout GetPreferredLogDialogLayout()
    {
        return new LogDialogLayout(
            PopupWidth: 1080,
            PopupHeight: 680,
            PanelWidth: 1020,
            PanelHeight: 520,
            ScrollHeight: 508,
            BodyFontSize: 24,
            ButtonFontSize: 22,
            ActionRowHeight: 44,
            ToolbarGap: 8);
    }

    public static TutorialDialogLayout GetPreferredTutorialDialogLayout()
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

    public static StyleEditorDialogLayout GetPreferredStyleEditorDialogLayout()
    {
        return new StyleEditorDialogLayout(
            PopupWidth: 900,
            PopupHeight: 690,
            PanelWidth: 820,
            ScrollHeight: 540,
            RowHeight: 64,
            LabelWidth: 250,
            SettingWidth: 500,
            SwatchSize: 44,
            PreviewHeight: 52,
            BodyFontSize: 20,
            ButtonFontSize: 22,
            HorizontalMargin: 120,
            VerticalMargin: 70);
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

    public static StyleEditorDialogLayout FitStyleEditorDialogToViewport(StyleEditorDialogLayout preferred, int viewportWidth, int viewportHeight)
    {
        int popupWidth = Math.Max(760, Math.Min(preferred.PopupWidth, viewportWidth - preferred.HorizontalMargin));
        int popupHeight = Math.Max(560, Math.Min(preferred.PopupHeight, viewportHeight - preferred.VerticalMargin));
        return preferred with
        {
            PopupWidth = popupWidth,
            PopupHeight = popupHeight,
            PanelWidth = Math.Max(700, popupWidth - 80),
            ScrollHeight = Math.Max(420, popupHeight - 150),
            SettingWidth = Math.Max(420, popupWidth - 400)
        };
    }
}
