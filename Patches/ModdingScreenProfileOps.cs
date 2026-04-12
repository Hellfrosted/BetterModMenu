using System.Collections.Generic;
using Godot;
using BetterModMenu.Data;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.Screens.ModdingScreen;
using MegaCrit.Sts2.Core.Saves;

namespace BetterModMenu.Patches;

internal static class ModdingScreenProfileOps
{
    public static bool TryApplyProfileSelection(int index, bool snapshotCurrentProfile, out ModProfile? profile)
    {
        profile = null;
        var previousProfiles = CloneProfiles(ProfileManager.Profiles);
        int previousProfileIndex = ProfileManager.CurrentProfileIndex;
        var options = SaveManager.Instance?.SettingsSave?.ModSettings;
        List<bool>? previousEnabledStates = options?.ModList.Select(mod => mod.IsEnabled).ToList();

        try
        {
            if (snapshotCurrentProfile)
                ProfileManager.SnapshotCurrentState();

            ProfileManager.CurrentProfileIndex = index;
            ProfileManager.NormalizeProfileIndex();

            profile = ProfileManager.CurrentProfile;
            if (options != null)
            {
                foreach (var mod in options.ModList)
                    mod.IsEnabled = !profile.DisabledMods.Contains(mod.Id);
            }

            if (!ProfileManager.SaveInMemoryState())
                throw new InvalidOperationException(ProfileManager.LastPersistenceError ?? "Failed to save the selected profile.");

            SaveManager.Instance?.SaveSettings();
            return true;
        }
        catch (Exception ex)
        {
            ProfileManager.Profiles = previousProfiles;
            ProfileManager.CurrentProfileIndex = previousProfileIndex;

            if (options != null && previousEnabledStates != null)
            {
                for (int i = 0; i < options.ModList.Count && i < previousEnabledStates.Count; i++)
                    options.ModList[i].IsEnabled = previousEnabledStates[i];
            }

            ProfileManager.SaveInMemoryState();
            try
            {
                SaveManager.Instance?.SaveSettings();
            }
            catch (Exception restoreEx)
            {
                ProfileManager.ModLogger.Error($"Failed to restore settings after a profile-selection error:\n{restoreEx}");
            }

            ProfileManager.ModLogger.Error($"Failed to apply profile selection:\n{ex}");
            return false;
        }
    }

    public static void SyncTickboxesForProfile(Control modRowContainer, ModProfile profile)
    {
        foreach (Node child in modRowContainer.GetChildren())
        {
            if (child is not NModMenuRow row || row.Mod?.manifest == null)
                continue;

            string modId = row.Mod.manifest.id ?? "";
            bool isOn = !string.IsNullOrEmpty(modId) && !profile.DisabledMods.Contains(modId);
            var tickbox = row.GetNodeOrNull<NTickbox>(ModdingScreenConstants.TickboxPath);
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
        var previousProfiles = CloneProfiles(ProfileManager.Profiles);
        int previousProfileIndex = ProfileManager.CurrentProfileIndex;
        var newProfile = new ModProfile
        {
            Name = "Profile " + (ProfileManager.Profiles.Count + 1),
            DisabledMods = new HashSet<string>(ProfileManager.CurrentProfile.DisabledMods)
        };

        ProfileManager.Profiles.Add(newProfile);
        ProfileManager.CurrentProfileIndex = ProfileManager.Profiles.Count - 1;
        if (ProfileManager.SaveInMemoryState())
            return;

        ProfileManager.Profiles = previousProfiles;
        ProfileManager.CurrentProfileIndex = previousProfileIndex;
    }

    public static bool TryRenameCurrentProfile(string newName)
    {
        string trimmedName = newName.Trim();
        if (string.IsNullOrEmpty(trimmedName))
            return false;

        string previousName = ProfileManager.CurrentProfile.Name;
        ProfileManager.CurrentProfile.Name = trimmedName;
        if (ProfileManager.SaveInMemoryState())
            return true;

        ProfileManager.CurrentProfile.Name = previousName;
        return false;
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

    private static List<ModProfile> CloneProfiles(IReadOnlyList<ModProfile> profiles)
    {
        return profiles
            .Select(profile => new ModProfile
            {
                Name = profile.Name,
                DisabledMods = new HashSet<string>(profile.DisabledMods, StringComparer.Ordinal)
            })
            .ToList();
    }
}
