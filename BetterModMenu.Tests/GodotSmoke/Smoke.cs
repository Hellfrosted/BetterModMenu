using Godot;
using BetterModMenu.Data;
using MegaCrit.Sts2.Core.Nodes.Screens.ModdingScreen;
using System.Reflection;

public partial class TestScreen : NModdingScreen { public override void _Ready() { } }

public partial class Smoke : Node
{
    private TestScreen _screen = null!;
    private object _session = null!;
    private MethodInfo _show = null!;
    private ConfirmationDialog? _popup;
    private string _first = "";
    private string _other = "";
    private int _stage;
    private int _frames;

    public override void _Ready()
    {
        try
        {
            TranslationServer.SetLocale("en");
            ProfileManager.ResetState();
            ProfileManager.ModNameStyles.Enabled = false;
            _screen = new TestScreen();
            AddChild(_screen);
            var assembly = typeof(ProfileManager).Assembly;
            var context = assembly.GetType("BetterModMenu.Patches.ModdingScreenContext", true)!;
            _session = context.GetMethod("GetSession", BindingFlags.Static | BindingFlags.Public)!.Invoke(null, [_screen])!;
            _session.GetType().GetProperty("SelectedModId")!.SetValue(_session, "smoke-mod");
            _show = assembly.GetType("BetterModMenu.Patches.ModNameStyleEditorDialog", true)!.GetMethod("Show", BindingFlags.Static | BindingFlags.Public)!;
            _popup = OpenEditor();
            var dropdown = Descendants(_popup).OfType<OptionButton>().Single();
            _first = dropdown.GetItemText(dropdown.Selected);
            _other = Enumerable.Range(0, dropdown.ItemCount).Select(dropdown.GetItemText).First(text => text != _first);
            var disable = Descendants(_popup).OfType<Button>().Single(button => button.Text == "Disable Tag");
            disable.EmitSignal(Button.SignalName.Pressed);
            ProfileManager.ModWorkshopTagsCache["smoke-mod"] = [_other];
            NotifyTagsChanged();
            if (dropdown.GetItemText(dropdown.Selected) != _first) throw new Exception("Tag refresh replaced a user choice");
            _popup.Hide();
        }
        catch (Exception ex) { Fail(ex); }
    }

    public override void _Process(double delta)
    {
        if (++_frames < 2) return;
        try
        {
            if (_stage++ == 0)
            {
                if (GodotObject.IsInstanceValid(_popup)) throw new Exception("Closed editor was not freed");
                ProfileManager.ModWorkshopTagsCache.Clear();
                _popup = OpenEditor();
                var dropdown = Descendants(_popup).OfType<OptionButton>().Single();
                if (dropdown.GetItemText(dropdown.Selected) != _first) throw new Exception("Unexpected initial tag");
                ProfileManager.ModWorkshopTagsCache["smoke-mod"] = [_other];
                NotifyTagsChanged();
                if (dropdown.GetItemText(dropdown.Selected) != _other) throw new Exception("Initial tag did not update after tag loading");
                _popup.Hide();
            }
            else
            {
                if (GodotObject.IsInstanceValid(_popup)) throw new Exception("Reopened editor was not freed");
                GD.Print("REAL_EDITOR_SMOKE_OK");
                GetTree().Quit();
            }
        }
        catch (Exception ex) { Fail(ex); }
    }

    private ConfirmationDialog OpenEditor()
    {
        _show.Invoke(null, [_screen, (Action)(() => { })]);
        var popup = _screen.GetChildren().OfType<ConfirmationDialog>().Single();
        if (!popup.Visible) throw new Exception("Style editor did not open");
        return popup;
    }
    private void NotifyTagsChanged() => ((Action?)_session.GetType().GetProperty("WorkshopTagsChanged")!.GetValue(_session))?.Invoke();
    private static IEnumerable<Node> Descendants(Node node)
    {
        foreach (Node child in node.GetChildren())
        {
            yield return child;
            foreach (Node descendant in Descendants(child)) yield return descendant;
        }
    }
    private void Fail(Exception ex) { GD.PushError(ex.ToString()); GetTree().Quit(1); }
}
