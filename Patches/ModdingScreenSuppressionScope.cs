using MegaCrit.Sts2.Core.Nodes.Screens.ModdingScreen;

namespace BetterModMenu.Patches;

internal sealed class ModdingScreenSuppressionScope : System.IDisposable
{
    private readonly ModdingScreenSession _session;
    private readonly bool _suppressAutoSave;
    private readonly bool _suppressTickboxes;

    public ModdingScreenSuppressionScope(NModdingScreen screen, bool suppressAutoSave, bool suppressTickboxes)
    {
        _session = ModdingScreenContext.GetSession(screen);
        _suppressAutoSave = suppressAutoSave;
        _suppressTickboxes = suppressTickboxes;

        if (_suppressAutoSave)
            _session.AutoSaveSuppressionDepth++;
        if (_suppressTickboxes)
            _session.TickboxSuppressionDepth++;
    }

    public void Dispose()
    {
        if (_suppressTickboxes && _session.TickboxSuppressionDepth > 0)
            _session.TickboxSuppressionDepth--;
        if (_suppressAutoSave && _session.AutoSaveSuppressionDepth > 0)
            _session.AutoSaveSuppressionDepth--;
    }
}
