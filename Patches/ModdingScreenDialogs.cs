using System;
using Godot;
using MegaCrit.Sts2.Core.Nodes.Screens.ModdingScreen;

namespace BetterModMenu.Patches;

internal static class ModdingScreenDialogs
{
    public static void ShowRenameGroupDialog(NModdingScreen screen, string oldName, Action<string> onConfirmed)
    {
        ShowTextInputDialog(screen, "Rename Group", oldName, onConfirmed);
    }

    public static void ShowRenameProfileDialog(NModdingScreen screen, string currentName, Action<string> onConfirmed)
    {
        ShowTextInputDialog(screen, "Rename Profile", currentName, onConfirmed);
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

        popup.AddChild(input);
        popup.Confirmed += () => onConfirmed(input.Text);

        screen.AddChild(popup);
        popup.PopupCentered(new Vector2I(300, 100));
    }
}
