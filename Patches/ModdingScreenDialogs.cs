using System;
using BetterModMenu.Data;
using Godot;
using MegaCrit.Sts2.Core.Nodes.Screens.ModdingScreen;

namespace BetterModMenu.Patches;

internal static class ModdingScreenDialogs
{
    private const int MaxVisibleBackupChoices = 12;
    private const int LogPopupWidth = 1080;
    private const int LogPopupHeight = 680;
    private const int LogContentWidth = 1020;
    private const int LogContentHeight = 600;
    private const int LogBodyFontSize = 22;
    private const int LogButtonFontSize = 22;

    public static void ShowInfoDialog(NModdingScreen screen, string title, string message)
    {
        TutorialDialogLayout layout = ModdingScreenDialogRules.GetPreferredTutorialDialogLayout();
        var popup = new AcceptDialog
        {
            Title = title,
            DialogText = string.Empty
        };

        var label = CreateReadableBodyLabel(message, layout.BodyFontSize);
        label.CustomMinimumSize = new Vector2(Mathf.Min(720, layout.ContentWidth), 0);
        popup.AddChild(label);
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

        var label = CreateReadableBodyLabel(message, layout.BodyFontSize);
        label.CustomMinimumSize = new Vector2(Mathf.Min(760, layout.ContentWidth), 0);
        popup.AddChild(label);
        ApplyReadableDialogButtons(popup, layout.ButtonFontSize);
        var cancelButton = popup.GetCancelButton();
        cancelButton.AddThemeFontSizeOverride("font_size", layout.ButtonFontSize);
        cancelButton.CustomMinimumSize = new Vector2(Mathf.Max(cancelButton.CustomMinimumSize.X, 96), 40);
        popup.Confirmed += onConfirmed;
        screen.AddChild(popup);
        popup.PopupCentered(new Vector2I(900, 360));
    }

    public static void ShowBackupSelectionDialog(NModdingScreen screen, IReadOnlyList<ProfileBackupEntry> backups, Action<string> onConfirmed)
    {
        TutorialDialogLayout layout = ModdingScreenDialogRules.GetPreferredTutorialDialogLayout();
        var visibleBackups = backups.Take(MaxVisibleBackupChoices).ToList();
        var popup = new ConfirmationDialog
        {
            Title = "Load Backup",
            DialogText = string.Empty
        };

        var body = new VBoxContainer();
        var helpLabel = CreateReadableBodyLabel(
            backups.Count > visibleBackups.Count
                ? "Choose a backup to load. Showing the newest " + visibleBackups.Count + " of " + backups.Count + " backups."
                : "Choose a backup to load. Installed mod files are not changed.",
            20);
        helpLabel.CustomMinimumSize = new Vector2(520, 0);
        body.AddChild(helpLabel);

        var backupDropdown = new OptionButton
        {
            CustomMinimumSize = new Vector2(520, ModdingScreenConstants.ToolbarControlHeight),
            TooltipText = "Backups are ordered from newest to oldest."
        };
        ModdingScreenVanillaStyle.ApplyOptionButton(backupDropdown);
        for (int i = 0; i < visibleBackups.Count; i++)
            backupDropdown.AddItem(visibleBackups[i].Label, i);
        backupDropdown.Select(0);
        body.AddChild(backupDropdown);

        popup.AddChild(body);
        ApplyReadableDialogButtons(popup, layout.ButtonFontSize);
        var cancelButton = popup.GetCancelButton();
        cancelButton.AddThemeFontSizeOverride("font_size", layout.ButtonFontSize);
        cancelButton.CustomMinimumSize = new Vector2(Mathf.Max(cancelButton.CustomMinimumSize.X, 96), 40);
        popup.Confirmed += () =>
        {
            int selectedIndex = backupDropdown.Selected;
            if (selectedIndex >= 0 && selectedIndex < visibleBackups.Count)
                onConfirmed(visibleBackups[selectedIndex].Path);
        };
        screen.AddChild(popup);
        popup.PopupCentered(new Vector2I(640, 220));
    }

