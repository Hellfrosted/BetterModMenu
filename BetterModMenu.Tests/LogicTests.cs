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
    public void BuildCsv_EscapesExcelFriendlyModExportRows()
    {
        string csv = ModListExportBuilder.BuildCsv(new[]
        {
            new ModListExportRow
            {
                ModId = "Example.Mod",
                Name = "Example, Mod",
                Version = "1.2.3",
                Link = "https://example.com/mod",
                Enabled = true,
                Group = "QoL \"Core\""
            }
        });

        string expected = string.Join(Environment.NewLine,
            "Mod Id,Name,Version,Link,Enabled,Group",
            "Example.Mod,\"Example, Mod\",1.2.3,https://example.com/mod,TRUE,\"QoL \"\"Core\"\"\"",
            string.Empty);

        Assert.AreEqual(expected, csv);
    }

    [TestMethod]
    public void TryReadManifestInfo_ReadsVersionAndBestAvailableLink()
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
              "website": "https://example.com/mod"
            }
            """);

            bool read = ManifestScanner.TryReadManifestInfo(manifestPath, "Example.Mod", out var info);

            Assert.IsTrue(read);
            Assert.AreEqual("Example.Mod", info.Id);
            Assert.AreEqual("Example Mod", info.Name);
            Assert.AreEqual("1.2.3", info.Version);
            Assert.AreEqual("https://example.com/mod", info.Link);
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
              "version": "1.2.3",
              "source": "https://example.com/source"
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
            Assert.AreEqual("https://example.com/source", rows[0].Link);
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
            Assert.IsTrue(File.ReadAllText(exportPath).StartsWith("Mod Id,Name,Version,Link,Enabled,Group", StringComparison.Ordinal));
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
        StringAssert.Contains(body, "Backup");
        StringAssert.Contains(body, "CSV");
        StringAssert.Contains(body, "Logs");
        StringAssert.Contains(body, "Game");
        StringAssert.Contains(body, "cloud behavior stays opt-in");
    }

    [TestMethod]
    public void TryReadVersion_ReturnsManifestVersionOnlyForExpectedId()
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

            Assert.IsTrue(ManifestScanner.TryReadVersion(manifestPath, "BetterModMenu", out string version));
            Assert.AreEqual("1.6.0", version);
            Assert.IsFalse(ManifestScanner.TryReadVersion(manifestPath, "OtherMod", out _));
        }
        finally
        {
            Directory.Delete(tempDirectory, true);
        }
    }

    [TestMethod]
    public void GetPackageBaseName_KeepsDefaultArtifactLocalOnlyAndAddsCloudSuffixOnlyWhenOptedIn()
    {
        Assert.AreEqual("BetterModMenu_v1.6.0", ReleasePackageRules.GetPackageBaseName("BetterModMenu", "1.6.0", includeCloudFeatures: false));
        Assert.AreEqual("BetterModMenu_v1.6.0_cloud", ReleasePackageRules.GetPackageBaseName("BetterModMenu", "1.6.0", includeCloudFeatures: true));
    }

    [TestMethod]
    public void GetCloudFeatureConstants_RequiresExplicitOptIn()
    {
        Assert.AreEqual(string.Empty, ReleasePackageRules.GetCloudFeatureConstants(includeCloudFeatures: false));
        Assert.AreEqual("BETTERMODMENU_CLOUD_FEATURES", ReleasePackageRules.GetCloudFeatureConstants(includeCloudFeatures: true));
    }

    [TestMethod]
    public void IsDefaultPackageFileName_ExcludesOptionalCloudSidecar()
    {
        Assert.IsTrue(ReleasePackageRules.IsDefaultPackageFileName("BetterModMenu_v1.6.0.zip"));
        Assert.IsFalse(ReleasePackageRules.IsDefaultPackageFileName("BetterModMenu_v1.6.0_cloud.zip"));
        Assert.IsTrue(ReleasePackageRules.IsCloudPackageFileName("BetterModMenu_v1.6.0_cloud.zip"));
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
    public void GetLogDialogLayout_UsesReadableSizeWithoutHorizontalOverflow()
    {
        LogDialogLayout layout = ModdingScreenDialogRules.GetLogDialogLayout();

        Assert.IsTrue(layout.PopupWidth >= 1000);
        Assert.IsTrue(layout.PopupHeight >= 640);
        Assert.IsTrue(layout.ContentWidth < layout.PopupWidth);
        Assert.IsTrue(layout.ContentHeight < layout.PopupHeight);
        Assert.IsTrue(layout.BodyFontSize >= 22);
        Assert.IsTrue(layout.ButtonFontSize >= 22);
    }

    [TestMethod]
    public void GetTutorialDialogLayout_UsesReadableTextSize()
    {
        TutorialDialogLayout layout = ModdingScreenDialogRules.GetTutorialDialogLayout();

        Assert.IsTrue(layout.PopupWidth >= 1000);
        Assert.IsTrue(layout.PopupHeight >= 700);
        Assert.IsTrue(layout.ContentWidth < layout.PopupWidth);
        Assert.IsTrue(layout.ContentHeight < layout.PopupHeight);
        Assert.IsTrue(layout.ContentHeight >= 560);
        Assert.IsTrue(layout.BodyFontSize >= 24);
        Assert.IsTrue(layout.ButtonFontSize >= 24);
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
    public void TrySelectVersion_SelectsValidatedSteamDbDerivedVersionByName()
    {
        var entries = new[]
        {
            new GameVersionEntry
            {
                DisplayName = "0.99.1",
                AppId = 2868840,
                DepotId = 2868841,
                ManifestId = 1234567890123456789,
                BuildId = "example"
            }
        };

        bool selected = GameVersionSelectionRules.TrySelectVersion(entries, "0.99.1", out var entry, out string? error);

        Assert.IsTrue(selected, error);
        Assert.AreEqual((uint)2868840, entry.AppId);
        Assert.AreEqual((uint)2868841, entry.DepotId);
        Assert.AreEqual((ulong)1234567890123456789, entry.ManifestId);
    }

    [TestMethod]
    public void TrySelectVersion_RejectsIncompleteSteamDbDerivedVersion()
    {
        var entries = new[]
        {
            new GameVersionEntry
            {
                DisplayName = "Broken",
                AppId = 2868840,
                DepotId = 0,
                ManifestId = 123
            }
        };

        bool selected = GameVersionSelectionRules.TrySelectVersion(entries, "Broken", out _, out string? error);

        Assert.IsFalse(selected);
        Assert.AreEqual("Steam depot id is required.", error);
    }

    [TestMethod]
    public void BuildSteamCmdDownloadDepotArguments_UsesAppDepotAndManifestIds()
    {
        var entry = new GameVersionEntry
        {
            DisplayName = "0.99.1",
            AppId = 2868840,
            DepotId = 2868841,
            ManifestId = 1234567890123456789
        };

        var args = GameVersionSelectionRules.BuildSteamCmdDownloadDepotArguments(entry, @"C:\Games\STS2-0.99.1");

        CollectionAssert.AreEqual(new[]
        {
            "+force_install_dir",
            @"C:\Games\STS2-0.99.1",
            "+login",
            "anonymous",
            "+download_depot",
            "2868840",
            "2868841",
            "1234567890123456789",
            "+quit"
        }, args.ToArray());
    }

    [TestMethod]
    public void TryBuildDownloadPlan_UsesSelectedVersionAndQuotesSteamCmdCommand()
    {
        var settings = new GameVersionDownloadSettings
        {
            Enabled = true,
            SteamCmdPath = @"C:\Program Files\steamcmd\steamcmd.exe",
            InstallRootDirectory = @"C:\Games\STS2 Versions",
            SelectedVersion = "0.99.1",
            Versions =
            [
                new GameVersionEntry
                {
                    DisplayName = "0.99.1",
                    AppId = 2868840,
                    DepotId = 2868841,
                    ManifestId = 1234567890123456789
                }
            ]
        };

        bool built = GameVersionSelectionRules.TryBuildDownloadPlan(settings, out var plan, out string? error);

        Assert.IsTrue(built, error);
        Assert.AreEqual(@"C:\Games\STS2 Versions\0.99.1", plan.InstallDirectory);
        Assert.AreEqual("\"C:\\Program Files\\steamcmd\\steamcmd.exe\" +force_install_dir \"C:\\Games\\STS2 Versions\\0.99.1\" +login anonymous +download_depot 2868840 2868841 1234567890123456789 +quit", plan.CommandLine);
    }

    [TestMethod]
    public void TryBuildDownloadPlan_RequiresExplicitEnablement()
    {
        var settings = new GameVersionDownloadSettings
        {
            Enabled = false,
            SteamCmdPath = "steamcmd",
            InstallRootDirectory = @"C:\Games",
            SelectedVersion = "0.99.1"
        };

        bool built = GameVersionSelectionRules.TryBuildDownloadPlan(settings, out _, out string? error);

        Assert.IsFalse(built);
        Assert.AreEqual("Game version downloads are not enabled.", error);
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
}
