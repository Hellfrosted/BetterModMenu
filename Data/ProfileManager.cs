using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
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
    public static readonly MegaCrit.Sts2.Core.Logging.Logger ModLogger = new("BetterModMenu", LogType.Generic);
    private const string UnassignedGroupName = "Unassigned";
    private static readonly JsonSerializerOptions JsonOpts = new() { 
        WriteIndented = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true
    };
    private static readonly ProfileConfigPathResolver ConfigPaths = new("BetterModMenu", ".json5", ".jsonc", ".json");
    public static string? LastPersistenceError { get; private set; }

    public static string PortableConfigDirectory => ConfigPaths.PortableConfigDirectory;
    public static string PortableConfigPath => ConfigPaths.PortableConfigPath;
    public static string UserConfigDirectory => ConfigPaths.UserConfigDirectory;
    public static string UserConfigPath => ConfigPaths.UserConfigPath;
    public static string SavePath => ConfigPaths.SavePath;

    public static bool TryGetPortableConfigPath(out string path) => ConfigPaths.TryGetPortableConfigPath(out path);
    public static bool TryGetPortableConfigPathForExtension(string extension, out string path) => ConfigPaths.TryGetPortableConfigPathForExtension(extension, out path);
    public static string GetPortableConfigPathForExtension(string extension) => ConfigPaths.GetPortableConfigPathForExtension(extension);
    public static string GetUserConfigPathForExtension(string extension) => ConfigPaths.GetUserConfigPathForExtension(extension);
    public static void DeleteOtherConfigVariants(string pathToKeep) => ConfigPaths.DeleteOtherConfigVariants(pathToKeep);

    public static List<ModProfile> Profiles { get; set; } = new();
    public static int CurrentProfileIndex { get; set; } = 0;

    public static List<string> CustomGroups { get; set; } = new();
    public static Dictionary<string, string> ModGroups { get; set; } = new();
    public static HashSet<string> CollapsedGroups { get; set; } = new();
    public static Dictionary<string, bool> ModGameplayImpactCache { get; set; } = new();

    public static void ResetState()
    {
        Profiles = new();
        CurrentProfileIndex = 0;
        CustomGroups = new();
        ModGroups = new();
        CollapsedGroups = new();
        ModGameplayImpactCache = new();
        LastPersistenceError = null;
    }

    public static ModProfile CurrentProfile
    {
        get
        {
            NormalizeProfileIndex();
            return Profiles[CurrentProfileIndex];
        }
    }

    public static void NormalizeProfileIndex()
    {
        if (Profiles.Count == 0)
            Profiles.Add(new ModProfile { Name = "Default" });

        if (CurrentProfileIndex < 0)
            CurrentProfileIndex = 0;
        else if (CurrentProfileIndex >= Profiles.Count)
            CurrentProfileIndex = Profiles.Count - 1;
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
    /// Reads the current game state into the active profile without writing to disk.
    /// </summary>
    public static void SnapshotCurrentState()
    {
        SnapshotIntoProfile(CurrentProfile);
    }

    /// <summary>
    /// Pure serialization — writes current in-memory profile/group state to the active save path.
    /// Use this when you've already updated in-memory state yourself.
    /// </summary>
    public static bool SaveInMemoryState()
    {
        return SaveToPath(SavePath);
    }

    /// <summary>
    /// Auto-save helper for live game changes such as enabling/disabling mods.
    /// </summary>
    public static bool SnapshotCurrentStateAndSave()
    {
        SnapshotCurrentState();
        return SaveInMemoryState();
    }

    /// <summary>
    /// Writes the current live game state to a specific save path.
    /// </summary>
    public static bool SaveCurrentStateToPath(string path)
    {
        SnapshotCurrentState();
        return SaveToPath(path);
    }

    public static bool SaveToPath(string path)
    {
        string? tempPath = null;
        try
        {
            if (string.IsNullOrWhiteSpace(path))
                throw new InvalidOperationException("No writable config path could be resolved.");

            ConfigPaths.SetActiveConfigExtensionFromPath(path);
            LastPersistenceError = null;

            var folder = Path.GetDirectoryName(path);
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
            tempPath = path + ".tmp";
            File.WriteAllText(tempPath, json);
            File.Move(tempPath, path, true);
            return true;
        }
        catch (Exception ex)
        {
            LastPersistenceError = ex.Message;
            if (!string.IsNullOrEmpty(tempPath) && File.Exists(tempPath))
                File.Delete(tempPath);
            ModLogger.Error($"Failed to save mod profiles to '{path}':\n{ex}");
            return false;
        }
    }

    public static void LoadProfiles()
    {
        try
        {
            ResetState();
            string savePath = SavePath;
            if (File.Exists(savePath))
            {
                ConfigPaths.SetActiveConfigExtensionFromPath(savePath);
                var json = File.ReadAllText(savePath);
                try
                {
                    var loaded = JsonSerializer.Deserialize<ProfileSaveData>(json, JsonOpts);
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
                    var legacy = JsonSerializer.Deserialize<List<ModProfile>>(json, JsonOpts);
                    if (legacy != null) Profiles = legacy;
                }
            }
            if (Profiles.Count == 0)
            {
                Profiles.Add(new ModProfile { Name = "Default" });
            }

            NormalizeProfileIndex();
        }
        catch (JsonException ex)
        {
            ResetState();
            ModLogger.Error($"Profile format corrupted:\n{ex}");
            Profiles.Add(new ModProfile { Name = "Default" });
            NormalizeProfileIndex();
        }
        catch (IOException ex)
        {
            ResetState();
            ModLogger.Error($"Unable to read profile save file. It may be locked by another program.\n{ex}");
            Profiles.Add(new ModProfile { Name = "Default" });
            NormalizeProfileIndex();
        }
        catch (Exception ex)
        {
            ResetState();
            ModLogger.Error($"Failed to load mod profiles:\n{ex}");
            Profiles.Add(new ModProfile { Name = "Default" });
            NormalizeProfileIndex();
        }
    }

    public static bool NormalizePersistedState(IEnumerable<string> installedModIds)
    {
        return ProfileStateRules.NormalizeGroups(CustomGroups, ModGroups, CollapsedGroups, installedModIds, UnassignedGroupName);
    }

    public static void NormalizePersistedStateAndSaveIfNeeded()
    {
        var installedModIds = ProfileInstalledModIds.Collect();

        if (installedModIds.Count == 0)
            return;

        if (NormalizePersistedState(installedModIds))
            SaveInMemoryState();
    }

    public static void BuildManifestCache()
    {
        ProfileManifestCacheBuilder.Rebuild(ModGameplayImpactCache, ModLogger, ConfigPaths.ConfigExtensions);
    }
}
