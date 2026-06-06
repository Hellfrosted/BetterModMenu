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
    public bool ModsScrollbarPersistenceSignalsConnected { get; set; }
    public Vector2? OriginalModsScrollPosition { get; set; }
    public Vector2? OriginalModsScrollSize { get; set; }
    public TopBarControls? TopBarControls { get; set; }
    public int TickboxSuppressionDepth { get; set; }

    public void Reset()
    {
        AutoSaveSuppressionDepth = 0;
        ChromeRoot = null;
        GeneratedGroupNodes.Clear();
        GroupBarControls = null;
        LayoutSignalsConnected = false;
        ModsScrollbarPersistenceSignalsConnected = false;
        OriginalModsScrollPosition = null;
        OriginalModsScrollSize = null;
        TopBarControls = null;
        TickboxSuppressionDepth = 0;
    }
}
