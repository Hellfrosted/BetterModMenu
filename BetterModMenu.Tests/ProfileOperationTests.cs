using BetterModMenu.Data;
using BetterModMenu.Patches;
using MegaCrit.Sts2.Core.Saves;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BetterModMenu.Tests;

[TestClass]
public class ProfileOperationTests
{
    [TestInitialize]
    public void SetUp()
    {
        ProfileManager.Profiles =
        [
            new() { Name = "First", DisabledMods = ["a"] },
            new() { Name = "Middle", DisabledMods = ["b"] },
            new() { Name = "Last", DisabledMods = ["a", "b"] }
        ];
        ProfileManager.FailSavesRemaining = 0;
        SaveManager.Instance = new SaveManager
        {
            SettingsSave = new FakeSettingsSave
            {
                ModSettings = new FakeModSettings
                {
                    ModList = [new() { Id = "a", IsEnabled = true }, new() { Id = "b", IsEnabled = false }]
                }
            }
        };
    }

    [TestMethod]
    [DataRow(0, "Middle")]
    [DataRow(1, "Last")]
    [DataRow(2, "Middle")]
    public void DeleteSelectsAndPersistsReplacement(int index, string replacement)
    {
        ProfileManager.CurrentProfileIndex = index;
        Assert.IsTrue(ModdingScreenProfileOps.TryDeleteCurrentProfile(out var selected));
        Assert.AreEqual(replacement, selected!.Name);
        Assert.AreEqual(replacement, ProfileManager.CurrentProfile.Name);
        CollectionAssert.AreEqual(ProfileManager.Profiles.Select(profile => profile.Name).ToArray(), ProfileManager.SavedProfileNames.ToArray());
        foreach (var mod in SaveManager.Instance!.SettingsSave!.ModSettings!.ModList)
            Assert.AreEqual(!selected.DisabledMods.Contains(mod.Id), mod.IsEnabled);
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void DeleteRestoresProfilesAndModStatesWhenPersistenceFails(bool failGameSettings)
    {
        ProfileManager.CurrentProfileIndex = 1;
        if (failGameSettings)
            SaveManager.Instance!.FailSavesRemaining = 1;
        else
            ProfileManager.FailSavesRemaining = 1;

        Assert.IsFalse(ModdingScreenProfileOps.TryDeleteCurrentProfile(out _));
        CollectionAssert.AreEqual(new[] { "First", "Middle", "Last" }, ProfileManager.Profiles.Select(profile => profile.Name).ToArray());
        CollectionAssert.AreEqual(new[] { "First", "Middle", "Last" }, ProfileManager.SavedProfileNames.ToArray());
        Assert.AreEqual(1, ProfileManager.CurrentProfileIndex);
        Assert.IsTrue(ProfileManager.CurrentProfile.DisabledMods.SetEquals(["b"]));
        CollectionAssert.AreEqual(new[] { true, false }, SaveManager.Instance!.SettingsSave!.ModSettings!.ModList.Select(mod => mod.IsEnabled).ToArray());
        CollectionAssert.AreEqual(new[] { true, false }, SaveManager.Instance.SavedEnabledStates.ToArray());
    }

    [TestMethod]
    public void SelectionSnapshotsCurrentModsBeforeSwitching()
    {
        ProfileManager.CurrentProfileIndex = 0;
        Assert.IsTrue(ModdingScreenProfileOps.TryApplyProfileSelection(1, true, out var selected));
        Assert.IsTrue(ProfileManager.Profiles[0].DisabledMods.SetEquals(["b"]));
        Assert.AreEqual("Middle", selected!.Name);
        CollectionAssert.AreEqual(new[] { true, false }, SaveManager.Instance!.SavedEnabledStates.ToArray());
    }

    [TestMethod]
    public void DeleteKeepsLastProfile()
    {
        ProfileManager.Profiles = [new ModProfile { Name = "Only" }];
        ProfileManager.CurrentProfileIndex = 0;
        Assert.IsFalse(ModdingScreenProfileOps.TryDeleteCurrentProfile(out _));
        Assert.AreEqual("Only", ProfileManager.CurrentProfile.Name);
    }
}
