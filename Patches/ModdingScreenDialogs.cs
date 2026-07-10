using System;
using BetterModMenu.Data;
using Godot;
using MegaCrit.Sts2.Core.Nodes.Screens.ModdingScreen;

namespace BetterModMenu.Patches;

internal static class ModdingScreenDialogs
{
    private const int BackupChoicesPerPage = 12;

    private static string T(string key, string fallback) => ModdingScreenText.Get(key, fallback);

    private static string F(string key, string fallback, params object[] args) => ModdingScreenText.Format(key, fallback, args);

    public static void ShowInfoDialog(NModdingScreen screen, string title, string message)
    {
        TutorialDialogLayout layout = ModdingScreenDialogRules.GetPreferredTutorialDialogLayout();
        var popup = new AcceptDialog
        {
            Title = title,
            DialogText = string.Empty
        };
        ModdingScreenVanillaStyle.ApplyDialogWindow(popup);

        var label = CreateReadableBodyLabel(message, layout.BodyFontSize);
        label.CustomMinimumSize = new Vector2(Mathf.Min(720, layout.ContentWidth), 0);
        popup.AddChild(CreateStyledDialogShell(label, Mathf.Min(760, layout.ContentWidth + 40)));
        ApplyReadableDialogButtons(popup, layout.ButtonFontSize);
        screen.AddChild(popup);
        popup.PopupCentered(new Vector2I(860, 300));
    }

    public static void ShowConfirmDialog(NModdingScreen screen, string title, string message, Action onConfirmed)
    {
        TutorialDialogLayout layout = ModdingScreenDialogRules.GetPreferredTutorialDialogLayout();
        var popup = new ConfirmationDialog
        {
            Title = title,
            DialogText = string.Empty
        };
        ModdingScreenVanillaStyle.ApplyDialogWindow(popup);

        var label = CreateReadableBodyLabel(message, layout.BodyFontSize);
        label.CustomMinimumSize = new Vector2(Mathf.Min(760, layout.ContentWidth), 0);
        popup.AddChild(CreateStyledDialogShell(label, Mathf.Min(800, layout.ContentWidth + 50)));
        ApplyReadableDialogButtons(popup, layout.ButtonFontSize);
        popup.Confirmed += onConfirmed;
        screen.AddChild(popup);
        popup.PopupCentered(new Vector2I(900, 360));
    }

    public static void ShowBackupSelectionDialog(NModdingScreen screen, IReadOnlyList<ProfileBackupEntry> backups, Action<string> onConfirmed)
    {
        TutorialDialogLayout layout = ModdingScreenDialogRules.GetPreferredTutorialDialogLayout();
        var popup = new ConfirmationDialog
        {
            Title = T(BmmText.DialogLoadBackupTitle, "Load Backup"),
            DialogText = string.Empty
        };
        ModdingScreenVanillaStyle.ApplyDialogWindow(popup);

        var body = new VBoxContainer();
        body.AddThemeConstantOverride("separation", 8);
        var helpLabel = CreateReadableBodyLabel(
            T(BmmText.DialogLoadBackupBody, "Choose a backup to load. Installed mod files are not changed."),
            20);
        helpLabel.CustomMinimumSize = new Vector2(520, 0);
        body.AddChild(helpLabel);

        var backupDropdown = new OptionButton
        {
            CustomMinimumSize = new Vector2(520, ModdingScreenConstants.ToolbarControlHeight),
            TooltipText = T(BmmText.DialogLoadBackupOrderTooltip, "Backups are ordered from newest to oldest.")
        };
        ModdingScreenVanillaStyle.ApplyOptionButton(backupDropdown);
        body.AddChild(backupDropdown);

        var pageRow = new HBoxContainer();
        pageRow.AddThemeConstantOverride("separation", 8);
        string newerBackupsLabel = T(BmmText.DialogLoadBackupNewerPageTooltip, "Show newer backups.");
        var newerButton = new Button
        {
            Text = "<",
            TooltipText = newerBackupsLabel,
            AccessibilityName = newerBackupsLabel
        };
        ModdingScreenVanillaStyle.ApplyButton(newerButton);
        pageRow.AddChild(newerButton);
        var pageLabel = new Label
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
        };
        ModdingScreenVanillaStyle.ApplyLabel(pageLabel, muted: true);
        pageLabel.AddThemeFontSizeOverride("font_size", 18);
        pageRow.AddChild(pageLabel);
        string olderBackupsLabel = T(BmmText.DialogLoadBackupOlderPageTooltip, "Show older backups.");
        var olderButton = new Button
        {
            Text = ">",
            TooltipText = olderBackupsLabel,
            AccessibilityName = olderBackupsLabel
        };
        ModdingScreenVanillaStyle.ApplyButton(olderButton);
        pageRow.AddChild(olderButton);
        body.AddChild(pageRow);

