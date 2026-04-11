using System.Collections.Generic;
using Godot;

namespace BetterModMenu.Patches;

internal sealed class ModdingScreenSession
{
    public int AutoSaveSuppressionDepth { get; set; }
    public Control? ChromeRoot { get; set; }
    public List<Node> GeneratedGroupNodes { get; } = new();
    public GroupBarControls? GroupBarControls { get; set; }
    public bool LayoutSignalsConnected { get; set; }
    public TopBarControls? TopBarControls { get; set; }
    public int TickboxSuppressionDepth { get; set; }
}
