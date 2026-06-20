using BetterModMenu.Data;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;

namespace BetterModMenu.Tests;

[TestClass]
public class ModListExportSecurityTests
{
    [TestMethod]
    public void BuildCsv_NeutralizesFormulaPrefixes_when_ManifestAndGroupValuesStartWithFormulaTriggers()
    {
        string tempDirectory = CreateTempDirectory();
        try
        {
            string manifestPath = Path.Combine(tempDirectory, "Formula.Mod.json");
            File.WriteAllText(manifestPath, """
            {
              "id": "Formula.Mod",
              "name": "=HYPERLINK(\"https://example.invalid\")",
              "version": "+1.2.3"
            }
            """);

            var rows = ModListExportBuilder.BuildRows(
                new[]
                {
                    new InstalledModExportInput
                    {
                        ModId = "Formula.Mod",
                        Enabled = true,
                        ManifestPath = manifestPath
                    }
                },
                new Dictionary<string, string> { ["Formula.Mod"] = "-Injected Group" },
                "Unassigned");

            string csv = ModListExportBuilder.BuildCsv(rows);

            string expected = string.Join(Environment.NewLine,
                "Mod Id,Name,Version,Enabled,Group,Workshop Link",
                "Formula.Mod,\"'=HYPERLINK(\"\"https://example.invalid\"\")\",'+1.2.3,TRUE,'-Injected Group,",
                string.Empty);
            Assert.AreEqual(expected, csv);
        }
        finally
        {
            Directory.Delete(tempDirectory, true);
        }
    }

    [TestMethod]
    public void BuildCsv_NeutralizesFormulaPrefixes_when_RawExportRowsStartWithControlTriggers()
    {
        string csv = ModListExportBuilder.BuildCsv(new[]
        {
            new ModListExportRow
            {
                ModId = "\tTabbed.Mod",
                Name = "\rCarriage Mod",
                Version = "1.0.0",
                Enabled = false,
                Group = "Safe Group",
                WorkshopUrl = "=HYPERLINK(\"https://example.invalid\")"
            }
        });

        string expected = string.Join(Environment.NewLine,
            "Mod Id,Name,Version,Enabled,Group,Workshop Link",
            "'\tTabbed.Mod,\"'\rCarriage Mod\",1.0.0,FALSE,Safe Group,\"'=HYPERLINK(\"\"https://example.invalid\"\")\"",
            string.Empty);
        Assert.AreEqual(expected, csv);
    }

    private static string CreateTempDirectory()
    {
        string path = Path.Combine(Path.GetTempPath(), "BetterModMenuTests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }
}
