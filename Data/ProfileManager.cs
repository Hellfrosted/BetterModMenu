using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Godot;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Saves;
using System.Linq;

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
    public static readonly MegaCrit.Sts2.Core.Logging.Logger ModLogger = new("BetterModMenu", LogType.Generic);
    private static readonly JsonSerializerOptions JsonOpts = new() { WriteIndented = true };
    public static string PortableConfigPath
    {
        get
        {
            var mod = MegaCrit.Sts2.Core.Modding.ModManager.Mods.FirstOrDefault(m => m.manifest?.id == "BetterModMenu");
            string assemblyFolder = (mod != null && !string.IsNullOrEmpty(mod.path)) 
                ? mod.path 
                : (System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) ?? "");
            return System.IO.Path.Combine(assemblyFolder, "mod_profiles.json");
        }
    }

    public static string SavePath
    {
        get
        {
            if (File.Exists(PortableConfigPath))
                return PortableConfigPath;

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
    public static Dictionary<string, bool> ModGameplayImpactCache { get; set; } = new();

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

    public static void BuildManifestCache()
    {
        ModGameplayImpactCache.Clear();
        var mods = MegaCrit.Sts2.Core.Modding.ModManager.Mods;
        if (mods == null) return;

        var directoriesToScan = new HashSet<string>();
        foreach (var mod in mods)
        {
            if (!string.IsNullOrEmpty(mod.path))
            {
                var dir = System.IO.Directory.Exists(mod.path) ? mod.path : System.IO.Path.GetDirectoryName(mod.path);
                if (!string.IsNullOrEmpty(dir) && System.IO.Directory.Exists(dir))
                {
                    directoriesToScan.Add(dir);
                }
            }
        }

        foreach (var dir in directoriesToScan)
        {
            try
            {
                var jsonFiles = System.IO.Directory.GetFiles(dir, "*.json", System.IO.SearchOption.TopDirectoryOnly);
                foreach (var file in jsonFiles)
                {
                    try
                    {
                        string content = System.IO.File.ReadAllText(file);
                        using var doc = JsonDocument.Parse(content);
                        if (doc.RootElement.TryGetProperty("id", out var idProp) && idProp.ValueKind == JsonValueKind.String)
                        {
                            string id = idProp.GetString() ?? "";
                            if (!string.IsNullOrEmpty(id))
                            {
                                bool affectsGameplay = false;
                                if (doc.RootElement.TryGetProperty("affects_gameplay", out var gameplayProp))
                                {
                                    if (gameplayProp.ValueKind == JsonValueKind.True) affectsGameplay = true;
                                    else if (gameplayProp.ValueKind == JsonValueKind.False) affectsGameplay = false;
                                }
                                ModGameplayImpactCache[id] = affectsGameplay;
                            }
                        }
                    }
                    catch (Exception)
                    {
                        // Ignore parse errors on irrelevant .json files
                    }
                }
            }
            catch (Exception ex)
            {
                ModLogger.Error($"Failed to scan directory {dir} for manifests: {ex.Message}");
            }
        }
    }
}
