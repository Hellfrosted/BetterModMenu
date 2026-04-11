using BetterModMenu.Data;
using BetterModMenu.Patches;
using System.IO;

var failures = new List<string>();

Run("CanAdd trims and rejects duplicates", () =>
{
    var existingGroups = new List<string> { "Bosses" };
    bool isValid = ModdingGroupRules.CanAdd(existingGroups, "  New Group  ", out string trimmedName);
    Assert.True(isValid, "expected a valid group name");
    Assert.Equal("New Group", trimmedName, "expected the name to be trimmed");
});

Run("CanAdd rejects reserved group name", () =>
{
    bool isValid = ModdingGroupRules.CanAdd(Array.Empty<string>(), ModdingScreenConstants.UnassignedGroup, out _);
    Assert.False(isValid, "reserved name should not be accepted");
});

Run("ValidateRename allows no-op rename", () =>
{
    var result = ModdingGroupRules.ValidateRename(new[] { "Bosses" }, "Bosses", "Bosses", out string trimmedName);
    Assert.Equal(GroupNameValidationResult.Unchanged, result, "same-name rename should be a no-op");
    Assert.Equal("Bosses", trimmedName, "expected unchanged name");
});

Run("ValidateRename rejects duplicate target", () =>
{
    var result = ModdingGroupRules.ValidateRename(new[] { "Bosses", "Elites" }, "Bosses", "Elites", out _);
    Assert.Equal(GroupNameValidationResult.Duplicate, result, "duplicate rename should be rejected");
});

Run("FindManifestPath only returns exact manifest names", () =>
{
    string tempDirectory = Path.Combine(Path.GetTempPath(), "BetterModMenuTests_" + Guid.NewGuid().ToString("N"));
    Directory.CreateDirectory(tempDirectory);
    try
    {
        File.WriteAllText(Path.Combine(tempDirectory, "RouteSuggestConfig.json"), "{ }");
        File.WriteAllText(Path.Combine(tempDirectory, "BetterModMenu.json"), "{ }");

        string? manifestPath = ManifestScanner.FindManifestPath(tempDirectory, "BetterModMenu", new[] { ".json" });
        Assert.Equal(Path.Combine(tempDirectory, "BetterModMenu.json"), manifestPath, "expected exact manifest match");
    }
    finally
    {
        Directory.Delete(tempDirectory, true);
    }
});

Run("TryReadAffectsGameplay ignores mismatched ids", () =>
{
    string tempPath = Path.Combine(Path.GetTempPath(), "BetterModMenuTests_" + Guid.NewGuid().ToString("N") + ".json");
    try
    {
        File.WriteAllText(tempPath, """
        {
          "id": "AnotherMod",
          "affects_gameplay": true
        }
        """);

        bool found = ManifestScanner.TryReadAffectsGameplay(tempPath, "BetterModMenu", out bool affectsGameplay);
        Assert.False(found, "mismatched ids should be ignored");
        Assert.False(affectsGameplay, "ignored manifests should not report gameplay impact");
    }
    finally
    {
        if (File.Exists(tempPath))
            File.Delete(tempPath);
    }
});

Run("NormalizePersistedState removes stale mod and group entries", () =>
{
    var customGroups = new List<string> { "Bosses", "Elites" };
    var modGroups = new Dictionary<string, string>
    {
        ["keep-mod"] = "Bosses",
        ["missing-mod"] = "Bosses",
        ["wrong-group"] = "Missing"
    };
    var collapsedGroups = new HashSet<string> { "Bosses", "Missing", "Unassigned" };

    bool changed = ProfileStateRules.NormalizeGroups(customGroups, modGroups, collapsedGroups, new[] { "keep-mod", "other-mod" }, "Unassigned");

    Assert.True(changed, "expected stale state to be normalized");
    Assert.Equal(1, modGroups.Count, "expected only valid mod-group assignment to remain");
    Assert.Equal("Bosses", modGroups["keep-mod"], "expected valid assignment to remain");
    Assert.True(collapsedGroups.Contains("Bosses"), "expected valid collapsed group to remain");
    Assert.False(collapsedGroups.Contains("Missing"), "expected invalid collapsed group to be removed");
    Assert.False(collapsedGroups.Contains("Unassigned"), "expected unassigned collapsed state to be removed");
});

Run("NormalizePersistedState reports no-op when state is already valid", () =>
{
    var customGroups = new List<string> { "Bosses" };
    var modGroups = new Dictionary<string, string> { ["keep-mod"] = "Bosses" };
    var collapsedGroups = new HashSet<string> { "Bosses" };

    bool changed = ProfileStateRules.NormalizeGroups(customGroups, modGroups, collapsedGroups, new[] { "keep-mod" }, "Unassigned");

    Assert.False(changed, "expected already-valid state to remain unchanged");
});

Run("BuildVisibleGroupOrder omits empty unassigned group", () =>
{
    var groups = new Dictionary<string, int>
    {
        [ModdingScreenConstants.UnassignedGroup] = 0,
        ["Bosses"] = 1,
        ["Elites"] = 0
    };
    var names = ProfileStateRules.BuildVisibleGroupOrder(groups, new List<string> { "Bosses", "Elites" }, ModdingScreenConstants.UnassignedGroup);

    Assert.Equal(2, names.Count, "expected only custom groups to be visible");
    Assert.Equal("Bosses", names[0], "expected custom groups to preserve order");
    Assert.Equal("Elites", names[1], "expected custom groups to preserve order");
});

if (failures.Count > 0)
{
    Console.Error.WriteLine("Test failures:");
    foreach (string failure in failures)
        Console.Error.WriteLine("- " + failure);
    return 1;
}

Console.WriteLine("All tests passed.");
return 0;

void Run(string name, Action test)
{
    try
    {
        test();
        Console.WriteLine("[PASS] " + name);
    }
    catch (Exception ex)
    {
        failures.Add($"{name}: {ex.Message}");
    }
}

static class Assert
{
    public static void Equal<T>(T expected, T actual)
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
            throw new InvalidOperationException($"Expected '{expected}', got '{actual}'.");
    }

    public static void Equal<T>(T expected, T actual, string message)
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
            throw new InvalidOperationException($"{message}. Expected '{expected}', got '{actual}'.");
    }

    public static void False(bool condition, string message)
    {
        if (condition)
            throw new InvalidOperationException(message);
    }

    public static void True(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException(message);
    }
}
