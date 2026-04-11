using System.Collections.Generic;
using Godot;

namespace BetterModMenu.Patches;

internal sealed class ModdingScreenSession
{
    public int AutoSaveSuppressionDepth { get; set; }
    public List<Node> GeneratedGroupNodes { get; } = new();
    public HBoxContainer? GroupBar { get; set; }
    public OptionButton? ProfileDropdown { get; set; }
    public HBoxContainer? TopBar { get; set; }
    public int TickboxSuppressionDepth { get; set; }
}
