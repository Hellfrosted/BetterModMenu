using BetterModMenu.Data;
using BetterModMenu.Patches;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;

namespace BetterModMenu.Tests;

[TestClass]
public class LogicTests
{
    [TestMethod]
    public void CanAdd_TrimsAndAcceptsValidNames()
    {
        var existingGroups = new List<string> { "Bosses" };

        bool isValid = ModdingGroupRules.CanAdd(existingGroups, "  New Group  ", out string trimmedName);

        Assert.IsTrue(isValid);
        Assert.AreEqual("New Group", trimmedName);
    }

    [TestMethod]
    public void CanAdd_RejectsReservedGroupName()
    {
        bool isValid = ModdingGroupRules.CanAdd(Array.Empty<string>(), ModdingScreenConstants.UnassignedGroup, out _);

        Assert.IsFalse(isValid);
    }

    [TestMethod]
    public void ValidateRename_AllowsNoOpRename()
    {
        var result = ModdingGroupRules.ValidateRename(new[] { "Bosses" }, "Bosses", "Bosses", out string trimmedName);

        Assert.AreEqual(GroupNameValidationResult.Unchanged, result);
        Assert.AreEqual("Bosses", trimmedName);
    }

    [TestMethod]
    public void ValidateRename_RejectsDuplicateTarget()
    {
        var result = ModdingGroupRules.ValidateRename(new[] { "Bosses", "Elites" }, "Bosses", "Elites", out _);

        Assert.AreEqual(GroupNameValidationResult.Duplicate, result);
    }

    [TestMethod]
    public void FindManifestPath_ReturnsOnlyExactManifestNames()
    {
        string tempDirectory = CreateTempDirectory();
        try
        {
            File.WriteAllText(Path.Combine(tempDirectory, "RouteSuggestConfig.json"), "{ }");
            File.WriteAllText(Path.Combine(tempDirectory, "BetterModMenu.json"), "{ }");

            string? manifestPath = ManifestScanner.FindManifestPath(tempDirectory, "BetterModMenu", new[] { ".json" });

            Assert.AreEqual(Path.Combine(tempDirectory, "BetterModMenu.json"), manifestPath);
        }
        finally
        {
            Directory.Delete(tempDirectory, true);
        }
    }

    [TestMethod]
    public void FindManifestPath_RejectsUnsafeManifestIds()
    {
        string tempDirectory = CreateTempDirectory();
        try
        {
            File.WriteAllText(Path.Combine(tempDirectory, "BetterModMenu.json"), "{ }");

            string? traversed = ManifestScanner.FindManifestPath(tempDirectory, @"..\BetterModMenu", new[] { ".json" });
            string rootedId = Path.Combine(tempDirectory, "BetterModMenu");
            string? rooted = ManifestScanner.FindManifestPath(tempDirectory, rootedId, new[] { ".json" });

            Assert.IsNull(traversed);
            Assert.IsNull(rooted);
        }
        finally
        {
            Directory.Delete(tempDirectory, true);
        }
    }

    [TestMethod]
    public void TryReadAffectsGameplay_IgnoresMismatchedIds()
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

            Assert.IsFalse(found);
            Assert.IsFalse(affectsGameplay);
        }
        finally
        {
            if (File.Exists(tempPath))
                File.Delete(tempPath);
        }
    }

    [TestMethod]
    public void NormalizeGroups_RemovesStaleEntries()
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

        Assert.IsTrue(changed);
        Assert.AreEqual(1, modGroups.Count);
        Assert.AreEqual("Bosses", modGroups["keep-mod"]);
        Assert.IsTrue(collapsedGroups.Contains("Bosses"));
        Assert.IsFalse(collapsedGroups.Contains("Missing"));
        Assert.IsFalse(collapsedGroups.Contains("Unassigned"));
    }

    [TestMethod]
    public void NormalizeGroups_ReportsNoOpWhenAlreadyValid()
    {
        var customGroups = new List<string> { "Bosses" };
        var modGroups = new Dictionary<string, string> { ["keep-mod"] = "Bosses" };
        var collapsedGroups = new HashSet<string> { "Bosses" };

        bool changed = ProfileStateRules.NormalizeGroups(customGroups, modGroups, collapsedGroups, new[] { "keep-mod" }, "Unassigned");

        Assert.IsFalse(changed);
    }

    [TestMethod]
    public void BuildVisibleGroupOrder_OmitsEmptyUnassignedGroup()
    {
        var groups = new Dictionary<string, int>
        {
            [ModdingScreenConstants.UnassignedGroup] = 0,
            ["Bosses"] = 1,
            ["Elites"] = 0
        };

        var names = ProfileStateRules.BuildVisibleGroupOrder(groups, new List<string> { "Bosses", "Elites" }, ModdingScreenConstants.UnassignedGroup);

        CollectionAssert.AreEqual(new[] { "Bosses", "Elites" }, names);
    }

    private static string CreateTempDirectory()
    {
        string tempDirectory = Path.Combine(Path.GetTempPath(), "BetterModMenuTests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDirectory);
        return tempDirectory;
    }
}
