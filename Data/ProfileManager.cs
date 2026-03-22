using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Godot;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Saves;

namespace BetterModMenu.Data;

public class ModProfile
{
    public string Name { get; set; } = "Default";
    public HashSet<string> DisabledMods { get; set; } = new();
}

public class ProfileSaveData
{
    public List<ModProfile> Profiles { get; set; } = new();
    public int CurrentProfileIndex { get; set; } = 0;

    public List<string> CustomGroups { get; set; } = new();
    public Dictionary<string, string> ModGroups { get; set; } = new();
    public HashSet<string> CollapsedGroups { get; set; } = new();
}

public static class ProfileManager
{
    public static readonly Logger ModLogger = new("BetterModMenu", LogType.Generic);
    private static readonly JsonSerializerOptions JsonOpts = new() { WriteIndented = true };
    private static string SavePath
    {
        get
        {
            string userPath = UserDataPathProvider.GetAccountScopedBasePath("mod_data/BetterModMenu");
            string absolutePath = ProjectSettings.GlobalizePath(userPath);
            if (!System.IO.Directory.Exists(absolutePath))
            {
                System.IO.Directory.CreateDirectory(absolutePath);
            }
            return System.IO.Path.Combine(absolutePath, "mod_profiles.json");
        }
    }

    public static List<ModProfile> Profiles { get; set; } = new();
    public static int CurrentProfileIndex { get; set; } = 0;

    public static List<string> CustomGroups { get; set; } = new();
    public static Dictionary<string, string> ModGroups { get; set; } = new();
    public static HashSet<string> CollapsedGroups { get; set; } = new();

    public static ModProfile CurrentProfile
    {
        get
        {
            if (Profiles.Count == 0) Profiles.Add(new ModProfile { Name = "Default" });
            if (CurrentProfileIndex >= Profiles.Count) CurrentProfileIndex = 0;
            return Profiles[CurrentProfileIndex];
        }
    }

    /// <summary>
    /// Reads the current game mod states and writes them into the given profile's DisabledMods.
    /// </summary>
    public static void SnapshotIntoProfile(ModProfile profile)
    {
        var options = SaveManager.Instance?.SettingsSave?.ModSettings;
        if (options == null) return;
        profile.DisabledMods.Clear();
        foreach (var mod in options.ModList)
        {
            if (!mod.IsEnabled && mod.Id != null)
                profile.DisabledMods.Add(mod.Id);
        }
    }

    /// <summary>
    /// Snapshot current game state into the active profile, then save to disk.
    /// Use this for auto-save scenarios (user toggles a mod).
    /// </summary>
    public static void SnapshotAndSave()
    {
        SnapshotIntoProfile(CurrentProfile);
        SaveToDisk();
    }

    /// <summary>
    /// Pure serialization — writes current in-memory data to disk WITHOUT snapshotting game state.
    /// Use this when you've already manually set DisabledMods (e.g. during profile switching).
    /// </summary>
    public static void SaveToDisk()
    {
        try
        {
            var folder = Path.GetDirectoryName(SavePath);
            if (!string.IsNullOrEmpty(folder) && !Directory.Exists(folder))
            {
                Directory.CreateDirectory(folder);
            }

            var saveData = new ProfileSaveData
            {
                Profiles = Profiles,
                CurrentProfileIndex = CurrentProfileIndex,
                CustomGroups = CustomGroups,
                ModGroups = ModGroups,
                CollapsedGroups = CollapsedGroups
            };
            var json = JsonSerializer.Serialize(saveData, JsonOpts);
            File.WriteAllText(SavePath, json);
        }
        catch (Exception ex)
        {
            ModLogger.Error("Failed to save mod profiles: " + ex.Message);
        }
    }

    public static void SaveProfiles() => SnapshotAndSave();

    public static void LoadProfiles()
    {
        try
        {
            if (File.Exists(SavePath))
            {
                var json = File.ReadAllText(SavePath);
                try
                {
                    var loaded = JsonSerializer.Deserialize<ProfileSaveData>(json);
                    if (loaded != null && loaded.Profiles != null && loaded.Profiles.Count > 0)
                    {
                        Profiles = loaded.Profiles;
                        CurrentProfileIndex = loaded.CurrentProfileIndex;
                        CustomGroups = loaded.CustomGroups ?? new();
                        ModGroups = loaded.ModGroups ?? new();
                        CollapsedGroups = loaded.CollapsedGroups ?? new();
                    }
                }
                catch
                {
                    // Fallback for legacy format
                    var legacy = JsonSerializer.Deserialize<List<ModProfile>>(json);
                    if (legacy != null) Profiles = legacy;
                }
            }
            if (Profiles.Count == 0)
            {
                Profiles.Add(new ModProfile { Name = "Default" });
            }
        }
        catch (JsonException ex)
        {
            ModLogger.Error($"Profile format corrupted:\n{ex}");
            Profiles.Add(new ModProfile { Name = "Default" });
        }
        catch (IOException ex)
        {
            ModLogger.Error($"Unable to read profile save file. It may be locked by another program.\n{ex}");
            Profiles.Add(new ModProfile { Name = "Default" });
        }
        catch (Exception ex)
        {
            ModLogger.Error($"Failed to load mod profiles:\n{ex}");
            Profiles.Add(new ModProfile { Name = "Default" });
        }
    }
}