    public static void ShowLogDialog(NModdingScreen screen, string title, string content)
    {
        var popup = new AcceptDialog
        {
            Title = title,
            DialogText = string.Empty
        };

        var scroll = new ScrollContainer
        {
            HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled,
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            SizeFlagsVertical = Control.SizeFlags.ExpandFill
        };
        var panel = new PanelContainer
        {
            CustomMinimumSize = new Vector2(LogContentWidth, LogContentHeight)
        };
        ModdingScreenVanillaStyle.ApplyLogPanel(panel);
        var contentBox = new VBoxContainer
        {
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            SizeFlagsVertical = Control.SizeFlags.ExpandFill
        };
        var copyButton = new Button
        {
            Text = "Copy All",
            TooltipText = "Copy the displayed log text to the clipboard"
        };
        ModdingScreenVanillaStyle.ApplyButton(copyButton);
        copyButton.Pressed += () => DisplayServer.ClipboardSet(content);
        contentBox.AddChild(copyButton);

        var label = new RichTextLabel
        {
            BbcodeEnabled = true,
            Text = LogHighlightService.BuildHighlightedBbCode(content),
            SelectionEnabled = true,
            ContextMenuEnabled = true,
            ScrollActive = false,
            FitContent = true,
            CustomMinimumSize = new Vector2(0, 0),
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            SizeFlagsVertical = Control.SizeFlags.ExpandFill
        };
        label.AddThemeColorOverride("default_color", new Color(0.92f, 0.86f, 0.74f, 1f));
        label.AddThemeFontSizeOverride("font_size", LogBodyFontSize);

        contentBox.AddChild(label);
        scroll.AddChild(contentBox);
        panel.AddChild(scroll);
        popup.AddChild(panel);
        ApplyReadableDialogButtons(popup, LogButtonFontSize);
        screen.AddChild(popup);
        popup.PopupCentered(new Vector2I(LogPopupWidth, LogPopupHeight));
    }


    public static void ShowTutorialDialog(NModdingScreen screen, string version, Action onDismissed)
    {
        TutorialDialogLayout layout = GetTutorialLayoutForScreen(screen);
        var popup = new AcceptDialog
        {
            Title = "Better Mod Menu v" + version,
            DialogText = string.Empty
        };

        var scroll = new ScrollContainer
        {
            CustomMinimumSize = new Vector2(layout.ContentWidth, layout.ContentHeight),
            HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled,
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
        };
        var label = CreateReadableBodyLabel(TutorialContentBuilder.BuildBody(), layout.BodyFontSize);
        label.CustomMinimumSize = new Vector2(layout.ContentWidth, 0);
        scroll.AddChild(label);
        popup.AddChild(scroll);
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
        ShowTextInputDialog(screen, "Rename Group", oldName, "This changes the group label for every mod currently assigned to it.", onConfirmed);
    }

    public static void ShowRenameProfileDialog(NModdingScreen screen, string currentName, Action<string> onConfirmed)
    {
        ShowTextInputDialog(screen, "Rename Profile", currentName, "This changes the profile name only. The enabled and disabled mods in the profile stay the same.", onConfirmed);
    }

    public static void ShowCloudBackupDialog(NModdingScreen screen, string currentDirectory, Action<string> onConfirmed)
    {
        ShowTextInputDialog(screen, "Cloud Backup Folder", currentDirectory, "Enter a OneDrive, Dropbox, or other synced folder. Leave it blank to turn cloud mirroring off.", onConfirmed);
    }

    private static void ShowTextInputDialog(NModdingScreen screen, string title, string initialText, string helpText, Action<string> onConfirmed)
    {
        var popup = new AcceptDialog
        {
            Title = title,
            DialogText = string.Empty
        };

        var body = new VBoxContainer();
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

        popup.AddChild(body);
        popup.Confirmed += () => onConfirmed(input.Text);

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

    private static void ApplyReadableDialogButtons(AcceptDialog popup, int fontSize)
    {
        var okButton = popup.GetOkButton();
        okButton.AddThemeFontSizeOverride("font_size", fontSize);
        okButton.CustomMinimumSize = new Vector2(Mathf.Max(okButton.CustomMinimumSize.X, 72), 40);
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
