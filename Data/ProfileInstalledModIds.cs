using System.Collections.Generic;
using MegaCrit.Sts2.Core.Saves;

namespace BetterModMenu.Data;

internal static class ProfileInstalledModIds
{
    public static HashSet<string> Collect()
    {
        var installedModIds = new HashSet<string>(System.StringComparer.Ordinal);

        foreach (var mod in Sts2ModManagerCompat.GetLoadedMods())
        {
            if (!string.IsNullOrWhiteSpace(mod.Id))
                installedModIds.Add(mod.Id);
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
