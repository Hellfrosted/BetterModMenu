using BetterModMenu.Data;
using Godot;
using MegaCrit.Sts2.Core.Nodes.Screens.ModdingScreen;

namespace BetterModMenu.Patches;

internal partial class WorkshopTagRefreshNode : Node
{
    public NModdingScreen? Screen { get; init; }

    public override void _Process(double delta)
    {
        if (!ModdingScreenContext.IsCurrentScreen(Screen) || !ProfileManager.PublishWorkshopTags())
            return;

        NModdingScreenPatch.RefreshModNameStyles(Screen!);
        ModdingScreenContext.GetSession(Screen!).WorkshopTagsChanged?.Invoke();
    }
}
