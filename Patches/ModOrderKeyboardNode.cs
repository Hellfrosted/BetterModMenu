using Godot;
using MegaCrit.Sts2.Core.Nodes.Screens.ModdingScreen;

namespace BetterModMenu.Patches;

internal partial class ModOrderKeyboardNode : Node
{
    public NModdingScreen? Screen { get; init; }

    public override void _Input(InputEvent input)
    {
        if (!ModdingScreenContext.IsCurrentScreen(Screen) ||
            input is not InputEventKey { Pressed: true, Echo: false, AltPressed: false, CtrlPressed: false, ShiftPressed: false, MetaPressed: false } key ||
            key.Keycode is not (Key.Up or Key.Down))
            return;

        if (Screen!.GetChildren().OfType<Window>().Any(window => window.Visible))
            return;
        var focus = Screen.GetViewport().GuiGetFocusOwner();
        if (focus is not NModMenuRow row || !row.IsVisibleInTree())
            return;
        string modId = ModdingScreenContext.GetSession(Screen).SelectedModId;
        if (string.IsNullOrEmpty(modId) || row.Mod?.manifest?.id != modId)
            return;

        Screen.GetViewport().SetInputAsHandled();
        NModdingScreenPatch.MoveModOrder(modId, key.Keycode == Key.Up ? -1 : 1);
    }
}
