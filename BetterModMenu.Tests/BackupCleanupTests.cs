using BetterModMenu.Data;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BetterModMenu.Tests;

[TestClass]
public class BackupCleanupTests
{
    [TestMethod]
    public void Cleanup_RetainsNewestOfEachKindEvenWhenAllAreOld()
    {
        string root = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        string directory = Path.Combine(root, "backups");
        Directory.CreateDirectory(directory);
        var now = new DateTimeOffset(2026, 11, 8, 12, 0, 0, TimeSpan.Zero);
        try
        {
            string[] names = ["20261104-120000.manual", "20261103-120000.manual", "20261102-120000.manual", "20261104-120000.resume", "20261103-120000.runstart"];
            for (int i = 0; i < names.Length; i++)
            {
                string path = Path.Combine(directory, "profiles." + names[i] + ".json");
                File.WriteAllText(path, "{}");
                File.SetLastWriteTimeUtc(path, now.UtcDateTime.AddDays(-(i is 0 or 3 ? 4 : i is 1 or 4 ? 5 : 6)));
            }
            Assert.IsTrue(ProfileBackupService.TryDeleteOldBackups(Path.Combine(root, "profiles.json"), [".json"], now, out int count, out string? error), error);
            Assert.AreEqual(2, count);
            CollectionAssert.AreEquivalent(new[] { "profiles." + names[0] + ".json", "profiles." + names[1] + ".json", "profiles." + names[3] + ".json" }, Directory.GetFiles(directory).Select(Path.GetFileName).ToArray());
        }
        finally { Directory.Delete(root, true); }
    }

    [TestMethod]
    public void Cleanup_DeletesOnlyOldRecognizedBackupsBeyondEachRetentionMinimum()
    {
        string root = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        string directory = Path.Combine(root, "backups");
        Directory.CreateDirectory(directory);
        var now = new DateTimeOffset(2026, 11, 8, 12, 0, 0, TimeSpan.Zero);
        try
        {
            string Add(string suffix, double days)
            {
                string path = Path.Combine(directory, "profiles." + suffix + ".json");
                File.WriteAllText(path, "{}");
                File.SetLastWriteTimeUtc(path, now.UtcDateTime.AddDays(-days));
                return path;
            }
            string manual1 = Add("20261107-120000.manual", 1);
            string manual2 = Add("20261103-120000.manual", 5);
            string manualOld = Add("20261102-120000.manual", 6);
            string auto1 = Add("20261104-120000.resume", 4);
            string autoOld = Add("20261103-120000.runstart", 5);
            string unknown = Add("legacy", 20);
            string fresh = Add("20261106-120000.runstart", 2);

            Assert.IsTrue(ProfileBackupService.TryDeleteOldBackups(Path.Combine(root, "profiles.json"), [".json"], now, out int count, out string? error), error);
            Assert.AreEqual(3, count);
            foreach (string path in new[] { manual1, manual2, fresh, unknown }) Assert.IsTrue(File.Exists(path), path);
            foreach (string path in new[] { manualOld, auto1, autoOld }) Assert.IsFalse(File.Exists(path), path);
        }
        finally { Directory.Delete(root, true); }
    }
}
