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

        var modIds = list.Select(mod => mod.Id ?? "").ToList();
        var assignedGroups = ModdingGroupStateOps.BuildAssignedGroupLookup(modIds);
        if (!ModOrderRules.TryBuildMove(modIds, assignedGroups, modId, direction, out var move))
            return false;

        var originalList = list.ToList();
        var movedItem = list[move.FromIndex];
        list.RemoveAt(move.FromIndex);
        list.Insert(move.InsertIndex, movedItem);

        try
        {
            SaveManager.Instance?.SaveSettings();
            if (ProfileManager.SaveInMemoryState())
                return true;
        }
        catch (Exception ex)
        {
            ProfileManager.ModLogger.Error($"Failed to persist mod order change:\n{ex}");
        }

        list.Clear();
        list.AddRange(originalList);
        try
        {
            SaveManager.Instance?.SaveSettings();
        }
        catch (Exception ex)
        {
            ProfileManager.ModLogger.Error($"Failed to restore mod order after a persistence error:\n{ex}");
        }

        return false;
    }

    public static bool ApplyToggleAllInGroup(Control modRowContainer, string groupName, bool isToggled)
    {
        var profile = ProfileManager.CurrentProfile;
        var options = SaveManager.Instance?.SettingsSave?.ModSettings;
        if (options == null)
            return false;

        var assignedGroups = ModdingGroupStateOps.BuildAssignedGroupLookup(
            modRowContainer.GetChildren()
                .OfType<NModMenuRow>()
                .Select(row => row.Mod?.manifest?.id ?? ""));
        var settingsById = options.ModList
            .Where(mod => !string.IsNullOrWhiteSpace(mod.Id))
            .GroupBy(mod => mod.Id!, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.Ordinal);

        bool changed = false;
        foreach (Node child in modRowContainer.GetChildren())
        {
            if (child is not NModMenuRow row || row.Mod?.manifest == null)
                continue;

            string modId = row.Mod.manifest.id ?? "";
            if (string.IsNullOrEmpty(modId) ||
                !assignedGroups.TryGetValue(modId, out string? assignedGroup) ||
                assignedGroup == null ||
                assignedGroup != groupName)
                continue;

            if (settingsById.TryGetValue(modId, out var settingsMod))
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
