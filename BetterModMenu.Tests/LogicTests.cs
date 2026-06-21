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
    public void TryReadManifestInfo_ReadsSearchMetadata()
    {
        string tempPath = Path.Combine(Path.GetTempPath(), "BetterModMenuTests_" + Guid.NewGuid().ToString("N") + ".json");
        try
        {
            File.WriteAllText(tempPath, """
            {
              "id": "Example.Mod",
              "name": "Example Mod",
              "author": "Tester",
              "description": "Adds config helpers.",
              "version": "1.2.3",
              "dependencies": [
                "BaseLib",
                { "id": "STS2-RitsuLib", "min_version": "0.4.0" }
              ],
              "affects_gameplay": true
            }
            """);

            bool found = ManifestScanner.TryReadManifestInfo(tempPath, "Example.Mod", out var info);

            Assert.IsTrue(found);
            Assert.AreEqual("Tester", info.Author);
            Assert.AreEqual("Adds config helpers.", info.Description);
            CollectionAssert.AreEqual(new[] { "BaseLib", "STS2-RitsuLib" }, info.Dependencies.ToList());
        }
        finally
        {
            if (File.Exists(tempPath))
                File.Delete(tempPath);
        }
    }

    [TestMethod]
    public void Search_RanksExactDependencyModBeforeDependents()
    {
        var documents = new[]
        {
            new ModSearchDocument("BaseLib", "BaseLib") { Description = "Modding utility" },
            new ModSearchDocument("Example.Mod", "Example Mod") { Dependencies = new[] { "BaseLib" } }
        };

        var results = ModSearchRules.Search(documents, "BaseLib");

        Assert.AreEqual("BaseLib", results[0].ModId);
        Assert.AreEqual("Example.Mod", results[1].ModId);
        StringAssert.Contains(results[1].MatchReason, "dependency");
    }

    [TestMethod]
    public void Search_ToleratesTyposAndLookalikesForLongQueries()
    {
        var documents = new[]
        {
            new ModSearchDocument("BaseLib", "BaseLib"),
            new ModSearchDocument("Other.Mod", "Other Mod")
        };

        var results = ModSearchRules.Search(documents, "ba5e lib");

        Assert.AreEqual(1, results.Count);
        Assert.AreEqual("BaseLib", results[0].ModId);
    }

    [TestMethod]
    public void Search_KeepsShortQueriesPrecise()
    {
        var documents = new[]
        {
            new ModSearchDocument("BaseLib", "BaseLib"),
            new ModSearchDocument("CombatTweaks", "Combat Tweaks") { Description = "Better balance" }
        };

        var results = ModSearchRules.Search(documents, "b");

        CollectionAssert.AreEqual(new[] { "BaseLib" }, results.Select(result => result.ModId).ToList());
    }

    [TestMethod]
    public void Search_MatchesWorkshopId()
    {
        var documents = new[]
        {
            new ModSearchDocument("Example.Mod", "Example Mod")
            {
                WorkshopId = "3456789012",
                WorkshopUrl = "https://steamcommunity.com/sharedfiles/filedetails/?id=3456789012"
            }
        };

        var results = ModSearchRules.Search(documents, "3456789012");

        Assert.AreEqual("Example.Mod", results[0].ModId);
        StringAssert.Contains(results[0].MatchReason, "Workshop");
    }

    [TestMethod]
    public void PickSelectedMod_PreservesCurrentMatchOtherwiseUsesTopResult()
    {
        var results = new[]
        {
            new ModSearchResult { ModId = "Top.Mod", Score = 100 },
            new ModSearchResult { ModId = "Current.Mod", Score = 80 }
        };

        Assert.AreEqual("Current.Mod", ModSearchRules.PickSelectedModId("Current.Mod", results));
        Assert.AreEqual("Top.Mod", ModSearchRules.PickSelectedModId("Hidden.Mod", results));
        Assert.AreEqual(string.Empty, ModSearchRules.PickSelectedModId("Hidden.Mod", Array.Empty<ModSearchResult>()));
    }

    [TestMethod]
    public void SelectProvider_PrefersRitsuLibThenBaseLib()
    {
        Assert.AreEqual(ModConfigProviderKind.RitsuLib, ModConfigProviderRules.SelectProvider(ritsuLibAvailable: true, baseLibAvailable: true));
        Assert.AreEqual(ModConfigProviderKind.BaseLib, ModConfigProviderRules.SelectProvider(ritsuLibAvailable: false, baseLibAvailable: true));
        Assert.AreEqual(ModConfigProviderKind.None, ModConfigProviderRules.SelectProvider(ritsuLibAvailable: false, baseLibAvailable: false));
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
            "Mod Id,Name,Version,Enabled,Group,Workshop Link",
            "Example.Mod,\"Example, Mod\",1.2.3,TRUE,\"QoL \"\"Core\"\"\",",
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
                        ManifestPath = manifestPath,
                        WorkshopUrl = "https://steamcommunity.com/sharedfiles/filedetails/?id=3456789012"
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
            Assert.AreEqual("https://steamcommunity.com/sharedfiles/filedetails/?id=3456789012", rows[0].WorkshopUrl);
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
            Assert.IsTrue(File.ReadAllText(exportPath).StartsWith("Mod Id,Name,Version,Enabled,Group,Workshop Link", StringComparison.Ordinal));
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
                    new InstalledModExportInput
                    {
                        ModId = "Example.A",
                        Enabled = true,
                        WorkshopUrl = "https://steamcommunity.com/sharedfiles/filedetails/?id=123456789"
                    }
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
            StringAssert.Contains(content, "https://steamcommunity.com/sharedfiles/filedetails/?id=123456789");
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
        StringAssert.Contains(body, "level toggles");
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
        StringAssert.Contains(workflow, "Nexus-Mods/upload-action@f6e1e2ea683dfe8f88cccf642c6e0e69ab66825e");
        StringAssert.Contains(workflow, "# v1.0.0-beta.8");
        StringAssert.Contains(workflow, "file_id: ${{ vars.NEXUSMODS_FILE_ID }}");
        StringAssert.Contains(workflow, "category: main");
        StringAssert.Contains(workflow, "archive_existing_version: false");
        StringAssert.Contains(workflow, "primary_mod_manager_download: false");
        StringAssert.Contains(workflow, "allow_mod_manager_download: true");
        StringAssert.Contains(workflow, "show_requirements_pop_up: false");
        StringAssert.Contains(workflow, "steps.nexus.outputs.version_id");
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
    public void BuildHighlightedBbCode_HighlightsGenericWarningsAndErrors()
    {
        string content = string.Join('\n',
            "WARNING: base game reported a stale manifest",
            "System.Exception: load failed");

        string highlighted = LogHighlightService.BuildHighlightedBbCode(content);

        StringAssert.Contains(highlighted, "[color=f0b14a][b]WARNING: base game reported a stale manifest[/b][/color]");
        StringAssert.Contains(highlighted, "[color=ff4040][b]System.Exception: load failed[/b][/color]");
    }

    [TestMethod]
    public void Classify_RecognizesCommonLogLevelVariants()
    {
        Assert.AreEqual(LogLevelFilter.Debug, LogLevelFilterService.Classify("[DEBUG] Debug message"));
        Assert.AreEqual(LogLevelFilter.Debug, LogLevelFilterService.Classify("[lb]DEBUG[rb] Escaped debug message"));
        Assert.AreEqual(LogLevelFilter.Info, LogLevelFilterService.Classify("[Server thread/INFO] Informational message"));
        Assert.AreEqual(LogLevelFilter.Warning, LogLevelFilterService.Classify("[WARN] Warning message"));
        Assert.AreEqual(LogLevelFilter.Warning, LogLevelFilterService.Classify("[Server thread/WARN] Warning message"));
        Assert.AreEqual(LogLevelFilter.Warning, LogLevelFilterService.Classify("WARN: warning message"));
        Assert.AreEqual(LogLevelFilter.Warning, LogLevelFilterService.Classify("WARN: operation failed"));
        Assert.AreEqual(LogLevelFilter.Warning, LogLevelFilterService.Classify("WARNING: warning message"));
        Assert.AreEqual(LogLevelFilter.Warning, LogLevelFilterService.Classify("WARNING: Running Modded. Loaded 19 mods WITH ERRORS!"));
        Assert.AreEqual(LogLevelFilter.Error, LogLevelFilterService.Classify("[ERROR] Error message"));
        Assert.AreEqual(LogLevelFilter.Error, LogLevelFilterService.Classify("[Server thread/ERROR] Error message"));
        Assert.AreEqual(LogLevelFilter.Error, LogLevelFilterService.Classify("[ERR] Error message"));
        Assert.AreEqual(LogLevelFilter.Error, LogLevelFilterService.Classify("System.Exception: load failed"));
        Assert.AreEqual(LogLevelFilter.Other, LogLevelFilterService.Classify("plain continuation line"));
    }

    [TestMethod]
    public void Filter_CanShowOnlyOneLevelOrExcludeAnyLevel()
    {
        string content = string.Join('\n',
            "[DEBUG] debug line",
            "[INFO] info line",
            "[WARN] warning line",
            "[ERROR] error line",
            "plain continuation");

        Assert.AreEqual("[DEBUG] debug line", LogLevelFilterService.Filter(content, LogLevelFilter.Debug));
        Assert.AreEqual("[INFO] info line", LogLevelFilterService.Filter(content, LogLevelFilter.Info));
        Assert.AreEqual("[WARN] warning line", LogLevelFilterService.Filter(content, LogLevelFilter.Warning));
        Assert.AreEqual("[ERROR] error line", LogLevelFilterService.Filter(content, LogLevelFilter.Error));

        string withoutDebug = LogLevelFilterService.Filter(content, LogLevelFilter.All & ~LogLevelFilter.Debug);

        Assert.IsFalse(withoutDebug.Contains("debug line", StringComparison.Ordinal));
        StringAssert.Contains(withoutDebug, "info line");
        StringAssert.Contains(withoutDebug, "warning line");
        StringAssert.Contains(withoutDebug, "error line");
        StringAssert.Contains(withoutDebug, "plain continuation");
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

            bool read = LogViewerService.TryReadLatestLog(new[] { first, second }, 10, 100, out string title, out string content, out string logPath, out string? error);

            Assert.IsTrue(read, error);
            Assert.AreEqual("BetterModMenu.log", title);
            Assert.AreEqual("hello", content);
            Assert.AreEqual(second, logPath);
        }
        finally
        {
            Directory.Delete(tempDirectory, true);
        }
    }

    [TestMethod]
    public void TryReadLatestLog_DefaultLimitsReadRepresentativeFullLog()
    {
        string tempDirectory = CreateTempDirectory();
        try
        {
            string logPath = Path.Combine(tempDirectory, "TTSMM.log");
            File.WriteAllLines(logPath, Enumerable.Range(1, 250).Select(number => "line " + number));

            bool read = LogViewerService.TryReadLatestLog(
                new[] { logPath },
                LogViewerService.DefaultMaxLines,
                LogViewerService.DefaultMaxChars,
                out _,
                out string content,
                out string? error);

            Assert.IsTrue(read, error);
            StringAssert.Contains(content, "line 1");
            StringAssert.Contains(content, "line 250");
        }
        finally
        {
            Directory.Delete(tempDirectory, true);
        }
    }

    [TestMethod]
    public void TryGetContainingDirectory_ReturnsLogParentDirectory()
    {
        string tempDirectory = CreateTempDirectory();
        try
        {
            string logPath = Path.Combine(tempDirectory, "logs", "TTSMM.log");
            Directory.CreateDirectory(Path.GetDirectoryName(logPath)!);
            File.WriteAllText(logPath, "hello");

            bool found = LogFolderOpenRules.TryGetContainingDirectory(logPath, out string directory, out string? error);

            Assert.IsTrue(found, error);
            Assert.AreEqual(Path.GetDirectoryName(logPath), directory);
        }
        finally
        {
            Directory.Delete(tempDirectory, true);
        }
    }

    [TestMethod]
    public void TryGetContainingDirectory_ReturnsFalseWhenLogFileIsMissing()
    {
        string tempDirectory = CreateTempDirectory();
        try
        {
            string missingLogPath = Path.Combine(tempDirectory, "missing.log");

            bool found = LogFolderOpenRules.TryGetContainingDirectory(
                missingLogPath,
                out string directory,
                out string? error);

            Assert.IsFalse(found);
            Assert.AreEqual(string.Empty, directory);
            Assert.AreEqual("Log file no longer exists.", error);
        }
        finally
        {
            Directory.Delete(tempDirectory, true);
        }
    }

    [TestMethod]
    public void BuildOpenFolderCommands_UsesExplorerForWindows()
    {
        var commands = LogFolderOpenRules.BuildOpenFolderCommands(@"C:\Users\you\AppData\LocalLow\STS2\logs", FolderOpenPlatform.Windows);

        Assert.AreEqual(1, commands.Count);
        Assert.AreEqual("explorer.exe", commands[0].Executable);
        CollectionAssert.AreEqual(new[] { @"C:\Users\you\AppData\LocalLow\STS2\logs" }, commands[0].Arguments.ToArray());
    }

    [TestMethod]
    public void BuildOpenFolderCommands_UsesNonBlockingLinuxOpenersWithFileUri()
    {
        var commands = LogFolderOpenRules.BuildOpenFolderCommands("/home/deck/My Games/STS2/logs", FolderOpenPlatform.Linux);

        Assert.IsTrue(commands.Count >= 3);
        Assert.AreEqual("xdg-open", commands[0].Executable);
        Assert.AreEqual("file:///home/deck/My%20Games/STS2/logs/", commands[0].Arguments[0]);
        Assert.AreEqual("gio", commands[1].Executable);
        CollectionAssert.AreEqual(new[] { "open", "file:///home/deck/My%20Games/STS2/logs/" }, commands[1].Arguments.ToArray());
    }

    [TestMethod]
    public void TryGetWorkshopUrl_ReturnsSteamSharedFileLinkForSts2WorkshopPath()
    {
        bool resolved = SteamWorkshopLinkResolver.TryGetWorkshopUrl(
            @"D:\SteamLibrary\steamapps\workshop\content\2868840\3456789012\Example.Mod\Example.Mod.json",
            out string workshopUrl);

        Assert.IsTrue(resolved);
        Assert.AreEqual("https://steamcommunity.com/sharedfiles/filedetails/?id=3456789012", workshopUrl);
    }

    [TestMethod]
    public void TryGetPublishedFileId_ReturnsSts2WorkshopId()
    {
        bool resolved = SteamWorkshopLinkResolver.TryGetPublishedFileId(
            @"D:\SteamLibrary\steamapps\workshop\content\2868840\3456789012\Example.Mod\Example.Mod.json",
            out string publishedFileId);

        Assert.IsTrue(resolved);
        Assert.AreEqual("3456789012", publishedFileId);
    }

    [TestMethod]
    public void TryGetWorkshopUrl_ReturnsFalseForLocalModPath()
    {
        bool resolved = SteamWorkshopLinkResolver.TryGetWorkshopUrl(
            @"D:\SteamLibrary\steamapps\common\Slay the Spire 2\mods\Example.Mod\Example.Mod.json",
            out string workshopUrl);

        Assert.IsFalse(resolved);
        Assert.AreEqual(string.Empty, workshopUrl);
    }

    [TestMethod]
    public void GetDefaultTagFormats_ReturnsOnlySupportedSteamWorkshopTags()
    {
        string[] supportedTags =
        {
            "<none selected>",
            "Acts",
            "Ancients",
            "Audio",
            "Cards",
            "Characters",
            "Cosmetics",
            "Events",
            "Expansion",
            "Extensions",
            "Humor",
            "Modifiers",
            "Monsters",
            "Potions",
            "QoL",
            "Relics",
            "Rooms",
            "Tools & APIs",
            "Utility",
            "Misc"
        };

        CollectionAssert.AreEqual(supportedTags, ModNameStyleRules.GetDefaultTagFormats().Keys.ToArray());
    }

    [TestMethod]
    public void BuildBbCode_UsesDefaultPriorityForWorkshopTags()
    {
        string bbCode = ModNameStyleRules.BuildBbCode(
            "BetterModMenu",
            "Better Mod Menu",
            new[] { "QoL", "Tools & APIs", "Utility" },
            new ModNameStyleSettings());

        Assert.AreEqual("[color=#74a6ff]Better Mod Menu[/color]", bbCode);
    }

    [TestMethod]
    public void BuildBbCode_DoesNotUseVisibleGroupsAsDefaultTags()
    {
        bool resolved = ModNameStyleRules.TryBuildBbCode(
            "BetterModMenu",
            "Better Mod Menu",
            new[] { "Libraries" },
            new ModNameStyleSettings(),
            out _);

        Assert.IsFalse(resolved);
    }

    [TestMethod]
    public void BuildBbCode_IgnoresCustomFormatsForUnsupportedWorkshopTags()
    {
        var settings = new ModNameStyleSettings
        {
            UseDefaultTagFormats = false,
            TagFormats = new Dictionary<string, string>
            {
                ["Multiplayer"] = "#ff8fb3"
            }
        };

        bool resolved = ModNameStyleRules.TryBuildBbCode(
            "BetterModMenu",
            "Better Mod Menu",
            new[] { "Multiplayer" },
            settings,
            out _);

        Assert.IsFalse(resolved);
    }

    [TestMethod]
    public void BuildBbCode_UsesCustomFormatForSupportedWorkshopTag()
    {
        var settings = new ModNameStyleSettings
        {
            TagFormats = new Dictionary<string, string>
            {
                ["QoL"] = "[color=#80f0b0][b]{name}[/b][/color]"
            }
        };

        string bbCode = ModNameStyleRules.BuildBbCode(
            "BetterModMenu",
            "Better Mod Menu",
            new[] { "QoL" },
            settings);

        Assert.AreEqual("[color=#80f0b0][b]Better Mod Menu[/b][/color]", bbCode);
    }

    [TestMethod]
    public void BuildBbCode_UsesDefaultPriorityForCustomOnlyTagFormats()
    {
        var settings = new ModNameStyleSettings
        {
            UseDefaultTagFormats = false,
            TagFormats = new Dictionary<string, string>
            {
                ["QoL"] = "#80f0b0",
                ["Tools & APIs"] = "#74a6ff"
            }
        };

        string bbCode = ModNameStyleRules.BuildBbCode(
            "BetterModMenu",
            "Better Mod Menu",
            new[] { "QoL", "Tools & APIs" },
            settings);

        Assert.AreEqual("[color=#74a6ff]Better Mod Menu[/color]", bbCode);
    }

    [TestMethod]
    public void BuildBbCode_TreatsQualityOfLifeTagAsQoLAlias()
    {
        string bbCode = ModNameStyleRules.BuildBbCode(
            "BetterModMenu",
            "Better Mod Menu",
            new[] { "Quality of Life" },
            new ModNameStyleSettings());

        Assert.AreEqual("[color=#b3ed5e]Better Mod Menu[/color]", bbCode);
    }

    [TestMethod]
    public void BuildBbCode_TrimsTagsAndConfigKeysBeforeAliasLookup()
    {
        Assert.AreEqual(
            "[color=#b3ed5e]Better Mod Menu[/color]",
            ModNameStyleRules.BuildBbCode(
                "BetterModMenu",
                "Better Mod Menu",
                new[] { " Quality of Life " },
                new ModNameStyleSettings()));

        Assert.AreEqual(
            "[color=#80f0b0]Better Mod Menu[/color]",
            ModNameStyleRules.BuildBbCode(
                "BetterModMenu",
                "Better Mod Menu",
                new[] { "QoL" },
                new ModNameStyleSettings
                {
                    TagFormats = new Dictionary<string, string>
                    {
                        [" Quality of Life "] = "#80f0b0"
                    }
                }));

        Assert.AreEqual(
            "[color=#eec46d]Better Mod Menu[/color]",
            ModNameStyleRules.BuildBbCode(
                "BetterModMenu",
                "Better Mod Menu",
                new[] { "Tools & APIs", "Utility" },
                new ModNameStyleSettings
                {
                    TagPriority = new List<string> { " Utilities " }
                }));

        Assert.AreEqual(
            "[color=#b8bec6]Better Mod Menu[/color]",
            ModNameStyleRules.BuildBbCode(
                "BetterModMenu",
                "Better Mod Menu",
                new[] { "QoL", "Misc" },
                new ModNameStyleSettings
                {
                    DisabledTags = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                    {
                        " Quality of Life "
                    }
                }));
    }

    [TestMethod]
    public void BuildBbCode_TreatsMechanicalTagVariantsAsAliases()
    {
        var cases = new[]
        {
            ("Card", "[color=#32d4ff]Alias Mod[/color]"),
            ("Character", "[color=#ff5ec7]Alias Mod[/color]"),
            ("Event", "[color=#ff7a35]Alias Mod[/color]"),
            ("Potion", "[color=#32e1ca]Alias Mod[/color]"),
            ("Relic", "[color=#c99638]Alias Mod[/color]"),
            ("Room", "[color=#82bd5c]Alias Mod[/color]"),
            ("Humour", "[color=#ffe066]Alias Mod[/color]"),
            ("Miscellaneous", "[color=#b8bec6]Alias Mod[/color]"),
            ("none selected", "[color=#8e99a6]Alias Mod[/color]")
        };

        foreach ((string tag, string expectedBbCode) in cases)
        {
            string bbCode = ModNameStyleRules.BuildBbCode(
                "Alias.Mod",
                "Alias Mod",
                new[] { tag },
                new ModNameStyleSettings());

            Assert.AreEqual(expectedBbCode, bbCode, tag);
        }
    }

    [TestMethod]
    public void BuildBbCode_AcceptsEveryWorkshopTagAlias()
    {
        var cases = new[]
        {
            ("none selected", "[color=#8e99a6]Alias Mod[/color]"),
            ("<none>", "[color=#8e99a6]Alias Mod[/color]"),
            ("none", "[color=#8e99a6]Alias Mod[/color]"),
            ("no tag", "[color=#8e99a6]Alias Mod[/color]"),
            ("no tags", "[color=#8e99a6]Alias Mod[/color]"),
            ("Act", "[color=#ffb257]Alias Mod[/color]"),
            ("Ancient", "[color=#dfd0a8]Alias Mod[/color]"),
            ("Card", "[color=#32d4ff]Alias Mod[/color]"),
            ("Character", "[color=#ff5ec7]Alias Mod[/color]"),
            ("Cosmetic", "[color=#f1a6ff]Alias Mod[/color]"),
            ("Event", "[color=#ff7a35]Alias Mod[/color]"),
            ("Expansions", "[color=#ff7894]Alias Mod[/color]"),
            ("Extension", "[color=#5fd6a1]Alias Mod[/color]"),
            ("Humour", "[color=#ffe066]Alias Mod[/color]"),
            ("Modifier", "[color=#a47cff]Alias Mod[/color]"),
            ("Monster", "[color=#ff4d3d]Alias Mod[/color]"),
            ("Potion", "[color=#32e1ca]Alias Mod[/color]"),
            ("Quality of Life", "[color=#b3ed5e]Alias Mod[/color]"),
            ("Quality-of-Life", "[color=#b3ed5e]Alias Mod[/color]"),
            ("Quality Of Life", "[color=#b3ed5e]Alias Mod[/color]"),
            ("Q.O.L.", "[color=#b3ed5e]Alias Mod[/color]"),
            ("Relic", "[color=#c99638]Alias Mod[/color]"),
            ("Room", "[color=#82bd5c]Alias Mod[/color]"),
            ("Tool", "[color=#74a6ff]Alias Mod[/color]"),
            ("Tools", "[color=#74a6ff]Alias Mod[/color]"),
            ("API", "[color=#74a6ff]Alias Mod[/color]"),
            ("APIs", "[color=#74a6ff]Alias Mod[/color]"),
            ("Tools & API", "[color=#74a6ff]Alias Mod[/color]"),
            ("Tool & API", "[color=#74a6ff]Alias Mod[/color]"),
            ("Tool & APIs", "[color=#74a6ff]Alias Mod[/color]"),
            ("Tools and APIs", "[color=#74a6ff]Alias Mod[/color]"),
            ("Tools and API", "[color=#74a6ff]Alias Mod[/color]"),
            ("Tool and API", "[color=#74a6ff]Alias Mod[/color]"),
            ("Tool and APIs", "[color=#74a6ff]Alias Mod[/color]"),
            ("Utilities", "[color=#eec46d]Alias Mod[/color]"),
            ("Miscellaneous", "[color=#b8bec6]Alias Mod[/color]")
        };

        foreach ((string alias, string expectedBbCode) in cases)
        {
            string bbCode = ModNameStyleRules.BuildBbCode(
                "Alias.Mod",
                "Alias Mod",
                new[] { alias },
                new ModNameStyleSettings());

            Assert.AreEqual(expectedBbCode, bbCode, alias);
        }
    }

    [TestMethod]
    public void BuildBbCode_TreatsToolsAndApiVariantsAsToolsAndApisAliases()
    {
        var aliases = new[]
        {
            "Tool",
            "Tools",
            "API",
            "APIs",
            "Tools & API",
            "Tools and APIs"
        };

        foreach (string tag in aliases)
        {
            string bbCode = ModNameStyleRules.BuildBbCode(
                "Alias.Mod",
                "Alias Mod",
                new[] { tag },
                new ModNameStyleSettings());

            Assert.AreEqual("[color=#74a6ff]Alias Mod[/color]", bbCode, tag);
        }
    }

    [TestMethod]
    public void BuildBbCode_UsesQoLCustomFormatForQualityOfLifeAlias()
    {
        var settings = new ModNameStyleSettings
        {
            TagFormats = new Dictionary<string, string>
            {
                ["QoL"] = "#80f0b0"
            }
        };

        string bbCode = ModNameStyleRules.BuildBbCode(
            "BetterModMenu",
            "Better Mod Menu",
            new[] { "Quality of Life" },
            settings);

        Assert.AreEqual("[color=#80f0b0]Better Mod Menu[/color]", bbCode);
    }

    [TestMethod]
    public void BuildBbCode_UsesQualityOfLifeCustomFormatForQoLTag()
    {
        var settings = new ModNameStyleSettings
        {
            TagFormats = new Dictionary<string, string>
            {
                ["Quality of Life"] = "#80f0b0"
            }
        };

        string bbCode = ModNameStyleRules.BuildBbCode(
            "BetterModMenu",
            "Better Mod Menu",
            new[] { "QoL" },
            settings);

        Assert.AreEqual("[color=#80f0b0]Better Mod Menu[/color]", bbCode);
    }

    [TestMethod]
    public void BuildBbCode_UsesCustomTagPriorityBeforeDefaultPriority()
    {
        var settings = new ModNameStyleSettings
        {
            TagPriority = new List<string> { "QoL", "Tools & APIs" }
        };

        string bbCode = ModNameStyleRules.BuildBbCode(
            "BetterModMenu",
            "Better Mod Menu",
            new[] { "Tools & APIs", "QoL" },
            settings);

        Assert.AreEqual("[color=#b3ed5e]Better Mod Menu[/color]", bbCode);
    }

    [TestMethod]
    public void BuildBbCode_IgnoresUnsupportedCustomPriorityTags()
    {
        var settings = new ModNameStyleSettings
        {
            TagPriority = new List<string> { "Multiplayer", "QoL" }
        };

        string bbCode = ModNameStyleRules.BuildBbCode(
            "BetterModMenu",
            "Better Mod Menu",
            new[] { "Tools & APIs", "QoL" },
            settings);

        Assert.AreEqual("[color=#b3ed5e]Better Mod Menu[/color]", bbCode);
    }

    [TestMethod]
    public void BuildBbCode_SkipsDisabledSupportedWorkshopTags()
    {
        var settings = new ModNameStyleSettings
        {
            DisabledTags = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "Tools & APIs"
            }
        };

        string bbCode = ModNameStyleRules.BuildBbCode(
            "BetterModMenu",
            "Better Mod Menu",
            new[] { "Tools & APIs", "QoL" },
            settings);

        Assert.AreEqual("[color=#b3ed5e]Better Mod Menu[/color]", bbCode);
    }

    [TestMethod]
    public void TryBuildSimpleColor_ReturnsColorForPlainColorFormatting()
    {
        bool resolved = ModNameStyleRules.TryBuildSimpleColor(
            "BetterModMenu",
            "Better Mod Menu",
            new[] { "Tools & APIs" },
            new ModNameStyleSettings(),
            out string color);

        Assert.IsTrue(resolved);
        Assert.AreEqual("#74a6ff", color);
    }

    [TestMethod]
    public void BuildBbCode_UsesCustomModFormatBeforeTags()
    {
        var settings = new ModNameStyleSettings
        {
            ModFormats = new Dictionary<string, string>
            {
                ["Favorite.Mod"] = "[rainbow freq=0.8 sat=0.9 val=1.0]{name}[/rainbow]"
            }
        };

        string bbCode = ModNameStyleRules.BuildBbCode(
            "Favorite.Mod",
            "Favorite [Mod]",
            new[] { "QoL" },
            settings);

        Assert.AreEqual("[rainbow freq=0.8 sat=0.9 val=1.0]Favorite [lb]Mod[rb][/rainbow]", bbCode);
    }

    [TestMethod]
    public void RequiresWorkshopTags_ReturnsFalseWhenOnlyModFormatsAreEnabled()
    {
        var settings = new ModNameStyleSettings
        {
            UseDefaultTagFormats = false,
            ModFormats = new Dictionary<string, string>
            {
                ["Favorite.Mod"] = "#ff77cc"
            }
        };

        Assert.IsFalse(ModNameStyleRules.RequiresWorkshopTags(settings));
    }

    [TestMethod]
    public void ParseTagsByPublishedFileId_ReadsSteamTags()
    {
        string json = """
        {
          "response": {
            "publishedfiledetails": [
              {
                "publishedfileid": "3748029698",
                "tags": [
                  { "tag": "QoL" },
                  { "tag": "Tools & APIs" }
                ]
              }
            ]
          }
        }
        """;

        var tagsByFileId = SteamWorkshopTagService.ParseTagsByPublishedFileId(json);

        CollectionAssert.AreEqual(new[] { "QoL", "Tools & APIs" }, tagsByFileId["3748029698"]);
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
    public void GetPreferredLogDialogLayout_ReservesVisibleActionRowOutsideScroll()
    {
        LogDialogLayout layout = ModdingScreenDialogRules.GetPreferredLogDialogLayout();

        Assert.IsTrue(layout.ActionRowHeight >= 40);
        Assert.IsTrue(layout.ToolbarGap >= 6);
        Assert.IsTrue(layout.ScrollHeight < layout.PanelHeight);
        Assert.IsTrue(layout.ScrollHeight + layout.ActionRowHeight > layout.PanelHeight);
        Assert.IsTrue(layout.PanelHeight + layout.ActionRowHeight + layout.ToolbarGap < layout.PopupHeight);
        Assert.IsTrue(layout.PopupHeight > layout.PanelHeight);
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
