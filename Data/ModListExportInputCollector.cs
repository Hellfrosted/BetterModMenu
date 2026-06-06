using System.Collections.Generic;
using System.IO;
using System.Linq;
using MegaCrit.Sts2.Core.Saves;

namespace BetterModMenu.Data;

internal static class ModListExportInputCollector
{
    public static List<InstalledModExportInput> Collect(IEnumerable<string> manifestExtensions)
    {
        var enabledById = new Dictionary<string, bool>(System.StringComparer.Ordinal);
        var settingsMods = SaveManager.Instance?.SettingsSave?.ModSettings?.ModList;
        if (settingsMods != null)
        {
            foreach (var mod in settingsMods)
            {
                if (!string.IsNullOrWhiteSpace(mod.Id))
                    enabledById[mod.Id] = mod.IsEnabled;
            }
        }

        var inputs = new List<InstalledModExportInput>();
        var liveMods = MegaCrit.Sts2.Core.Modding.ModManager.Mods;
        if (liveMods == null)
            return inputs;

        foreach (var mod in liveMods)
        {
            string modId = mod.manifest?.id ?? string.Empty;
            if (string.IsNullOrWhiteSpace(modId))
                continue;

            string? directory = Directory.Exists(mod.path) ? mod.path : Path.GetDirectoryName(mod.path);
            string manifestPath = !string.IsNullOrWhiteSpace(directory)
                ? ManifestScanner.FindManifestPath(directory, modId, manifestExtensions) ?? string.Empty
                : string.Empty;

            inputs.Add(new InstalledModExportInput
            {
                ModId = modId,
                Enabled = !enabledById.TryGetValue(modId, out bool isEnabled) || isEnabled,
                ManifestPath = manifestPath
            });
        }

        return inputs
            .OrderBy(input => input.ModId, System.StringComparer.OrdinalIgnoreCase)
            .ToList();
    }
}
