using Godot;
using BetterModMenu.Data;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.Screens.ModdingScreen;
using MegaCrit.Sts2.Core.Saves;

namespace BetterModMenu.Patches;

internal static class ModdingScreenListOps
{
    public static bool TryMoveModOrder(string modId, int direction)
    {
        var options = SaveManager.Instance?.SettingsSave?.ModSettings;
        if (options == null)
            return false;

        var list = options.ModList;
        int index = list.FindIndex(mod => mod.Id == modId);
        if (index == -1)
            return false;

        int newIndex = index + direction;
        if (newIndex < 0 || newIndex >= list.Count)
            return false;

        var temp = list[index];
        list[index] = list[newIndex];
        list[newIndex] = temp;
        SaveManager.Instance?.SaveSettings();
        ProfileManager.SaveInMemoryState();
        return true;
    }

    public static bool ApplyToggleAllInGroup(Control modRowContainer, string groupName, bool isToggled)
    {
        var profile = ProfileManager.CurrentProfile;
        var options = SaveManager.Instance?.SettingsSave?.ModSettings;
        if (options == null)
            return false;

        bool changed = false;
        foreach (Node child in modRowContainer.GetChildren())
        {
            if (child is not NModMenuRow row || row.Mod?.manifest == null)
                continue;

            string modId = row.Mod.manifest.id ?? "";
            if (string.IsNullOrEmpty(modId) || ModdingScreenStateOps.GetAssignedGroup(modId) != groupName)
                continue;

            var settingsMod = options.ModList.Find(mod => mod.Id == modId);
            if (settingsMod != null)
            {
                settingsMod.IsEnabled = isToggled;
                if (isToggled)
                    profile.DisabledMods.Remove(modId);
                else
                    profile.DisabledMods.Add(modId);
                changed = true;
            }

            var tickbox = row.GetNodeOrNull<NTickbox>(ModdingScreenConstants.TickboxPath);
            if (tickbox == null)
                continue;

            try
            {
                tickbox.IsTicked = isToggled;
            }
            catch (System.Exception ex)
            {
                ProfileManager.ModLogger.Error($"Failed to toggle tickbox:\n{ex}");
            }
        }

        return changed;
    }
}
