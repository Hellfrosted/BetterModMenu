using System.Collections.Generic;
using Godot;
using BetterModMenu.Data;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.Screens.ModdingScreen;
using MegaCrit.Sts2.Core.Saves;

namespace BetterModMenu.Patches;

internal static class ModdingScreenProfileOps
{
    public static ModProfile ApplyProfileSelection(int index, bool snapshotCurrentProfile)
    {
        if (snapshotCurrentProfile)
            ProfileManager.SnapshotCurrentState();

        ProfileManager.CurrentProfileIndex = index;
        ProfileManager.NormalizeProfileIndex();

        var profile = ProfileManager.CurrentProfile;
        var options = SaveManager.Instance.SettingsSave.ModSettings;
        if (options != null)
        {
            foreach (var mod in options.ModList)
                mod.IsEnabled = !profile.DisabledMods.Contains(mod.Id);
        }

        ProfileManager.SaveInMemoryState();
        SaveManager.Instance.SaveSettings();
        return profile;
    }

    public static void SyncTickboxesForProfile(Control modRowContainer, ModProfile profile)
    {
        foreach (Node child in modRowContainer.GetChildren())
        {
            if (child is not NModMenuRow row || row.Mod?.manifest == null)
                continue;

            string modId = row.Mod.manifest.id ?? "";
            bool isOn = !string.IsNullOrEmpty(modId) && !profile.DisabledMods.Contains(modId);
            var tickbox = row.GetNodeOrNull<NTickbox>("Tickbox");
            if (tickbox == null)
                continue;

            try
            {
                tickbox.IsTicked = isOn;
            }
            catch (System.Exception ex)
            {
                ProfileManager.ModLogger.Error($"Failed to set tickbox state:\n{ex}");
            }
        }
    }

    public static void CreateNewProfileFromCurrentState()
    {
        ProfileManager.SnapshotCurrentState();
        var newProfile = new ModProfile
        {
            Name = "Profile " + (ProfileManager.Profiles.Count + 1),
            DisabledMods = new HashSet<string>(ProfileManager.CurrentProfile.DisabledMods)
        };

        ProfileManager.Profiles.Add(newProfile);
        ProfileManager.CurrentProfileIndex = ProfileManager.Profiles.Count - 1;
        ProfileManager.SaveInMemoryState();
    }

    public static bool TryRenameCurrentProfile(string newName)
    {
        string trimmedName = newName.Trim();
        if (string.IsNullOrEmpty(trimmedName))
            return false;

        ProfileManager.CurrentProfile.Name = trimmedName;
        ProfileManager.SaveInMemoryState();
        return true;
    }

    public static int? DeleteCurrentProfile()
    {
        if (ProfileManager.Profiles.Count <= 1)
            return null;

        int removedIndex = ProfileManager.CurrentProfileIndex;
        ProfileManager.Profiles.RemoveAt(removedIndex);

        int replacementIndex = removedIndex;
        if (replacementIndex >= ProfileManager.Profiles.Count)
            replacementIndex = ProfileManager.Profiles.Count - 1;

        return replacementIndex;
    }
}
