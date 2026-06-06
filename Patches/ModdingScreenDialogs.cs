using System;
using BetterModMenu.Data;
using Godot;
using MegaCrit.Sts2.Core.Nodes.Screens.ModdingScreen;

namespace BetterModMenu.Patches;

internal static class ModdingScreenDialogs
{
    public static void ShowInfoDialog(NModdingScreen screen, string title, string message)
    {
        TutorialDialogLayout layout = ModdingScreenDialogRules.GetTutorialDialogLayout();
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
        popup.PopupCentered(new Vector2I(760, 220));
    }

    public static void ShowLogDialog(NModdingScreen screen, string title, string content)
    {
        LogDialogLayout layout = ModdingScreenDialogRules.GetLogDialogLayout();
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
            CustomMinimumSize = new Vector2(layout.ContentWidth, layout.ContentHeight)
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
        label.AddThemeFontSizeOverride("font_size", layout.BodyFontSize);

        contentBox.AddChild(label);
        scroll.AddChild(contentBox);
        panel.AddChild(scroll);
        popup.AddChild(panel);
        ApplyReadableDialogButtons(popup, layout.ButtonFontSize);
        screen.AddChild(popup);
        popup.PopupCentered(new Vector2I(layout.PopupWidth, layout.PopupHeight));
    }


    public static void ShowTutorialDialog(NModdingScreen screen, string version, Action onDismissed)
    {
        TutorialDialogLayout layout = GetTutorialLayoutForScreen(screen);
        var popup = new AcceptDialog
        {
            Title = "Better Mod Menu v" + version,
            DialogText = TutorialContentBuilder.BuildBody()
        };

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
        ShowTextInputDialog(screen, "Rename Group", oldName, onConfirmed);
    }

    public static void ShowRenameProfileDialog(NModdingScreen screen, string currentName, Action<string> onConfirmed)
    {
        ShowTextInputDialog(screen, "Rename Profile", currentName, onConfirmed);
    }

    public static void ShowCloudBackupDialog(NModdingScreen screen, string currentDirectory, Action<string> onConfirmed)
    {
        ShowTextInputDialog(screen, "Cloud Backup Folder", currentDirectory, onConfirmed);
    }

    private static void ShowTextInputDialog(NModdingScreen screen, string title, string initialText, Action<string> onConfirmed)
    {
        var popup = new AcceptDialog
        {
            Title = title,
            DialogText = string.Empty
        };

        var input = new LineEdit
        {
            Text = initialText,
            CustomMinimumSize = new Vector2(250, 0)
        };
        ModdingScreenVanillaStyle.ApplyLineEdit(input);

        popup.AddChild(input);
        popup.Confirmed += () => onConfirmed(input.Text);

        screen.AddChild(popup);
        popup.PopupCentered(new Vector2I(300, 100));
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
            ModdingScreenDialogRules.GetTutorialDialogLayout(),
            (int)viewportSize.X,
            (int)viewportSize.Y);
    }

    private static void PopupTutorialDialog(NModdingScreen screen, AcceptDialog popup)
    {
        TutorialDialogLayout layout = GetTutorialLayoutForScreen(screen);
        popup.PopupCentered(new Vector2I(layout.PopupWidth, layout.PopupHeight));
    }
}
