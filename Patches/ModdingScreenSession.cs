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
    public string SearchQuery { get; set; } = string.Empty;
    public Dictionary<string, Data.ModSearchResult> SearchResults { get; } = new(StringComparer.OrdinalIgnoreCase);
    public string SelectedModId { get; set; } = string.Empty;
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
        SearchQuery = string.Empty;
        SearchResults.Clear();
        SelectedModId = string.Empty;
        TopBarControls = null;
        TickboxSuppressionDepth = 0;
    }
}
