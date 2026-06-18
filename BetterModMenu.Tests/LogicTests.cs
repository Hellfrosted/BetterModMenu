using BetterModMenu.Data;
using BetterModMenu.Patches;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;
using System.Text;

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
    public void CanRename_AllowsNoOpRename()
    {
        bool canRename = ModdingGroupRules.CanRename(new[] { "Bosses" }, "Bosses", "Bosses", out string trimmedName, out bool unchanged);

        Assert.IsTrue(canRename);
        Assert.IsTrue(unchanged);
        Assert.AreEqual("Bosses", trimmedName);
    }

    [TestMethod]
    public void CanRename_RejectsDuplicateTarget()
    {
        bool canRename = ModdingGroupRules.CanRename(new[] { "Bosses", "Elites" }, "Bosses", "Elites", out _, out bool unchanged);

        Assert.IsFalse(canRename);
        Assert.IsFalse(unchanged);
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
    public void FindManifestPath_ReturnsLegacyModManifestWhenExactManifestIsMissing()
    {
        string tempDirectory = CreateTempDirectory();
        try
        {
            string legacyPath = Path.Combine(tempDirectory, "mod_manifest.json");
            File.WriteAllText(legacyPath, """
            {
              "id": "MultiHitDamage"
            }
            """);

            string? manifestPath = ManifestScanner.FindManifestPath(tempDirectory, "MultiHitDamage", new[] { ".json" });

            Assert.AreEqual(legacyPath, manifestPath);
        }
        finally
        {
            Directory.Delete(tempDirectory, true);
        }
    }

    [TestMethod]
    public void FindManifestPath_ReturnsTopLevelManifestWithMatchingId()
    {
        string tempDirectory = CreateTempDirectory();
        try
        {
            string manifestPath = Path.Combine(tempDirectory, "p9.json");
            File.WriteAllText(manifestPath, """
            {
              "id": "P9"
            }
            """);
            File.WriteAllText(Path.Combine(tempDirectory, "config.json"), "{ }");

            string? foundPath = ManifestScanner.FindManifestPath(tempDirectory, "P9", new[] { ".json" });

            Assert.AreEqual(manifestPath, foundPath);
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
    public void TryGetDirectoryFromPath_ReturnsDirectoryForFolderOrFilePath()
    {
        string tempDirectory = CreateTempDirectory();
        try
        {
            string manifestPath = Path.Combine(tempDirectory, "BetterModMenu.json");
            File.WriteAllText(manifestPath, "{ }");

            Assert.IsTrue(ModInstallPathResolver.TryGetDirectoryFromPath(tempDirectory, out string directoryFromFolder));
            Assert.AreEqual(Path.GetFullPath(tempDirectory), directoryFromFolder);

            Assert.IsTrue(ModInstallPathResolver.TryGetDirectoryFromPath(manifestPath, out string directoryFromFile));
            Assert.AreEqual(Path.GetFullPath(tempDirectory), directoryFromFile);
        }
        finally
        {
            Directory.Delete(tempDirectory, true);
        }
    }

    [TestMethod]
    public void TryGetDirectoryFromPath_ReturnsFalseForMissingWorkshopPlaceholderPath()
    {
        string tempDirectory = CreateTempDirectory();
        try
        {
            string missingPath = Path.Combine(tempDirectory, "workshop-content", "BetterModMenu.json");

            Assert.IsFalse(ModInstallPathResolver.TryGetDirectoryFromPath(missingPath, out string directory));
            Assert.AreEqual(string.Empty, directory);
        }
        finally
        {
            Directory.Delete(tempDirectory, true);
        }
    }

    [TestMethod]
    public void TryReadManifestInfo_IgnoresMismatchedIds()
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

            bool found = ManifestScanner.TryReadManifestInfo(tempPath, "BetterModMenu", out var info);

            Assert.IsFalse(found);
            Assert.IsFalse(info.AffectsGameplay);
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

    [TestMethod]
    public void BuildGroupLayout_SortsRowsAndGroupsBySavedOrder()
    {
        var rows = new[]
        {
            new ModdingScreenGroupLayoutRow<string>("Boss Mod", "boss"),
            new ModdingScreenGroupLayoutRow<string>("Unassigned Mod", "free"),
            new ModdingScreenGroupLayoutRow<string>("Elite Mod", "elite")
        };
        var assignedGroups = new Dictionary<string, string>
        {
            ["boss"] = "Bosses",
            ["elite"] = "Elites"
        };
        var modOrder = new Dictionary<string, int>
        {
            ["elite"] = 0,
            ["free"] = 1,
            ["boss"] = 2
        };

        var layout = ModdingScreenGroupLayoutBuilder.Build(
            rows,
            assignedGroups,
            new[] { "Bosses", "Elites" },
            new HashSet<string> { "Elites" },
            modOrder,
            ModdingScreenConstants.UnassignedGroup);

        CollectionAssert.AreEqual(
            new[] { ModdingScreenConstants.UnassignedGroup, "Bosses", "Elites" },
            layout.Groups.Select(group => group.Name).ToList());
        CollectionAssert.AreEqual(new[] { "Unassigned Mod" }, layout.Groups[0].Rows.Select(row => row.Item).ToList());
        CollectionAssert.AreEqual(new[] { "Boss Mod" }, layout.Groups[1].Rows.Select(row => row.Item).ToList());
        CollectionAssert.AreEqual(new[] { "Elite Mod" }, layout.Groups[2].Rows.Select(row => row.Item).ToList());
        Assert.IsFalse(layout.Groups[0].IsCollapsed);
        Assert.IsTrue(layout.Groups[2].IsCollapsed);
    }

    [TestMethod]
    public void BuildGroupLayout_OmitsEmptyUnassignedGroup()
    {
        var rows = new[]
        {
            new ModdingScreenGroupLayoutRow<string>("Boss Mod", "boss")
        };
        var assignedGroups = new Dictionary<string, string>
        {
            ["boss"] = "Bosses"
        };

        var layout = ModdingScreenGroupLayoutBuilder.Build(
            rows,
            assignedGroups,
            new[] { "Bosses" },
            Array.Empty<string>().ToHashSet(),
            new Dictionary<string, int>(),
            ModdingScreenConstants.UnassignedGroup);

        CollectionAssert.AreEqual(new[] { "Bosses" }, layout.Groups.Select(group => group.Name).ToList());
        CollectionAssert.AreEqual(new[] { "Boss Mod" }, layout.Groups[0].Rows.Select(row => row.Item).ToList());
    }

    [TestMethod]
    public void TryBuildMove_RepositionsModAcrossInterveningGroups_when_GroupedMoveRequested()
    {
        var modIds = new List<string> { "A", "X", "B", "C" };
        var assignedGroups = new Dictionary<string, string>
        {
            ["A"] = "Bosses",
            ["B"] = "Bosses",
            ["C"] = "Bosses",
            ["X"] = "Other"
        };

        Assert.IsTrue(ModOrderRules.TryBuildMove(modIds, assignedGroups, "A", 1, out var moveDown));
        CollectionAssert.AreEqual(
            new[] { "X", "B", "A", "C" },
            ApplyMove(modIds, moveDown.FromIndex, moveDown.InsertIndex));

        Assert.IsTrue(ModOrderRules.TryBuildMove(modIds, assignedGroups, "B", -1, out var moveUp));
        CollectionAssert.AreEqual(
            new[] { "B", "A", "X", "C" },
            ApplyMove(modIds, moveUp.FromIndex, moveUp.InsertIndex));
    }

    [TestMethod]
    public void TryBuildMove_ReturnsFalse_when_ModIsAlreadyAtGroupBoundary()
    {
        var modIds = new List<string> { "OtherTop", "A", "OtherMiddle", "B", "OtherBottom" };
        var assignedGroups = new Dictionary<string, string>
        {
            ["OtherTop"] = "Other",
            ["A"] = "Bosses",
            ["OtherMiddle"] = "Other",
            ["B"] = "Bosses",
            ["OtherBottom"] = "Other"
        };

        bool movePastGroupStart = ModOrderRules.TryBuildMove(modIds, assignedGroups, "A", -1, out _);
        bool movePastGroupEnd = ModOrderRules.TryBuildMove(modIds, assignedGroups, "B", 1, out _);

        Assert.IsFalse(movePastGroupStart);
        Assert.IsFalse(movePastGroupEnd);
    }

    [TestMethod]
    public void GetTopBarPresentation_UsesCompactLabels_when_AvailableWidthIsNarrow()
    {
        var compact = ModdingScreenLayoutRules.GetTopBarPresentation(isCompact: true);

        Assert.AreEqual("New", compact.NewProfile.Text);
        Assert.AreEqual("Edit", compact.RenameProfile.Text);
        Assert.AreEqual("Del", compact.DeleteProfile.Text);
        Assert.IsTrue(compact.NewProfile.TooltipText.Contains("New profile", StringComparison.Ordinal));
        Assert.IsTrue(compact.RenameProfile.TooltipText.Contains("Rename profile", StringComparison.Ordinal));
        Assert.IsTrue(compact.DeleteProfile.TooltipText.Contains("Delete profile", StringComparison.Ordinal));

        var wide = ModdingScreenLayoutRules.GetTopBarPresentation(isCompact: false);

        Assert.AreEqual("+ New", wide.NewProfile.Text);
        Assert.AreEqual("Rename", wide.RenameProfile.Text);
        Assert.AreEqual("Del", wide.DeleteProfile.Text);
    }

    [TestMethod]
    public void TryBackupExistingSave_CopiesSaveIntoTimestampedBackupFolder()
    {
        string tempDirectory = CreateTempDirectory();
        try
        {
            string savePath = Path.Combine(tempDirectory, "mod_profiles.json5");
            File.WriteAllText(savePath, "{ \"Profiles\": [] }");
            var timestamp = new DateTimeOffset(2026, 6, 5, 21, 0, 0, TimeSpan.Zero);

            bool backedUp = ProfileBackupService.TryBackupExistingSave(savePath, ProfileBackupReason.RunStart, timestamp, out string backupPath, out string? error);

            Assert.IsTrue(backedUp, error);
            Assert.AreEqual(Path.Combine(tempDirectory, "backups", "mod_profiles.20260605-210000.runstart.json5"), backupPath);
            Assert.AreEqual(File.ReadAllText(savePath), File.ReadAllText(backupPath));
        }
        finally
        {
            Directory.Delete(tempDirectory, true);
        }
    }

    [TestMethod]
    public void ProfileSaveStorage_RoundTripsCurrentSaveData()
    {
        string tempDirectory = CreateTempDirectory();
        try
        {
            string savePath = Path.Combine(tempDirectory, "mod_profiles.json5");
            var saveData = new ProfileSaveData
            {
                Profiles = new List<ModProfile>
                {
                    new() { Name = "Bosses", DisabledMods = new HashSet<string> { "mod-a" } }
                },
                CurrentProfileIndex = 0,
                CustomGroups = new List<string> { "Bosses" },
                ModGroups = new Dictionary<string, string> { ["mod-a"] = "Bosses" },
                CollapsedGroups = new HashSet<string> { "Bosses" }
            };

            bool wrote = ProfileSaveStorage.TryWrite(savePath, saveData, _ => { }, out string? error);
            var loaded = ProfileSaveStorage.LoadOrDefault(savePath, _ => { });

            Assert.IsTrue(wrote, error);
            Assert.AreEqual("Bosses", loaded.Profiles[0].Name);
            Assert.IsTrue(loaded.Profiles[0].DisabledMods.Contains("mod-a"));
            Assert.AreEqual("Bosses", loaded.CustomGroups[0]);
            Assert.AreEqual("Bosses", loaded.ModGroups["mod-a"]);
            Assert.IsTrue(loaded.CollapsedGroups.Contains("Bosses"));
        }
        finally
        {
            Directory.Delete(tempDirectory, true);
        }
    }

    [TestMethod]
    public void ProfileSaveStorage_LoadOrDefault_ReadsLegacyProfileList()
    {
        string tempDirectory = CreateTempDirectory();
        try
        {
            string savePath = Path.Combine(tempDirectory, "mod_profiles.json");
            File.WriteAllText(savePath, """
            [
              {
                "Name": "Legacy",
                "DisabledMods": ["mod-a"]
              }
            ]
            """);

            var loaded = ProfileSaveStorage.LoadOrDefault(savePath, _ => { });

            Assert.AreEqual("Legacy", loaded.Profiles[0].Name);
            Assert.IsTrue(loaded.Profiles[0].DisabledMods.Contains("mod-a"));
        }
        finally
        {
            Directory.Delete(tempDirectory, true);
        }
    }

    [TestMethod]
    public void TryBackupExistingSave_ReturnsFalseWithoutErrorWhenSourceDoesNotExist()
    {
        string tempDirectory = CreateTempDirectory();
        try
        {
            bool backedUp = ProfileBackupService.TryBackupExistingSave(
                Path.Combine(tempDirectory, "mod_profiles.json"),
                ProfileBackupReason.Manual,
                DateTimeOffset.UnixEpoch,
                out string backupPath,
                out string? error);

            Assert.IsFalse(backedUp);
            Assert.AreEqual(string.Empty, backupPath);
            Assert.IsNull(error);
        }
        finally
        {
            Directory.Delete(tempDirectory, true);
        }
    }

    [TestMethod]
    public void TryListBackups_ReturnsProfileBackupsNewestFirst()
    {
        string tempDirectory = CreateTempDirectory();
        try
        {
            string savePath = Path.Combine(tempDirectory, "mod_profiles.json");
            string backupDirectory = Path.Combine(tempDirectory, "backups");
            Directory.CreateDirectory(backupDirectory);
            string oldBackup = Path.Combine(backupDirectory, "mod_profiles.20260605-210000.manual.json");
            string newBackup = Path.Combine(backupDirectory, "mod_profiles.20260606-210000.manual.json");
            string settingsBackup = Path.Combine(backupDirectory, "mod_settings.20260607-210000.manual.json");
            File.WriteAllText(oldBackup, "{ \"Profiles\": [{ \"Name\": \"Old\" }] }");
            File.WriteAllText(newBackup, "{ \"Profiles\": [{ \"Name\": \"New\" }] }");
            File.WriteAllText(settingsBackup, "[]");
            File.SetLastWriteTimeUtc(oldBackup, new DateTime(2026, 6, 5, 21, 0, 0, DateTimeKind.Utc));
            File.SetLastWriteTimeUtc(newBackup, new DateTime(2026, 6, 6, 21, 0, 0, DateTimeKind.Utc));
            File.SetLastWriteTimeUtc(settingsBackup, new DateTime(2026, 6, 7, 21, 0, 0, DateTimeKind.Utc));

            bool found = ProfileBackupService.TryListBackups(savePath, new[] { ".json", ".json5", ".jsonc" }, out var backups, out string? error);

            Assert.IsTrue(found, error);
            Assert.AreEqual(2, backups.Count);
            Assert.AreEqual(newBackup, backups[0].Path);
            Assert.AreEqual(oldBackup, backups[1].Path);
            StringAssert.Contains(backups[0].Label, "2026-06-06");
            StringAssert.Contains(backups[0].Label, "Manual backup");
        }
        finally
        {
            Directory.Delete(tempDirectory, true);
        }
    }

    [TestMethod]
    public void BuildCsv_EscapesExcelFriendlyModExportRows()
    {
        string csv = ModListExportBuilder.BuildCsv(new[]
        {
            new ModListExportRow
            {
                ModId = "Example.Mod",
                Name = "Example, Mod",
                Version = "1.2.3",
                Enabled = true,
                Group = "QoL \"Core\""
            }
        });

        string expected = string.Join(Environment.NewLine,
            "Mod Id,Name,Version,Enabled,Group",
            "Example.Mod,\"Example, Mod\",1.2.3,TRUE,\"QoL \"\"Core\"\"\"",
            string.Empty);

        Assert.AreEqual(expected, csv);
    }

    [TestMethod]
    public void TryReadManifestInfo_ReadsSpecManifestInfo()
    {
        string tempDirectory = CreateTempDirectory();
        try
        {
            string manifestPath = Path.Combine(tempDirectory, "Example.Mod.json");
            File.WriteAllText(manifestPath, """
            {
              "id": "Example.Mod",
              "name": "Example Mod",
              "version": "1.2.3",
              "affects_gameplay": true
            }
            """);

            bool read = ManifestScanner.TryReadManifestInfo(manifestPath, "Example.Mod", out var info);

            Assert.IsTrue(read);
            Assert.AreEqual("Example.Mod", info.Id);
            Assert.AreEqual("Example Mod", info.Name);
            Assert.AreEqual("1.2.3", info.Version);
            Assert.IsTrue(info.AffectsGameplay);
        }
        finally
        {
            Directory.Delete(tempDirectory, true);
        }
    }

    [TestMethod]
    public void BuildRows_UsesManifestInfoAndAssignedGroups()
    {
        string tempDirectory = CreateTempDirectory();
        try
        {
            string manifestPath = Path.Combine(tempDirectory, "Example.Mod.json");
            File.WriteAllText(manifestPath, """
            {
              "id": "Example.Mod",
              "name": "Example Mod",
              "version": "1.2.3"
            }
            """);

            var rows = ModListExportBuilder.BuildRows(
                new[]
                {
                    new InstalledModExportInput
                    {
                        ModId = "Example.Mod",
                        Enabled = false,
                        ManifestPath = manifestPath
                    }
                },
                new Dictionary<string, string> { ["Example.Mod"] = "Core" },
                "Unassigned");

            Assert.AreEqual(1, rows.Count);
            Assert.AreEqual("Example.Mod", rows[0].ModId);
            Assert.AreEqual("Example Mod", rows[0].Name);
            Assert.AreEqual("1.2.3", rows[0].Version);
            Assert.IsFalse(rows[0].Enabled);
            Assert.AreEqual("Core", rows[0].Group);
        }
        finally
        {
            Directory.Delete(tempDirectory, true);
        }
    }

    [TestMethod]
    public void TryWriteCsv_WritesUniqueTimestampedExport()
    {
        string tempDirectory = CreateTempDirectory();
        try
        {
            var timestamp = new DateTimeOffset(2026, 6, 5, 22, 0, 0, TimeSpan.Zero);
            File.WriteAllText(Path.Combine(tempDirectory, "mod_list.20260605-220000.csv"), "existing");

            bool wrote = ModListExportBuilder.TryWriteCsv(
                tempDirectory,
                Array.Empty<ModListExportRow>(),
                timestamp,
                out string exportPath,
                out string? error);

            Assert.IsTrue(wrote, error);
            Assert.AreEqual(Path.Combine(tempDirectory, "mod_list.20260605-220000-2.csv"), exportPath);
            Assert.IsTrue(File.ReadAllText(exportPath).StartsWith("Mod Id,Name,Version,Enabled,Group", StringComparison.Ordinal));
        }
        finally
        {
            Directory.Delete(tempDirectory, true);
        }
    }

    [TestMethod]
    public void TryWriteSnapshot_WritesUniqueTimestampedModSettingsBackup()
    {
        string tempDirectory = CreateTempDirectory();
        try
        {
            var timestamp = new DateTimeOffset(2026, 6, 5, 23, 0, 0, TimeSpan.Zero);
            File.WriteAllText(Path.Combine(tempDirectory, "mod_settings.20260605-230000.manual.json"), "existing");

            bool wrote = ModSettingsBackupService.TryWriteSnapshot(
                tempDirectory,
                new[]
                {
                    new InstalledModExportInput { ModId = "Example.B", Enabled = false },
                    new InstalledModExportInput { ModId = "Example.A", Enabled = true }
                },
                ProfileBackupReason.Manual,
                timestamp,
                out string backupPath,
                out string? error);

            Assert.IsTrue(wrote, error);
            Assert.AreEqual(Path.Combine(tempDirectory, "mod_settings.20260605-230000.manual-2.json"), backupPath);
            string content = File.ReadAllText(backupPath);
            Assert.IsTrue(content.IndexOf("Example.A", StringComparison.Ordinal) < content.IndexOf("Example.B", StringComparison.Ordinal));
            Assert.IsTrue(content.Contains("\"Enabled\": true", StringComparison.Ordinal));
            Assert.IsTrue(content.Contains("\"Enabled\": false", StringComparison.Ordinal));
        }
        finally
        {
            Directory.Delete(tempDirectory, true);
        }
    }

    [TestMethod]
    public void TryWriteSnapshot_ReturnsFalseWithoutErrorWhenNoModSettingsExist()
    {
        string tempDirectory = CreateTempDirectory();
        try
        {
            bool wrote = ModSettingsBackupService.TryWriteSnapshot(
                tempDirectory,
                Array.Empty<InstalledModExportInput>(),
                ProfileBackupReason.Manual,
                DateTimeOffset.UnixEpoch,
                out string backupPath,
                out string? error);

            Assert.IsFalse(wrote);
            Assert.AreEqual(string.Empty, backupPath);
            Assert.IsNull(error);
        }
        finally
        {
            Directory.Delete(tempDirectory, true);
        }
    }

    [TestMethod]
    public void ShouldShowTutorial_ReturnsTrueOnFirstOpenAndAfterVersionChange()
    {
        var state = new TutorialState();

        Assert.IsTrue(TutorialStateRules.ShouldShowTutorial(state, "1.6.0"));

        TutorialStateRules.MarkSeen(state, "1.6.0");

        Assert.IsFalse(TutorialStateRules.ShouldShowTutorial(state, "1.6.0"));
        Assert.IsTrue(TutorialStateRules.ShouldShowTutorial(state, "1.6.1"));
    }

    [TestMethod]
    public void ShouldShowTutorial_ReturnsFalseWhenCurrentVersionIsUnknown()
    {
        Assert.IsFalse(TutorialStateRules.ShouldShowTutorial(new TutorialState(), ""));
        Assert.IsFalse(TutorialStateRules.ShouldShowTutorial(new TutorialState(), "   "));
    }

    [TestMethod]
    public void BuildBody_DescribesV16ActionsWithoutRuntimeSpecificInstructions()
    {
        string body = TutorialContentBuilder.BuildBody();

        StringAssert.Contains(body, "profiles");
        StringAssert.Contains(body, "Portable Mode");
        StringAssert.Contains(body, "Backup");
        StringAssert.Contains(body, "CSV");
        StringAssert.Contains(body, "Logs");
        StringAssert.Contains(body, "Load lets you choose");
        Assert.IsFalse(body.Contains("timestamped safety", StringComparison.OrdinalIgnoreCase));
        StringAssert.Contains(body, "cloud behavior stays opt-in");
    }

    [TestMethod]
    public void TryReadManifestInfo_ReturnsManifestVersionOnlyForExpectedId()
    {
        string tempDirectory = CreateTempDirectory();
        try
        {
            string manifestPath = Path.Combine(tempDirectory, "BetterModMenu.json");
            File.WriteAllText(manifestPath, """
            {
              "id": "BetterModMenu",
              "version": "1.6.0"
            }
            """);

            Assert.IsTrue(ManifestScanner.TryReadManifestInfo(manifestPath, "BetterModMenu", out var info));
            Assert.AreEqual("1.6.0", info.Version);
            Assert.IsFalse(ManifestScanner.TryReadManifestInfo(manifestPath, "OtherMod", out _));
        }
        finally
        {
            Directory.Delete(tempDirectory, true);
        }
    }

    [TestMethod]
    public void ProjectPackaging_AddsCloudSuffixAndDefinesOnlyWhenOptedIn()
    {
        string project = File.ReadAllText(Path.Combine(GetRepoRoot(), "BetterModMenu.csproj"));

        StringAssert.Contains(project, "<IncludeCloudFeatures Condition=\"'$(IncludeCloudFeatures)' == ''\">false</IncludeCloudFeatures>");
        StringAssert.Contains(project, "<CloudPackageSuffix Condition=\"'$(IncludeCloudFeatures)' == 'true'\">_cloud</CloudPackageSuffix>");
        StringAssert.Contains(project, "<CloudPackageSuffix Condition=\"'$(IncludeCloudFeatures)' != 'true'\"></CloudPackageSuffix>");
        StringAssert.Contains(project, "<DefineConstants Condition=\"'$(IncludeCloudFeatures)' == 'true'\">$(DefineConstants);BETTERMODMENU_CLOUD_FEATURES</DefineConstants>");
        StringAssert.Contains(project, "<BaseZipName>$(AssemblyName)_v$(ModVersion)$(CloudPackageSuffix)</BaseZipName>");
    }

    [TestMethod]
    public void NexusReleaseWorkflow_UploadsDefaultPackageAndTreatsCloudPackageAsSidecar()
    {
        string workflow = File.ReadAllText(Path.Combine(GetRepoRoot(), ".github", "workflows", "publish-nexus-release.yml"));

        StringAssert.Contains(workflow, "--pattern \"BetterModMenu_v*.zip\"");
        StringAssert.Contains(workflow, "find release-assets -maxdepth 1 -type f -name 'BetterModMenu_v*.zip' ! -name '*_cloud.zip'");
        StringAssert.Contains(workflow, "Cloud-capable *_cloud.zip assets are optional sidecars and are not uploaded as the main Nexus file.");
        StringAssert.Contains(workflow, "find release-assets -maxdepth 1 -type f -name 'BetterModMenu_v*_cloud.zip'");
    }

    [TestMethod]
    public void TryReadTail_ReturnsLastRequestedLines()
    {
        string tempDirectory = CreateTempDirectory();
        try
        {
            string logPath = Path.Combine(tempDirectory, "TTSMM.log");
            File.WriteAllLines(logPath, new[] { "one", "two", "three", "four" });

            bool read = LogViewerService.TryReadTail(logPath, maxLines: 2, maxChars: 100, out string content, out string? error);

            Assert.IsTrue(read, error);
            Assert.AreEqual(string.Join(Environment.NewLine, "three", "four"), content);
        }
        finally
        {
            Directory.Delete(tempDirectory, true);
        }
    }

    [TestMethod]
    public void BuildHighlightedBbCode_HighlightsStartupWarningModNames()
    {
        string content = string.Join('\n',
            "Mod STS2-QuickAnimationMode has a mod manifest that should be migrated! See logs for more info.",
            "Assembly DLL for mod SoloOne failed to initialize! See logs for more info.",
            "Running Modded. Loaded 19 mods WITH ERRORS!");

        string highlighted = LogHighlightService.BuildHighlightedBbCode(content);

        StringAssert.Contains(highlighted, "[color=ff5a4e][b]STS2-QuickAnimationMode[/b][/color]");
        StringAssert.Contains(highlighted, "[color=ff4d4d][b]SoloOne[/b][/color]");
        StringAssert.Contains(highlighted, "[color=ff4040][b]Running Modded. Loaded 19 mods WITH ERRORS![/b][/color]");
    }

    [TestMethod]
    public void TryReadTail_CanReadLogOpenForSharedWriting()
    {
        string tempDirectory = CreateTempDirectory();
        try
        {
            string logPath = Path.Combine(tempDirectory, "godot.log");
            using (var writerStream = new FileStream(logPath, FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite | FileShare.Delete))
            {
                using var writer = new StreamWriter(writerStream, Encoding.UTF8, leaveOpen: true);
                writer.WriteLine("one");
                writer.WriteLine("two");
                writer.Flush();

                bool read = LogViewerService.TryReadTail(logPath, maxLines: 10, maxChars: 100, out string content, out string? error);

                Assert.IsTrue(read, error);
                StringAssert.Contains(content, "one");
                StringAssert.Contains(content, "two");
            }
        }
        finally
        {
            Directory.Delete(tempDirectory, true);
        }
    }

    [TestMethod]
    public void TryReadTail_TruncatesByCharactersAfterLineSelection()
    {
        string tempDirectory = CreateTempDirectory();
        try
        {
            string logPath = Path.Combine(tempDirectory, "TTSMM.log");
            File.WriteAllText(logPath, "abcdef");

            bool read = LogViewerService.TryReadTail(logPath, maxLines: 10, maxChars: 3, out string content, out string? error);

            Assert.IsTrue(read, error);
            Assert.AreEqual("def", content);
        }
        finally
        {
            Directory.Delete(tempDirectory, true);
        }
    }

    [TestMethod]
    public void TryReadLatestLog_UsesFirstExistingCandidate()
    {
        string tempDirectory = CreateTempDirectory();
        try
        {
            string first = Path.Combine(tempDirectory, "missing.log");
            string second = Path.Combine(tempDirectory, "BetterModMenu.log");
            File.WriteAllText(second, "hello");

            bool read = LogViewerService.TryReadLatestLog(new[] { first, second }, 10, 100, out string title, out string content, out string? error);

            Assert.IsTrue(read, error);
            Assert.AreEqual("BetterModMenu.log", title);
            Assert.AreEqual("hello", content);
        }
        finally
        {
            Directory.Delete(tempDirectory, true);
        }
    }

    [TestMethod]
    public void BuildCandidatePaths_IncludesDirectAndNestedKnownLogLocationsWithoutDuplicates()
    {
        string tempDirectory = CreateTempDirectory();
        try
        {
            var paths = LogViewerService.BuildCandidatePaths(new[] { tempDirectory, tempDirectory });

            CollectionAssert.Contains(paths.ToList(), Path.Combine(tempDirectory, "TTSMM.log"));
            CollectionAssert.Contains(paths.ToList(), Path.Combine(tempDirectory, "logs", "TTSMM.log"));
            CollectionAssert.Contains(paths.ToList(), Path.Combine(tempDirectory, "logs", "BetterModMenu.log"));
            CollectionAssert.Contains(paths.ToList(), Path.Combine(tempDirectory, "player.log"));
            Assert.AreEqual(paths.Count, paths.Distinct(StringComparer.OrdinalIgnoreCase).Count());
        }
        finally
        {
            Directory.Delete(tempDirectory, true);
        }
    }

    [TestMethod]
    public void BuildCandidatePaths_IncludesAncestorLogDirectoriesForRealSts2Layout()
    {
        string tempDirectory = CreateTempDirectory();
        try
        {
            string configDirectory = Path.Combine(tempDirectory, "SlayTheSpire2", "steam", "76561198307270194", "mod_data", "BetterModMenu");

            var paths = LogViewerService.BuildCandidatePaths(new[] { configDirectory });

            CollectionAssert.Contains(paths.ToList(), Path.Combine(tempDirectory, "SlayTheSpire2", "logs", "godot.log"));
        }
        finally
        {
            Directory.Delete(tempDirectory, true);
        }
    }

    [TestMethod]
    public void FitTutorialDialogToViewport_KeepsDialogInsideInitial1080pWindow()
    {
        TutorialDialogLayout layout = ModdingScreenDialogRules.FitTutorialDialogToViewport(
            ModdingScreenDialogRules.GetPreferredTutorialDialogLayout(),
            viewportWidth: 1080,
            viewportHeight: 720);

        Assert.IsTrue(layout.PopupWidth <= 1000);
        Assert.IsTrue(layout.PopupHeight <= 640);
        Assert.IsTrue(layout.ContentWidth < layout.PopupWidth);
        Assert.IsTrue(layout.ContentHeight < layout.PopupHeight);
        Assert.IsTrue(layout.BodyFontSize >= 22);
    }

    [TestMethod]
    public void FitTutorialDialogToViewport_ReservesRoomForDialogButtons()
    {
        TutorialDialogLayout layout = ModdingScreenDialogRules.FitTutorialDialogToViewport(
            ModdingScreenDialogRules.GetPreferredTutorialDialogLayout(),
            viewportWidth: 1080,
            viewportHeight: 720);

        Assert.IsTrue(layout.PopupHeight - layout.ContentHeight >= 180);
    }

    [TestMethod]
    public void ShouldCreateAutomaticBackup_RunsAutomaticReasonsOnceAndNeverForManual()
    {
        var completedReasons = new HashSet<ProfileBackupReason>();

        Assert.IsTrue(BackupTriggerRules.ShouldCreateAutomaticBackup(completedReasons, ProfileBackupReason.RunStart));

        BackupTriggerRules.MarkAutomaticBackupCreated(completedReasons, ProfileBackupReason.RunStart);

        Assert.IsFalse(BackupTriggerRules.ShouldCreateAutomaticBackup(completedReasons, ProfileBackupReason.RunStart));
        Assert.IsTrue(BackupTriggerRules.ShouldCreateAutomaticBackup(completedReasons, ProfileBackupReason.Resume));
        Assert.IsFalse(BackupTriggerRules.ShouldCreateAutomaticBackup(completedReasons, ProfileBackupReason.Manual));
    }

    [TestMethod]
    public void ShouldMirror_RequiresEnabledCloudDirectoryAndMatchingKind()
    {
        var settings = new CloudBackupSettings
        {
            Enabled = true,
            Directory = @"C:\Cloud\BetterModMenu",
            MirrorProfileBackups = true,
            MirrorModSettingsBackups = true,
            MirrorModListExports = false
        };

        Assert.IsTrue(CloudBackupService.ShouldMirror(settings, CloudBackupKind.ProfileSettings));
        Assert.IsTrue(CloudBackupService.ShouldMirror(settings, CloudBackupKind.ModSettings));
        Assert.IsFalse(CloudBackupService.ShouldMirror(settings, CloudBackupKind.ModList));

        settings.Directory = "";

        Assert.IsFalse(CloudBackupService.ShouldMirror(settings, CloudBackupKind.ProfileSettings));
    }

    [TestMethod]
    public void WithDirectory_TrimsAndEnablesCloudBackupsWhilePreservingMirrorChoices()
    {
        var current = new CloudBackupSettings
        {
            Enabled = false,
            Directory = "",
            MirrorProfileBackups = false,
            MirrorModSettingsBackups = true,
            MirrorModListExports = false
        };

        var updated = CloudBackupSettingsRules.WithDirectory(current, "  C:\\Users\\you\\OneDrive\\BetterModMenu  ");

        Assert.IsTrue(updated.Enabled);
        Assert.AreEqual("C:\\Users\\you\\OneDrive\\BetterModMenu", updated.Directory);
        Assert.IsFalse(updated.MirrorProfileBackups);
        Assert.IsTrue(updated.MirrorModSettingsBackups);
        Assert.IsFalse(updated.MirrorModListExports);
    }

    [TestMethod]
    public void WithDirectory_DisablesCloudBackupsWhenDirectoryIsBlank()
    {
        var updated = CloudBackupSettingsRules.WithDirectory(
            new CloudBackupSettings { Enabled = true, Directory = "C:\\Cloud" },
            "   ");

        Assert.IsFalse(updated.Enabled);
        Assert.AreEqual(string.Empty, updated.Directory);
    }

    [TestMethod]
    public void TryMirrorFile_CopiesFileIntoKindSpecificCloudDirectoryWithUniqueName()
    {
        string tempDirectory = CreateTempDirectory();
        try
        {
            string sourcePath = Path.Combine(tempDirectory, "mod_profiles.20260605-210000.manual.json5");
            File.WriteAllText(sourcePath, "backup");
            string cloudDirectory = Path.Combine(tempDirectory, "cloud");
            string categoryDirectory = Path.Combine(cloudDirectory, CloudBackupService.ProfileSettingsCategory);
            Directory.CreateDirectory(categoryDirectory);
            File.WriteAllText(Path.Combine(categoryDirectory, Path.GetFileName(sourcePath)), "existing");

            var settings = new CloudBackupSettings
            {
                Enabled = true,
                Directory = cloudDirectory
            };

            bool mirrored = CloudBackupService.TryMirrorFile(settings, CloudBackupKind.ProfileSettings, sourcePath, out string mirroredPath, out string? error);

            Assert.IsTrue(mirrored, error);
            Assert.AreEqual(Path.Combine(categoryDirectory, "mod_profiles.20260605-210000.manual-2.json5"), mirroredPath);
            Assert.AreEqual("backup", File.ReadAllText(mirroredPath));
        }
        finally
        {
            Directory.Delete(tempDirectory, true);
        }
    }

    private static List<string> ApplyMove(IReadOnlyList<string> modIds, int fromIndex, int insertIndex)
    {
        var moved = new List<string>(modIds);
        string item = moved[fromIndex];
        moved.RemoveAt(fromIndex);
        moved.Insert(insertIndex, item);
        return moved;
    }

    private static string CreateTempDirectory()
    {
        string tempDirectory = Path.Combine(Path.GetTempPath(), "BetterModMenuTests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDirectory);
        return tempDirectory;
    }

    private static string GetRepoRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory != null && !File.Exists(Path.Combine(directory.FullName, "BetterModMenu.csproj")))
            directory = directory.Parent;

        return directory?.FullName ?? throw new DirectoryNotFoundException("Could not find BetterModMenu.csproj.");
    }

}
