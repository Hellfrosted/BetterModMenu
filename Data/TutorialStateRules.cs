namespace BetterModMenu.Data;

public sealed class TutorialState
{
    public string LastSeenVersion { get; set; } = string.Empty;
}

internal static class TutorialStateRules
{
    public static bool ShouldShowTutorial(TutorialState? state, string currentVersion)
    {
        if (string.IsNullOrWhiteSpace(currentVersion))
            return false;

        return state == null ||
            !string.Equals(state.LastSeenVersion, currentVersion, System.StringComparison.Ordinal);
    }

    public static void MarkSeen(TutorialState state, string currentVersion)
    {
        if (!string.IsNullOrWhiteSpace(currentVersion))
            state.LastSeenVersion = currentVersion;
    }
}
