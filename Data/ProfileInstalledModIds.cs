using System.Collections.Generic;
using MegaCrit.Sts2.Core.Saves;

namespace BetterModMenu.Data;

internal static class ProfileInstalledModIds
{
    public static HashSet<string> Collect()
    {
        var installedModIds = new HashSet<string>(System.StringComparer.Ordinal);

        var liveMods = MegaCrit.Sts2.Core.Modding.ModManager.Mods;
        if (liveMods != null)
        {
            foreach (var mod in liveMods)
            {
                string modId = mod.manifest?.id ?? string.Empty;
                if (!string.IsNullOrWhiteSpace(modId))
                    installedModIds.Add(modId);
            }
        }

        var settingsMods = SaveManager.Instance?.SettingsSave?.ModSettings?.ModList;
        if (settingsMods != null)
        {
            foreach (var mod in settingsMods)
            {
                if (!string.IsNullOrWhiteSpace(mod.Id))
                    installedModIds.Add(mod.Id);
            }
        }

        return installedModIds;
    }
}