        int requestedPageIndex = 0;
        int pageStartIndex = 0;
        void RefreshPage()
        {
            BackupSelectionPage page = ModdingScreenDialogRules.GetBackupSelectionPage(backups.Count, requestedPageIndex, BackupChoicesPerPage);
            requestedPageIndex = page.PageIndex;
            pageStartIndex = page.StartIndex;
            backupDropdown.Clear();
            for (int i = 0; i < page.ItemCount; i++)
                backupDropdown.AddItem(BuildBackupDisplayLabel(backups[page.StartIndex + i]), i);
            if (page.ItemCount > 0)
                backupDropdown.Select(0);

            int firstVisible = page.ItemCount == 0 ? 0 : page.StartIndex + 1;
            pageLabel.Text = $"{firstVisible}-{page.StartIndex + page.ItemCount} / {backups.Count}";
            newerButton.Disabled = page.PageIndex == 0;
            olderButton.Disabled = page.PageIndex + 1 >= page.PageCount;
        }

        newerButton.Pressed += () =>
        {
            requestedPageIndex--;
            RefreshPage();
        };
        olderButton.Pressed += () =>
        {
            requestedPageIndex++;
            RefreshPage();
        };
        RefreshPage();

        popup.AddChild(CreateStyledDialogShell(body, 560));
        ApplyReadableDialogButtons(popup, layout.ButtonFontSize);
        popup.Confirmed += () =>
        {
            int selectedIndex = backupDropdown.Selected;
            int backupIndex = pageStartIndex + selectedIndex;
            if (selectedIndex >= 0 && backupIndex < backups.Count)
                onConfirmed(backups[backupIndex].Path);
        };
        screen.AddChild(popup);
        popup.PopupCentered(new Vector2I(640, 220));
    }

    public static void ShowLogDialog(NModdingScreen screen, string title, string content, string logPath)
    {
        LogDialogLayout layout = ModdingScreenDialogRules.GetPreferredLogDialogLayout();
        if (content == LogViewerService.EmptyLogContent)
            content = T(BmmText.LogFileEmpty, LogViewerService.EmptyLogContent);

        var popup = new AcceptDialog
        {
            Title = title,
            DialogText = string.Empty
        };
        ModdingScreenVanillaStyle.ApplyDialogWindow(popup);

        popup.AddChild(CreateLogDialogBody(layout, content, () => OpenLogFolder(screen, logPath)));
        ApplyReadableDialogButtons(popup, layout.ButtonFontSize);
        screen.AddChild(popup);
        popup.PopupCentered(new Vector2I(layout.PopupWidth, layout.PopupHeight));
    }

    private static Control CreateLogDialogBody(LogDialogLayout layout, string content, Action onOpenFolderPressed)
    {
        LogLevelFilter includedLevels = LogLevelFilter.All;
        string displayedContent = content;
        int renderVersion = 0;
        var dialogBox = new VBoxContainer
        {
            CustomMinimumSize = new Vector2(layout.PanelWidth, layout.PanelHeight + layout.ActionRowHeight + layout.ToolbarGap),
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            SizeFlagsVertical = Control.SizeFlags.ExpandFill
        };
        dialogBox.AddThemeConstantOverride("separation", layout.ToolbarGap);

        var toolbarPanel = new PanelContainer
        {
            CustomMinimumSize = new Vector2(layout.PanelWidth, layout.ActionRowHeight),
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
        };
        ModdingScreenVanillaStyle.ApplyLogToolbarPanel(toolbarPanel);
        var actionRow = new HBoxContainer
        {
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            CustomMinimumSize = new Vector2(0, layout.ActionRowHeight)
        };
        actionRow.AddThemeConstantOverride("separation", 8);

        var panel = new PanelContainer
        {
            CustomMinimumSize = new Vector2(layout.PanelWidth, layout.PanelHeight)
        };
        ModdingScreenVanillaStyle.ApplyLogPanel(panel);

        var label = new RichTextLabel
        {
            BbcodeEnabled = false,
            Text = displayedContent,
            SelectionEnabled = false,
            ContextMenuEnabled = false,
            ScrollActive = true,
            FitContent = false,
            Threaded = true,
            ProgressBarDelay = 250,
            FocusMode = Control.FocusModeEnum.None,
            CustomMinimumSize = new Vector2(0, layout.ScrollHeight),
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            SizeFlagsVertical = Control.SizeFlags.ExpandFill
        };
        label.AddThemeColorOverride("default_color", new Color(0.96f, 0.91f, 0.82f, 1f));
        label.AddThemeFontSizeOverride("normal_font_size", layout.BodyFontSize);
        label.AddThemeFontSizeOverride("bold_font_size", layout.BodyFontSize);
        label.AddThemeFontSizeOverride("italics_font_size", layout.BodyFontSize);
        label.AddThemeFontSizeOverride("bold_italics_font_size", layout.BodyFontSize);

        void RefreshLogText()
        {
            displayedContent = LogLevelFilterService.Filter(content, includedLevels);
            int currentRenderVersion = ++renderVersion;
            label.BbcodeEnabled = false;
            label.Text = displayedContent;
            Callable.From(() => ApplyHighlightedLogText(currentRenderVersion)).CallDeferred();
        }

        void ApplyHighlightedLogText(int expectedRenderVersion)
        {
            if (expectedRenderVersion != renderVersion)
                return;

            label.BbcodeEnabled = true;
            label.Text = LogHighlightService.BuildHighlightedBbCode(displayedContent);
        }

        var copyButton = new Button
        {
            Text = T(BmmText.LogCopyAll, "Copy All"),
            TooltipText = T(BmmText.LogCopyAllTooltip, "Copy the full displayed log text to the clipboard")
        };
        ModdingScreenVanillaStyle.ApplyButton(copyButton);
        copyButton.Pressed += () => DisplayServer.ClipboardSet(displayedContent);
        actionRow.AddChild(copyButton);

        var openFolderButton = new Button
        {
            Text = T(BmmText.LogOpenFolder, "Open Folder"),
            TooltipText = T(BmmText.LogOpenFolderTooltip, "Open the folder that contains this log file.")
        };
        ModdingScreenVanillaStyle.ApplyButton(openFolderButton);
        openFolderButton.Pressed += onOpenFolderPressed;
        actionRow.AddChild(openFolderButton);

        var spacer = new Control
        {
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
        };
        actionRow.AddChild(spacer);

        var levelLabel = new Label
        {
            Text = T(BmmText.LogLevels, "Levels"),
            TooltipText = T(BmmText.LogLevelsTooltip, "Checked levels are shown. Uncheck a level to exclude it.")
        };
        ModdingScreenVanillaStyle.ApplyLabel(levelLabel);
        levelLabel.AddThemeFontSizeOverride("font_size", layout.ButtonFontSize);
        actionRow.AddChild(levelLabel);

        actionRow.AddChild(CreateLogLevelToggle(T(BmmText.LogLevelDebug, "Debug"), LogLevelFilter.Debug));
        actionRow.AddChild(CreateLogLevelToggle(T(BmmText.LogLevelInfo, "Info"), LogLevelFilter.Info));
        actionRow.AddChild(CreateLogLevelToggle(T(BmmText.LogLevelWarn, "Warn"), LogLevelFilter.Warning));
        actionRow.AddChild(CreateLogLevelToggle(T(BmmText.LogLevelError, "Error"), LogLevelFilter.Error));
        actionRow.AddChild(CreateLogLevelToggle(T(BmmText.LogLevelOther, "Other"), LogLevelFilter.Other));

        Callable.From(RefreshLogText).CallDeferred();
        panel.AddChild(label);
        toolbarPanel.AddChild(actionRow);
        dialogBox.AddChild(toolbarPanel);
        dialogBox.AddChild(panel);

        CheckButton CreateLogLevelToggle(string text, LogLevelFilter level)
        {
            var toggle = new CheckButton
            {
                Text = text,
                ButtonPressed = true,
                TooltipText = F(BmmText.LogLevelTooltipFormat, "Show or hide {0} log lines.", text.ToLowerInvariant())
            };
            ModdingScreenVanillaStyle.ApplyButton(toggle);
            toggle.CustomMinimumSize = new Vector2(Mathf.Max(toggle.CustomMinimumSize.X, 82), 34);
            toggle.AddThemeFontSizeOverride("font_size", layout.ButtonFontSize);
            toggle.Toggled += pressed =>
            {
                includedLevels = pressed
                    ? includedLevels | level
                    : includedLevels & ~level;
                RefreshLogText();
            };
            return toggle;
        }

        return dialogBox;
    }

    private static void OpenLogFolder(NModdingScreen screen, string logPath)
    {
        if (!LogFolderOpenRules.TryGetContainingDirectory(logPath, out string directory, out string? error))
        {
            string localizedError = ModdingScreenText.LocalizeKnownError(error);
            ShowInfoDialog(
                screen,
                T(BmmText.LogFolderNotOpenedTitle, "Log Folder Not Opened"),
                string.IsNullOrWhiteSpace(localizedError)
                    ? T(BmmText.LogFolderNotOpenedGeneric, "The log folder could not be opened.")
                    : localizedError);
            return;
        }

        if (!LogFolderOpenRules.TryOpenDirectory(directory, out string? openError))
        {
            ShowInfoDialog(
                screen,
                T(BmmText.LogFolderNotOpenedTitle, "Log Folder Not Opened"),
                F(BmmText.LogFolderOsErrorFormat, "The operating system could not open this folder:\n{0}\n\nError:\n{1}", directory, ModdingScreenText.LocalizeKnownError(openError)));
        }
    }

    private static string BuildBackupDisplayLabel(ProfileBackupEntry entry)
    {
        string reason = entry.Reason switch
        {
            ProfileBackupReason.Manual => T(BmmText.BackupReasonManual, "Manual backup"),
            ProfileBackupReason.Resume => T(BmmText.BackupReasonAuto, "Auto backup"),
            ProfileBackupReason.RunStart => T(BmmText.BackupReasonStartup, "Startup backup"),
            _ => T(BmmText.BackupReasonGeneric, "Backup")
        };

        int separator = entry.Label.LastIndexOf(" - ", StringComparison.Ordinal);
        if (separator < 0)
            return reason;

        return entry.Label[..separator] + " - " + reason;
    }


    public static void ShowTutorialDialog(NModdingScreen screen, string version, Action onDismissed)
    {
        TutorialDialogLayout layout = GetTutorialLayoutForScreen(screen);
        var popup = new AcceptDialog
        {
            Title = F(BmmText.TutorialTitleFormat, "Better Mod Menu v{0}", version),
            DialogText = string.Empty
        };
        ModdingScreenVanillaStyle.ApplyDialogWindow(popup);

        var scroll = new ScrollContainer
        {
            CustomMinimumSize = new Vector2(layout.ContentWidth, layout.ContentHeight),
            HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled,
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
        };
        var label = CreateReadableBodyLabel(TutorialContentBuilder.BuildBody(ModdingScreenText.Get), layout.BodyFontSize);
        label.CustomMinimumSize = new Vector2(layout.ContentWidth, 0);
        scroll.AddChild(label);
        popup.AddChild(CreateStyledDialogShell(scroll, layout.ContentWidth + 32, layout.ContentHeight + 24));
        popup.AddThemeFontSizeOverride("font_size", layout.BodyFontSize);
        ApplyReadableDialogButtons(popup, layout.ButtonFontSize);
        popup.Confirmed += onDismissed;
        popup.Canceled += onDismissed;
        screen.AddChild(popup);
        PopupTutorialDialog(screen, popup);
        screen.Resized += () =>
        {
            if (!GodotObject.IsInstanceValid(popup) || !popup.Visible)
                return;

            PopupTutorialDialog(screen, popup);
        };
    }

    public static void ShowRenameGroupDialog(NModdingScreen screen, string oldName, Action<string> onConfirmed)
    {
        ShowTextInputDialog(
            screen,
            T(BmmText.DialogRenameGroupTitle, "Rename Group"),
            oldName,
            T(BmmText.DialogRenameGroupHelp, "This changes the group label for every mod currently assigned to it."),
            onConfirmed);
    }

    public static void ShowRenameProfileDialog(NModdingScreen screen, string currentName, Action<string> onConfirmed)
    {
        ShowTextInputDialog(
            screen,
            T(BmmText.DialogRenameProfileTitle, "Rename Profile"),
            currentName,
            T(BmmText.DialogRenameProfileHelp, "This changes the profile name only. The enabled and disabled mods in the profile stay the same."),
            onConfirmed);
    }

    public static void ShowCloudBackupDialog(NModdingScreen screen, string currentDirectory, Action<string> onConfirmed)
    {
        ShowTextInputDialog(
            screen,
            T(BmmText.DialogCloudFolderTitle, "Cloud Backup Folder"),
            currentDirectory,
            T(BmmText.DialogCloudFolderHelp, "Enter a OneDrive, Dropbox, or other synced folder. Leave it blank to turn cloud mirroring off."),
            onConfirmed);
    }

    private static void ShowTextInputDialog(NModdingScreen screen, string title, string initialText, string helpText, Action<string> onConfirmed)
    {
        var popup = new AcceptDialog
        {
            Title = title,
            DialogText = string.Empty
        };
        ModdingScreenVanillaStyle.ApplyDialogWindow(popup);

        var body = new VBoxContainer();
        body.AddThemeConstantOverride("separation", 8);
        var helpLabel = CreateReadableBodyLabel(helpText, 20);
        helpLabel.CustomMinimumSize = new Vector2(440, 0);
        body.AddChild(helpLabel);

        var input = new LineEdit
        {
            Text = initialText,
            CustomMinimumSize = new Vector2(440, 0)
        };
        ModdingScreenVanillaStyle.ApplyLineEdit(input);
        body.AddChild(input);

        popup.AddChild(CreateStyledDialogShell(body, 480));
        popup.Confirmed += () => onConfirmed(input.Text);
        ApplyReadableDialogButtons(popup, 22);

        screen.AddChild(popup);
        popup.PopupCentered(new Vector2I(520, 180));
    }

    private static Label CreateReadableBodyLabel(string text, int fontSize)
    {
        var label = new Label
        {
            Text = text,
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            SizeFlagsVertical = Control.SizeFlags.ExpandFill
        };
        ModdingScreenVanillaStyle.ApplyLabel(label);
        label.VerticalAlignment = VerticalAlignment.Top;
        label.AddThemeFontSizeOverride("font_size", fontSize);
        return label;
    }

    private static PanelContainer CreateStyledDialogShell(Control content, float width, float height = 0)
    {
        var shell = new PanelContainer
        {
            CustomMinimumSize = new Vector2(width, height),
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            SizeFlagsVertical = Control.SizeFlags.ExpandFill
        };
        ModdingScreenVanillaStyle.ApplyDialogPanel(shell);

        var margins = new MarginContainer();
        margins.AddThemeConstantOverride("margin_left", 12);
        margins.AddThemeConstantOverride("margin_right", 12);
        margins.AddThemeConstantOverride("margin_top", 10);
        margins.AddThemeConstantOverride("margin_bottom", 10);
        margins.AddChild(content);
        shell.AddChild(margins);
        return shell;
    }

    private static void ApplyReadableDialogButtons(AcceptDialog popup, int fontSize)
    {
        ModdingScreenVanillaStyle.ApplyDialogWindow(popup);
        var okButton = popup.GetOkButton();
        ModdingScreenVanillaStyle.ApplyButton(okButton);
        okButton.AddThemeFontSizeOverride("font_size", fontSize);
        okButton.CustomMinimumSize = new Vector2(Mathf.Max(okButton.CustomMinimumSize.X, 72), 40);

        if (popup is ConfirmationDialog confirmation)
        {
            var cancelButton = confirmation.GetCancelButton();
            ModdingScreenVanillaStyle.ApplyButton(cancelButton);
            cancelButton.AddThemeFontSizeOverride("font_size", fontSize);
            cancelButton.CustomMinimumSize = new Vector2(Mathf.Max(cancelButton.CustomMinimumSize.X, 96), 40);
        }
    }

    private static TutorialDialogLayout GetTutorialLayoutForScreen(NModdingScreen screen)
    {
        var viewportSize = screen.GetViewportRect().Size;
        return ModdingScreenDialogRules.FitTutorialDialogToViewport(
            ModdingScreenDialogRules.GetPreferredTutorialDialogLayout(),
            (int)viewportSize.X,
            (int)viewportSize.Y);
    }

    private static void PopupTutorialDialog(NModdingScreen screen, AcceptDialog popup)
    {
        TutorialDialogLayout layout = GetTutorialLayoutForScreen(screen);
        popup.PopupCentered(new Vector2I(layout.PopupWidth, layout.PopupHeight));
    }
}
