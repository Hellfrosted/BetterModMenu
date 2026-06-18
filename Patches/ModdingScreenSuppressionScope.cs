using MegaCrit.Sts2.Core.Nodes.Screens.ModdingScreen;

namespace BetterModMenu.Patches;

internal sealed class ModdingScreenSuppressionScope : System.IDisposable
{
    private readonly ModdingScreenSession _session;

    public ModdingScreenSuppressionScope(NModdingScreen screen)
    {
        _session = ModdingScreenContext.GetSession(screen);
        _session.AutoSaveSuppressionDepth++;
        _session.TickboxSuppressionDepth++;
    }

    public void Dispose()
    {
        if (_session.TickboxSuppressionDepth > 0)
            _session.TickboxSuppressionDepth--;
        if (_session.AutoSaveSuppressionDepth > 0)
            _session.AutoSaveSuppressionDepth--;
    }
}
