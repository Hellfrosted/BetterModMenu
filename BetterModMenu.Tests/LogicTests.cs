using BetterModMenu.Data;
using BetterModMenu.Patches;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;
using System.Reflection;
using System.Text;
using System.Text.Json;

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
    public void Search_ProvidesLocalizationKeyForMatchReason()
    {
        var documents = new[]
        {
            new ModSearchDocument("Example.Mod", "Example Mod") { Author = "Tester" }
        };

        var results = ModSearchRules.Search(documents, "Tester");

        Assert.AreEqual(BmmText.SearchMatchAuthor, results[0].MatchReasonKey);
        Assert.AreEqual("Matched author", results[0].MatchReason);
    }

    [TestMethod]
    public void Search_MatchesPersonalAliasAndNotes()
    {
        var documents = new[]
        {
            new ModSearchDocument("Example.Mod", "Example Mod")
            {
                Alias = "Daily run helper",
                Notes = "Disable before multiplayer"
            }
        };

        var aliasResults = ModSearchRules.Search(documents, "Daily run helper");
        var noteResults = ModSearchRules.Search(documents, "multiplayer");

        Assert.AreEqual(BmmText.SearchMatchAlias, aliasResults[0].MatchReasonKey);
        Assert.AreEqual(BmmText.SearchMatchNotes, noteResults[0].MatchReasonKey);
    }

    [TestMethod]
    public void ModAnnotations_NormalizeWhitespaceAndDiscardEmptyEntries()
    {
        var annotations = ModAnnotationRules.NormalizeDictionary(new Dictionary<string, ModAnnotation>
        {
            [" Example.Mod "] = new() { Alias = "  Helper  ", Notes = "line one\r\nline two  " },
            ["Empty.Mod"] = new()
        });

        Assert.AreEqual(1, annotations.Count);
        Assert.AreEqual("Helper", annotations["Example.Mod"].Alias);
        Assert.AreEqual("line one\nline two", annotations["Example.Mod"].Notes);
    }

    [TestMethod]
    public void ModAnnotations_LoadNormalizationPreservesFutureOrManuallyEditedLengths()
    {
        string longNotes = new('x', ModAnnotationRules.MaxNotesLength + 1);
        var annotations = ModAnnotationRules.NormalizeDictionary(new Dictionary<string, ModAnnotation>
        {
            ["Example.Mod"] = new() { Notes = longNotes }
        });

        Assert.AreEqual(longNotes, annotations["Example.Mod"].Notes);
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
    public void GetTopBarPresentation_UsesIconOnlyActions()
    {
        var compact = ModdingScreenLayoutRules.GetTopBarPresentation(isCompact: true);

        Assert.AreEqual(string.Empty, compact.NewProfile.Text);
        Assert.AreEqual(string.Empty, compact.RenameProfile.Text);
        Assert.AreEqual(string.Empty, compact.DeleteProfile.Text);
        Assert.AreEqual(ModdingScreenConstants.TopBarButtonCompactWidth, compact.ButtonWidth);
        Assert.AreEqual(BmmText.NewProfileTooltip, compact.NewProfile.TooltipKey);
        Assert.AreEqual(BmmText.RenameProfileTooltip, compact.RenameProfile.TooltipKey);
        Assert.AreEqual(BmmText.DeleteProfileTooltip, compact.DeleteProfile.TooltipKey);

        var wide = ModdingScreenLayoutRules.GetTopBarPresentation(isCompact: false);

        Assert.AreEqual(string.Empty, wide.NewProfile.Text);
        Assert.AreEqual(string.Empty, wide.RenameProfile.Text);
        Assert.AreEqual(string.Empty, wide.DeleteProfile.Text);
        Assert.AreEqual(ModdingScreenConstants.TopBarButtonCompactWidth, wide.ButtonWidth);
    }

    [TestMethod]
    public void ShouldStackTopBar_UsesAvailableLocalizedTitleSpace()
    {
        Assert.IsTrue(ModdingScreenLayoutRules.ShouldStackTopBar(0f, 275f));
        Assert.IsTrue(ModdingScreenLayoutRules.ShouldStackTopBar(274f, 275f));
        Assert.IsFalse(ModdingScreenLayoutRules.ShouldStackTopBar(275f, 275f));
    }

    [TestMethod]
    public void ShouldShowRowMoveButtons_PreservesGroupPickerOnNarrowRows()
    {
        const float compactControlsWidth = 184f;
        const float exactThreshold =
            compactControlsWidth +
            ModdingScreenConstants.RowControlsRightPadding +
            ModdingScreenConstants.RowNativeTickboxReserveWidth +
            ModdingScreenConstants.RowMinimumCompactLeftContentWidth;

        Assert.IsFalse(ModdingScreenLayoutRules.ShouldShowRowMoveButtons(rowWidth: 270f, compactControlsWidth));
        Assert.IsFalse(ModdingScreenLayoutRules.ShouldShowRowMoveButtons(rowWidth: exactThreshold - 1f, compactControlsWidth));
        Assert.IsTrue(ModdingScreenLayoutRules.ShouldShowRowMoveButtons(rowWidth: exactThreshold, compactControlsWidth));
        Assert.IsTrue(ModdingScreenLayoutRules.ShouldShowRowMoveButtons(rowWidth: 0f, compactControlsWidth));
    }

    [TestMethod]
    public void IntersectVisibleRowSpan_UsesActualClippingIntersection()
    {
        var aligned = ModdingScreenLayoutRules.IntersectVisibleRowSpan(
            new VisibleRowSpan(0f, 900f),
            rowGlobalLeft: 200f,
            clipGlobalLeft: 200f,
            clipWidth: 300f);
        var inset = ModdingScreenLayoutRules.IntersectVisibleRowSpan(
            new VisibleRowSpan(0f, 900f),
            rowGlobalLeft: 200f,
            clipGlobalLeft: 250f,
            clipWidth: 300f);
        var zero = ModdingScreenLayoutRules.IntersectVisibleRowSpan(
            new VisibleRowSpan(0f, 900f),
            rowGlobalLeft: 200f,
            clipGlobalLeft: 200f,
            clipWidth: 0f);
        var nested = ModdingScreenLayoutRules.IntersectVisibleRowSpan(
            inset,
            rowGlobalLeft: 200f,
            clipGlobalLeft: 300f,
            clipWidth: 100f);

        Assert.AreEqual(new VisibleRowSpan(0f, 300f), aligned);
        Assert.AreEqual(new VisibleRowSpan(50f, 350f), inset);
        Assert.AreEqual(0f, zero.Width);
        Assert.AreEqual(new VisibleRowSpan(100f, 200f), nested);
    }

    [TestMethod]
    public void Localization_UsesSts2LanguageCodesPlusVietnamese()
    {
        CollectionAssert.AreEqual(
            new[]
            {
                "eng",
                "fra",
                "ita",
                "deu",
                "esp",
                "jpn",
                "kor",
                "pol",
                "ptb",
                "rus",
                "zhs",
                "spa",
                "tha",
                "tur",
                "vie"
            },
            BmmLocalization.SupportedLanguageCodes.ToList());
    }

    [TestMethod]
    public void Localization_NormalizesGameAndLocaleCodes()
    {
        var cases = new Dictionary<string, string[]>
        {
            ["eng"] = ["english", "en", "en-US", "en_GB", "eng"],
            ["fra"] = ["french", "fr", "fr-FR", "fra"],
            ["ita"] = ["italian", "it", "it-IT", "ita"],
            ["deu"] = ["german", "de", "de-DE", "deu"],
            ["esp"] = ["spanish", "spanish_spain", "es", "es-ES", "esp"],
            ["jpn"] = ["japanese", "ja", "ja-JP", "jpn"],
            ["kor"] = ["koreana", "korean", "ko", "ko-KR", "kor"],
            ["pol"] = ["polish", "pl", "pl-PL", "pol"],
            ["ptb"] = ["brazilian", "portuguese_brazil", "pt", "pt-BR", "ptb"],
            ["rus"] = ["russian", "ru", "ru-RU", "rus"],
            ["zhs"] = ["schinese", "simplified_chinese", "zh", "zh-CN", "zh-Hans", "zh-SG", "zhs"],
            ["spa"] = ["latam", "spanish_latin_america", "es-419", "es-MX", "es-AR", "es-CL", "es-CO", "spa"],
            ["tha"] = ["thai", "th", "th-TH", "tha"],
            ["tur"] = ["turkish", "tr", "tr-TR", "tur"],
            ["vie"] = ["vietnamese", "vi", "vi-VN", "vie"]
        };

        foreach (var entry in cases)
        {
            foreach (string input in entry.Value)
                Assert.AreEqual(entry.Key, BmmLocalization.NormalizeLanguageCode(input), input);
        }
    }

    [TestMethod]
    public void Localization_FilesCoverEveryEnglishKey()
    {
        string localizationDirectory = Path.Combine(GetRepoRoot(), "Localization");
        var catalogs = BmmLocalization.SupportedLanguageCodes.ToDictionary(
            code => code,
            code => BmmLocalization.LoadJsonFile(Path.Combine(localizationDirectory, code + ".json")),
            StringComparer.Ordinal);

        var errors = BmmLocalization.FindCoverageErrors(catalogs);

        Assert.AreEqual(string.Empty, string.Join(Environment.NewLine, errors));
    }

    [TestMethod]
    public void Localization_EnglishFileCoversEveryBmmTextKey()
    {
        string localizationDirectory = Path.Combine(GetRepoRoot(), "Localization");
        var englishCatalog = BmmLocalization.LoadJsonFile(Path.Combine(localizationDirectory, BmmLocalization.EnglishLanguageCode + ".json"));
        var bmmTextKeys = typeof(BmmText)
            .GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy)
            .Where(field => field.IsLiteral && !field.IsInitOnly && field.FieldType == typeof(string))
            .Select(field => (string)field.GetRawConstantValue()!)
            .OrderBy(key => key, StringComparer.Ordinal)
            .ToList();
        var missing = bmmTextKeys
            .Where(key => !englishCatalog.ContainsKey(key))
            .ToList();

        Assert.AreEqual(string.Empty, string.Join(Environment.NewLine, missing));
    }

    [TestMethod]
    public void Localization_UnsupportedLanguageFallsBackToEnglishThenCallerFallback()
    {
        string localizationDirectory = Path.Combine(GetRepoRoot(), "Localization");
        var englishCatalog = BmmLocalization.LoadJsonFile(Path.Combine(localizationDirectory, BmmLocalization.EnglishLanguageCode + ".json"));
        var catalog = new BmmLocalizationCatalog(
            BmmLocalization.NormalizeLanguageCode("nl-NL"),
            englishCatalog,
            new Dictionary<string, string>(StringComparer.Ordinal));

        Assert.AreEqual("nl_nl", catalog.Language);
        Assert.AreEqual("Backup", catalog.Get(BmmText.Backup, "fallback"));
        Assert.AreEqual("fallback", catalog.Get("MISSING.KEY", "fallback"));
    }

    [TestMethod]
    public void Localization_FilesDoNotContainDuplicateKeys()
    {
        string localizationDirectory = Path.Combine(GetRepoRoot(), "Localization");
        var errors = new List<string>();

        foreach (string code in BmmLocalization.SupportedLanguageCodes)
        {
            string path = Path.Combine(localizationDirectory, code + ".json");
            var duplicates = ReadDuplicateJsonPropertyNames(path);

            if (duplicates.Count > 0)
                errors.Add(code + " has duplicate localization keys: " + string.Join(", ", duplicates));
        }

        Assert.AreEqual(string.Empty, string.Join(Environment.NewLine, errors));
    }

    [TestMethod]
    public void Localization_FilesUseEnglishKeyOrder()
    {
        string localizationDirectory = Path.Combine(GetRepoRoot(), "Localization");
        var englishKeys = ReadJsonPropertyNames(Path.Combine(localizationDirectory, BmmLocalization.EnglishLanguageCode + ".json"));
        var errors = new List<string>();

        foreach (string code in BmmLocalization.SupportedLanguageCodes.Where(code => code != BmmLocalization.EnglishLanguageCode))
        {
            var keys = ReadJsonPropertyNames(Path.Combine(localizationDirectory, code + ".json"));
            if (!englishKeys.SequenceEqual(keys))
                errors.Add(code + " localization keys are not in English source order.");
        }

        Assert.AreEqual(string.Empty, string.Join(Environment.NewLine, errors));
    }

    [TestMethod]
    public void Localization_TargetFilesAreNotEnglishPlaceholders()
    {
        string localizationDirectory = Path.Combine(GetRepoRoot(), "Localization");
        var englishCatalog = BmmLocalization.LoadJsonFile(Path.Combine(localizationDirectory, BmmLocalization.EnglishLanguageCode + ".json"));
        int maxAllowedEnglishCopies = englishCatalog.Count / 4;
        var errors = new List<string>();

        foreach (string code in BmmLocalization.SupportedLanguageCodes.Where(code => code != BmmLocalization.EnglishLanguageCode))
        {
            var catalog = BmmLocalization.LoadJsonFile(Path.Combine(localizationDirectory, code + ".json"));
            int copiedValueCount = catalog.Count(entry =>
                englishCatalog.TryGetValue(entry.Key, out string? englishValue) &&
                string.Equals(entry.Value, englishValue, StringComparison.Ordinal));

            if (copiedValueCount > maxAllowedEnglishCopies)
            {
                errors.Add(
                    code + " has " + copiedValueCount + " values identical to English; " +
                    "this looks like an English placeholder catalog.");
            }
        }

        Assert.AreEqual(string.Empty, string.Join(Environment.NewLine, errors));
    }

    [TestMethod]
    public void Localization_FormatStringsAcceptEnglishPlaceholderShape()
    {
        string localizationDirectory = Path.Combine(GetRepoRoot(), "Localization");
        var catalogs = BmmLocalization.SupportedLanguageCodes.ToDictionary(
            code => code,
            code => BmmLocalization.LoadJsonFile(Path.Combine(localizationDirectory, code + ".json")),
            StringComparer.Ordinal);
        var errors = new List<string>();

        foreach (var key in catalogs[BmmLocalization.EnglishLanguageCode].Keys)
        {
            int argumentCount = CountFormatArguments(catalogs[BmmLocalization.EnglishLanguageCode][key]);
            if (argumentCount == 0)
                continue;

            object[] args = Enumerable.Range(0, argumentCount).Select(index => (object)("value" + index)).ToArray();
            foreach (var catalog in catalogs)
            {
                try
                {
                    _ = string.Format(catalog.Value[key], args);
                }
                catch (FormatException ex)
                {
                    errors.Add(catalog.Key + " has invalid format string for " + key + ": " + ex.Message);
                }
            }
        }

        Assert.AreEqual(string.Empty, string.Join(Environment.NewLine, errors));
    }

    [TestMethod]
    public void DetailActionPanelGeometry_ReservesOnlyTheVisibleActionArea()
    {
        float expectedContentHeight =
            ModdingScreenConstants.DetailStatusLineHeight +
            ModdingScreenConstants.DetailConfigButtonHeight +
            ModdingScreenConstants.DetailActionGap;
        float expectedPanelDistanceFromBottom =
            expectedContentHeight + ModdingScreenConstants.DetailActionBottomInset;
        float expectedDescriptionInset =
            expectedPanelDistanceFromBottom + ModdingScreenConstants.DetailDescriptionActionGap;

        Assert.AreEqual(expectedContentHeight, ModdingScreenConstants.DetailActionContentHeight);
        Assert.AreEqual(expectedPanelDistanceFromBottom, ModdingScreenConstants.DetailActionPanelHeight);
        Assert.AreEqual(expectedDescriptionInset, ModdingScreenConstants.DetailDescriptionBottomInset);
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
                CollapsedGroups = new HashSet<string> { "Bosses" },
                ModAnnotations = new Dictionary<string, ModAnnotation>
                {
                    ["mod-a"] = new() { Alias = "My boss mod", Notes = "Load after BaseLib" }
                }
            };

            bool wrote = ProfileSaveStorage.TryWrite(savePath, saveData, _ => { }, out string? error);
            var loaded = ProfileSaveStorage.LoadOrDefault(savePath, _ => { });

            Assert.IsTrue(wrote, error);
            Assert.AreEqual("Bosses", loaded.Profiles[0].Name);
            Assert.IsTrue(loaded.Profiles[0].DisabledMods.Contains("mod-a"));
            Assert.AreEqual("Bosses", loaded.CustomGroups[0]);
            Assert.AreEqual("Bosses", loaded.ModGroups["mod-a"]);
            Assert.IsTrue(loaded.CollapsedGroups.Contains("Bosses"));
            Assert.AreEqual("My boss mod", loaded.ModAnnotations["mod-a"].Alias);
            Assert.AreEqual("Load after BaseLib", loaded.ModAnnotations["mod-a"].Notes);
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
            string collisionBackup = Path.Combine(backupDirectory, "mod_profiles.20260606-210000.manual-2.json");
            string settingsBackup = Path.Combine(backupDirectory, "mod_settings.20260607-210000.manual.json");
            File.WriteAllText(oldBackup, "{ \"Profiles\": [{ \"Name\": \"Old\" }] }");
            File.WriteAllText(newBackup, "{ \"Profiles\": [{ \"Name\": \"New\" }] }");
            File.WriteAllText(collisionBackup, "{ \"Profiles\": [{ \"Name\": \"Collision\" }] }");
            File.WriteAllText(settingsBackup, "[]");
            File.SetLastWriteTimeUtc(oldBackup, new DateTime(2026, 6, 5, 21, 0, 0, DateTimeKind.Utc));
            File.SetLastWriteTimeUtc(newBackup, new DateTime(2026, 6, 6, 21, 0, 0, DateTimeKind.Utc));
            File.SetLastWriteTimeUtc(collisionBackup, new DateTime(2026, 6, 6, 22, 0, 0, DateTimeKind.Utc));
            File.SetLastWriteTimeUtc(settingsBackup, new DateTime(2026, 6, 7, 21, 0, 0, DateTimeKind.Utc));

            bool found = ProfileBackupService.TryListBackups(savePath, new[] { ".json", ".json5", ".jsonc" }, out var backups, out string? error);

            Assert.IsTrue(found, error);
            Assert.AreEqual(3, backups.Count);
            Assert.AreEqual(collisionBackup, backups[0].Path);
            Assert.AreEqual(newBackup, backups[1].Path);
            Assert.AreEqual(oldBackup, backups[2].Path);
            StringAssert.Contains(backups[0].Label, "2026-06-06");
            StringAssert.Contains(backups[0].Label, "Manual backup");
            Assert.AreEqual(ProfileBackupReason.Manual, backups[0].Reason);
        }
        finally
        {
            Directory.Delete(tempDirectory, true);
        }
    }

    [TestMethod]
    public void TryPruneAutomaticProfileBackups_KeepsNewestTwelveAndPreservesOtherFiles()
    {
        string tempDirectory = CreateTempDirectory();
        try
        {
            string savePath = Path.Combine(tempDirectory, "mod_profiles.json5");
            File.WriteAllText(savePath, "{ \"Profiles\": [] }");
            var automaticBackups = new List<string>();

            for (int i = 0; i < 14; i++)
            {
                var timestamp = new DateTimeOffset(2026, 6, 1, 0, i, 0, TimeSpan.Zero);
                var reason = i % 2 == 0 ? ProfileBackupReason.RunStart : ProfileBackupReason.Resume;
                Assert.IsTrue(ProfileBackupService.TryBackupExistingSave(savePath, reason, timestamp, out string backupPath, out string? backupError), backupError);
                File.SetLastWriteTimeUtc(backupPath, timestamp.UtcDateTime);
                automaticBackups.Add(backupPath);
            }

            var manualBackups = new List<string>();
            var manualTimestamp = new DateTimeOffset(2026, 6, 2, 0, 0, 0, TimeSpan.Zero);
            for (int i = 0; i < 14; i++)
            {
                Assert.IsTrue(ProfileBackupService.TryBackupExistingSave(savePath, ProfileBackupReason.Manual, manualTimestamp, out string manualBackup, out string? manualError), manualError);
                manualBackups.Add(manualBackup);
            }
            string backupDirectory = Path.Combine(tempDirectory, "backups");
            string unknownBackup = Path.Combine(backupDirectory, "mod_profiles.not-a-timestamp.runstart.json5");
            string unrelatedBackup = Path.Combine(backupDirectory, "other_profiles.20260602-020000.runstart.json5");
            string settingsBackup = Path.Combine(backupDirectory, "mod_settings.20260602-030000.runstart.json");
            File.WriteAllText(unknownBackup, "{}");
            File.WriteAllText(unrelatedBackup, "{}");
            File.WriteAllText(settingsBackup, "[]");

            bool pruned = ProfileBackupService.TryPruneAutomaticBackups(
                savePath,
                new[] { ".json", ".json5", ".jsonc" },
                12,
                out string? error);

            Assert.IsTrue(pruned, error);
            Assert.IsFalse(File.Exists(automaticBackups[0]));
            Assert.IsFalse(File.Exists(automaticBackups[1]));
            Assert.IsTrue(automaticBackups.Skip(2).All(File.Exists));
            Assert.IsTrue(manualBackups.All(File.Exists));
            Assert.IsTrue(File.Exists(unknownBackup));
            Assert.IsTrue(File.Exists(unrelatedBackup));
            Assert.IsTrue(File.Exists(settingsBackup));
        }
        finally
        {
            Directory.Delete(tempDirectory, true);
        }
    }

    [TestMethod]
    public void TryPruneAutomaticModSettingsBackups_KeepsNewestTwelveAndPreservesOtherFiles()
    {
        string tempDirectory = CreateTempDirectory();
        try
        {
            var mods = new[] { new InstalledModExportInput { ModId = "Example.Mod", Enabled = true } };
            var automaticBackups = new List<string>();

            for (int i = 0; i < 14; i++)
            {
                var timestamp = new DateTimeOffset(2026, 6, 1, 0, i, 0, TimeSpan.Zero);
                var reason = i % 2 == 0 ? ProfileBackupReason.RunStart : ProfileBackupReason.Resume;
                Assert.IsTrue(ModSettingsBackupService.TryWriteSnapshot(tempDirectory, mods, reason, timestamp, out string backupPath, out string? backupError), backupError);
                File.SetLastWriteTimeUtc(backupPath, timestamp.UtcDateTime);
                automaticBackups.Add(backupPath);
            }

            var manualBackups = new List<string>();
            var manualTimestamp = new DateTimeOffset(2026, 6, 2, 0, 0, 0, TimeSpan.Zero);
            for (int i = 0; i < 14; i++)
            {
                Assert.IsTrue(ModSettingsBackupService.TryWriteSnapshot(tempDirectory, mods, ProfileBackupReason.Manual, manualTimestamp, out string manualBackup, out string? manualError), manualError);
                manualBackups.Add(manualBackup);
            }
            string unknownBackup = Path.Combine(tempDirectory, "mod_settings.not-a-timestamp.runstart.json");
            string unrelatedBackup = Path.Combine(tempDirectory, "other_settings.20260602-020000.runstart.json");
            string profileBackup = Path.Combine(tempDirectory, "mod_profiles.20260602-030000.runstart.json5");
            File.WriteAllText(unknownBackup, "[]");
            File.WriteAllText(unrelatedBackup, "[]");
            File.WriteAllText(profileBackup, "{}");

            bool pruned = ModSettingsBackupService.TryPruneAutomaticBackups(tempDirectory, 12, out string? error);

            Assert.IsTrue(pruned, error);
            Assert.IsFalse(File.Exists(automaticBackups[0]));
            Assert.IsFalse(File.Exists(automaticBackups[1]));
            Assert.IsTrue(automaticBackups.Skip(2).All(File.Exists));
            Assert.IsTrue(manualBackups.All(File.Exists));
            Assert.IsTrue(File.Exists(unknownBackup));
            Assert.IsTrue(File.Exists(unrelatedBackup));
            Assert.IsTrue(File.Exists(profileBackup));
        }
        finally
        {
            Directory.Delete(tempDirectory, true);
        }
    }

    [TestMethod]
    public void BackupSelectionPages_BoundEachDropdownPageAndReachEveryBackup()
    {
        BackupSelectionPage firstPage = ModdingScreenDialogRules.GetBackupSelectionPage(25, 0, 12);
        BackupSelectionPage secondPage = ModdingScreenDialogRules.GetBackupSelectionPage(25, 1, 12);
        BackupSelectionPage lastPage = ModdingScreenDialogRules.GetBackupSelectionPage(25, 99, 12);
        BackupSelectionPage clampedFirstPage = ModdingScreenDialogRules.GetBackupSelectionPage(25, -1, 12);

        Assert.AreEqual(new BackupSelectionPage(0, 12, 0, 3), firstPage);
        Assert.AreEqual(new BackupSelectionPage(12, 12, 1, 3), secondPage);
        Assert.AreEqual(new BackupSelectionPage(24, 1, 2, 3), lastPage);
        Assert.AreEqual(firstPage, clampedFirstPage);

        var backupPaths = Enumerable.Range(0, 25).Select(index => $"backup-{index}").ToList();
        Assert.AreEqual("backup-24", backupPaths[lastPage.StartIndex]);
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
            "Mod Id,Name,Version,Enabled,Group,Workshop Link,Alias,Notes",
            "Example.Mod,\"Example, Mod\",1.2.3,TRUE,\"QoL \"\"Core\"\"\",,,",
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
                "Unassigned",
                new Dictionary<string, ModAnnotation>
                {
                    ["Example.Mod"] = new() { Alias = "My helper", Notes = "Keep enabled" }
                });

            Assert.AreEqual(1, rows.Count);
            Assert.AreEqual("Example.Mod", rows[0].ModId);
            Assert.AreEqual("Example Mod", rows[0].Name);
            Assert.AreEqual("1.2.3", rows[0].Version);
            Assert.IsFalse(rows[0].Enabled);
            Assert.AreEqual("Core", rows[0].Group);
            Assert.AreEqual("https://steamcommunity.com/sharedfiles/filedetails/?id=3456789012", rows[0].WorkshopUrl);
            Assert.AreEqual("My helper", rows[0].Alias);
            Assert.AreEqual("Keep enabled", rows[0].Notes);
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
            string logPath = Path.Combine(tempDirectory, "BetterModMenu.log");
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
    public void TryReadTail_ReturnsTailWithoutScanningEarlierContent()
    {
        string tempDirectory = CreateTempDirectory();
        try
        {
            string logPath = Path.Combine(tempDirectory, "BetterModMenu.log");
            File.WriteAllLines(logPath, Enumerable.Range(1, 6000).Select(number => "line " + number));

            bool read = LogViewerService.TryReadTail(logPath, maxLines: 5000, maxChars: 500000, out string content, out string? error);

            Assert.IsTrue(read, error);
            Assert.IsFalse(content.Contains("line 1" + Environment.NewLine, StringComparison.Ordinal));
            StringAssert.Contains(content, "line 1001");
            StringAssert.Contains(content, "line 6000");
        }
        finally
        {
            Directory.Delete(tempDirectory, true);
        }
    }

    [TestMethod]
    public void TryReadTail_LongSingleLineReturnsBoundedTail()
    {
        string tempDirectory = CreateTempDirectory();
        try
        {
            string logPath = Path.Combine(tempDirectory, "BetterModMenu.log");
            File.WriteAllText(logPath, new string('a', 100_000) + new string('b', 12_000));

            bool read = LogViewerService.TryReadTail(logPath, maxLines: 1, maxChars: 10000, out string content, out string? error);

            Assert.IsTrue(read, error);
            Assert.AreEqual(10000, content.Length);
            Assert.AreEqual(new string('b', 10000), content);
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
            string logPath = Path.Combine(tempDirectory, "BetterModMenu.log");
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
            string logPath = Path.Combine(tempDirectory, "BetterModMenu.log");
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
            string logPath = Path.Combine(tempDirectory, "logs", "BetterModMenu.log");
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
    public void TrySetTagColor_CanonicalizesTagAndNormalizesHexColor()
    {
        var settings = new ModNameStyleSettings
        {
            DisabledTags = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "QoL"
            }
        };

        bool set = ModNameStyleEditorRules.TrySetTagColor(
            settings,
            " Quality of Life ",
            " ABCDEF ",
            out string supportedTag,
            out string normalizedColor);

        Assert.IsTrue(set);
        Assert.AreEqual("QoL", supportedTag);
        Assert.AreEqual("#abcdef", normalizedColor);
        Assert.AreEqual("#abcdef", settings.TagFormats["QoL"]);
        Assert.IsFalse(settings.DisabledTags.Contains("QoL"));
    }

    [TestMethod]
    public void TryDisableAndResetTagColor_UseCanonicalWorkshopTags()
    {
        var settings = new ModNameStyleSettings();

        bool disabled = ModNameStyleEditorRules.TryDisableTagColor(settings, "Tools and APIs", out string disabledTag);
        string disabledPreview = ModNameStyleEditorRules.BuildPreviewBbCode(
            "BetterModMenu",
            "Better Mod Menu",
            new[] { "Tools & APIs" },
            settings);
        bool wasDisabled = settings.DisabledTags.Contains("Tools & APIs");

        bool reset = ModNameStyleEditorRules.TryResetTagColor(settings, "Tool", out string resetTag);
        string resetPreview = ModNameStyleEditorRules.BuildPreviewBbCode(
            "BetterModMenu",
            "Better Mod Menu",
            new[] { "Tools & APIs" },
            settings);

        Assert.IsTrue(disabled);
        Assert.AreEqual("Tools & APIs", disabledTag);
        Assert.IsTrue(wasDisabled);
        Assert.AreEqual("Better Mod Menu", disabledPreview);
        Assert.IsTrue(reset);
        Assert.AreEqual("Tools & APIs", resetTag);
        Assert.IsFalse(settings.DisabledTags.Contains("Tools & APIs"));
        Assert.AreEqual("[color=#74a6ff]Better Mod Menu[/color]", resetPreview);
    }

    [TestMethod]
    public void TrySetModColor_BuildPreviewUsesModOverrideBeforeTags()
    {
        var settings = new ModNameStyleSettings();

        Assert.IsTrue(ModNameStyleEditorRules.TrySetTagColor(settings, "QoL", "#00FF00", out _, out _));
        bool setModColor = ModNameStyleEditorRules.TrySetModColor(
            settings,
            " Favorite.Mod ",
            "FF77CC",
            out string normalizedColor);

        string overridePreview = ModNameStyleEditorRules.BuildPreviewBbCode(
            "Favorite.Mod",
            "Favorite Mod",
            new[] { "QoL" },
            settings);
        bool removed = ModNameStyleEditorRules.RemoveModColor(settings, "favorite.mod");
        string tagPreview = ModNameStyleEditorRules.BuildPreviewBbCode(
            "Favorite.Mod",
            "Favorite Mod",
            new[] { "QoL" },
            settings);

        Assert.IsTrue(setModColor);
        Assert.AreEqual("#ff77cc", normalizedColor);
        Assert.AreEqual("[color=#ff77cc]Favorite Mod[/color]", overridePreview);
        Assert.IsTrue(removed);
        Assert.AreEqual("[color=#00ff00]Favorite Mod[/color]", tagPreview);
    }

    [TestMethod]
    public void TrySetColor_InvalidColorDoesNotMutateSettings()
    {
        var settings = new ModNameStyleSettings
        {
            Enabled = false,
            UseDefaultTagFormats = false,
            TagFormats = new Dictionary<string, string>
            {
                ["QoL"] = "#123456"
            },
            TagPriority = new List<string> { "QoL" },
            DisabledTags = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "Misc"
            },
            ModFormats = new Dictionary<string, string>
            {
                ["Favorite.Mod"] = "#abcdef"
            }
        };
        var before = ModNameStyleEditorRules.Clone(settings);

        bool tagSet = ModNameStyleEditorRules.TrySetTagColor(settings, "QoL", "#12345g", out _, out _);
        bool modSet = ModNameStyleEditorRules.TrySetModColor(settings, "Favorite.Mod", "blue", out _);

        Assert.IsFalse(tagSet);
        Assert.IsFalse(modSet);
        AssertModNameStyleSettingsEqual(before, settings);
    }

    [TestMethod]
    public void TrySetTagColor_RejectsUnsupportedTagWithoutMutatingSettings()
    {
        var settings = new ModNameStyleSettings
        {
            TagFormats = new Dictionary<string, string>
            {
                ["QoL"] = "#123456"
            }
        };
        var before = ModNameStyleEditorRules.Clone(settings);

        bool set = ModNameStyleEditorRules.TrySetTagColor(settings, "Multiplayer", "#abcdef", out _, out _);

        Assert.IsFalse(set);
        AssertModNameStyleSettingsEqual(before, settings);
    }

    [TestMethod]
    public void ResetToDefaults_RestoresDefaultModNameStyleSettings()
    {
        var settings = new ModNameStyleSettings
        {
            Enabled = false,
            UseDefaultTagFormats = false,
            TagFormats = new Dictionary<string, string>
            {
                ["QoL"] = "#123456"
            },
            TagPriority = new List<string> { "QoL" },
            DisabledTags = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "Misc"
            },
            ModFormats = new Dictionary<string, string>
            {
                ["Favorite.Mod"] = "#abcdef"
            }
        };

        ModNameStyleEditorRules.ResetToDefaults(settings);

        Assert.IsTrue(settings.Enabled);
        Assert.IsTrue(settings.UseDefaultTagFormats);
        Assert.AreEqual(0, settings.TagFormats.Count);
        Assert.AreEqual(0, settings.TagPriority.Count);
        Assert.AreEqual(0, settings.DisabledTags.Count);
        Assert.AreEqual(0, settings.ModFormats.Count);
        Assert.AreEqual(
            "[color=#b3ed5e]Better Mod Menu[/color]",
            ModNameStyleEditorRules.BuildPreviewBbCode(
                "BetterModMenu",
                "Better Mod Menu",
                new[] { "QoL" },
                settings));
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

            CollectionAssert.Contains(paths.ToList(), Path.Combine(tempDirectory, "BetterModMenu.log"));
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
    public void GetPreferredStyleEditorDialogLayout_UsesDenseRowsAndFixedControls()
    {
        StyleEditorDialogLayout layout = ModdingScreenDialogRules.GetPreferredStyleEditorDialogLayout();

        Assert.AreEqual(64, layout.RowHeight);
        Assert.AreEqual(44, layout.SwatchSize);
        Assert.IsTrue(layout.SettingWidth >= 324);
        Assert.IsTrue(layout.LabelWidth + layout.SettingWidth < layout.PanelWidth);
        Assert.IsTrue(layout.ScrollHeight < layout.PopupHeight);
    }

    [TestMethod]
    public void FitStyleEditorDialogToViewport_KeepsEditorInsideInitial1080pWindow()
    {
        StyleEditorDialogLayout layout = ModdingScreenDialogRules.FitStyleEditorDialogToViewport(
            ModdingScreenDialogRules.GetPreferredStyleEditorDialogLayout(),
            viewportWidth: 1080,
            viewportHeight: 720);

        Assert.IsTrue(layout.PopupWidth <= 960);
        Assert.IsTrue(layout.PopupHeight <= 650);
        Assert.IsTrue(layout.PanelWidth < layout.PopupWidth);
        Assert.IsTrue(layout.ScrollHeight < layout.PopupHeight);
        Assert.IsTrue(layout.SettingWidth >= 420);
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

    private static int CountFormatArguments(string format)
    {
        int maxIndex = -1;
        for (int i = 0; i < format.Length; i++)
        {
            if (format[i] != '{')
                continue;

            if (i + 1 < format.Length && format[i + 1] == '{')
            {
                i++;
                continue;
            }

            int cursor = i + 1;
            int index = 0;
            bool hasDigit = false;
            while (cursor < format.Length && char.IsDigit(format[cursor]))
            {
                hasDigit = true;
                index = (index * 10) + (format[cursor] - '0');
                cursor++;
            }

            if (hasDigit)
                maxIndex = Math.Max(maxIndex, index);
        }

        return maxIndex + 1;
    }

    private static List<string> ReadJsonPropertyNames(string path)
    {
        var keys = new List<string>();
        var reader = new Utf8JsonReader(Encoding.UTF8.GetBytes(File.ReadAllText(path)));

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.PropertyName)
                keys.Add(reader.GetString() ?? string.Empty);
        }

        return keys;
    }

    private static List<string> ReadDuplicateJsonPropertyNames(string path)
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);
        var duplicates = new List<string>();

        foreach (string key in ReadJsonPropertyNames(path))
        {
            if (!seen.Add(key))
                duplicates.Add(key);
        }

        return duplicates;
    }

    private static void AssertModNameStyleSettingsEqual(ModNameStyleSettings expected, ModNameStyleSettings actual)
    {
        Assert.AreEqual(expected.Enabled, actual.Enabled);
        Assert.AreEqual(expected.UseDefaultTagFormats, actual.UseDefaultTagFormats);
        CollectionAssert.AreEqual(expected.TagFormats.ToList(), actual.TagFormats.ToList());
        CollectionAssert.AreEqual(expected.TagPriority, actual.TagPriority);
        CollectionAssert.AreEqual(expected.DisabledTags.ToList(), actual.DisabledTags.ToList());
        CollectionAssert.AreEqual(expected.ModFormats.ToList(), actual.ModFormats.ToList());
    }

}
