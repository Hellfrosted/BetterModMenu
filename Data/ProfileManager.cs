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
    private static readonly string[] ConfigExtensions = new[] { ".json5", ".jsonc", ".json" };
    private static readonly JsonSerializerOptions JsonOpts = new() { 
        WriteIndented = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true
    };
    private static string _activeConfigExtension = ".json";

    private static string NormalizeConfigExtension(string extension)
    {
        if (string.IsNullOrEmpty(extension))
            return _activeConfigExtension;

        string normalized = extension.StartsWith(".") ? extension.ToLowerInvariant() : "." + extension.ToLowerInvariant();
        return ConfigExtensions.Contains(normalized) ? normalized : _activeConfigExtension;
    }

    private static string BuildConfigPath(string directory, string extension)
    {
        return Path.Combine(directory, "mod_profiles" + NormalizeConfigExtension(extension));
    }

    private static void SetActiveConfigExtension(string extension)
    {
        _activeConfigExtension = NormalizeConfigExtension(extension);
    }

    private static void SetActiveConfigExtensionFromPath(string path)
    {
        SetActiveConfigExtension(Path.GetExtension(path));
    }

    private static string? FindExistingConfigPath(string directory)
    {
        if (string.IsNullOrEmpty(directory) || !Directory.Exists(directory))
            return null;

        foreach (var ext in ConfigExtensions)
        {
            string path = BuildConfigPath(directory, ext);
            if (File.Exists(path))
                return path;
        }

        return null;
    }

    private static string ResolveConfigPath(string directory, bool ensureDirectoryExists = false)
    {
        if (ensureDirectoryExists && !string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            Directory.CreateDirectory(directory);

        var existingPath = FindExistingConfigPath(directory);
        if (!string.IsNullOrEmpty(existingPath))
            return existingPath;

        return BuildConfigPath(directory, _activeConfigExtension);
    }

    private static string ResolveUserConfigDirectory(bool ensureDirectoryExists)
    {
        string userPath = UserDataPathProvider.GetAccountScopedBasePath("mod_data/BetterModMenu");
        string absolutePath = ProjectSettings.GlobalizePath(userPath);
        if (ensureDirectoryExists && !Directory.Exists(absolutePath))
            Directory.CreateDirectory(absolutePath);

        return absolutePath;
    }

    private static string ResolvePortableConfigDirectory()
    {
        var mod = MegaCrit.Sts2.Core.Modding.ModManager.Mods.FirstOrDefault(m => m.manifest?.id == "BetterModMenu");
        string path = (mod != null && !string.IsNullOrEmpty(mod.path))
            ? mod.path
            : (Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) ?? "");

        if (Directory.Exists(path))
            return path;

        return Path.GetDirectoryName(path) ?? "";
    }

    public static string PortableConfigDirectory => ResolvePortableConfigDirectory();
    public static string PortableConfigPath => ResolveConfigPath(PortableConfigDirectory);

    public static string UserConfigDirectory
    {
        get
        {
            return ResolveUserConfigDirectory(ensureDirectoryExists: false);
        }
    }

    public static string UserConfigPath => ResolveConfigPath(ResolveUserConfigDirectory(ensureDirectoryExists: true));

    public static string SavePath
    {
        get
        {
            string portablePath = PortableConfigPath;
            if (File.Exists(portablePath))
                return portablePath;

            return UserConfigPath;
        }
    }

    public static string GetPortableConfigPathForExtension(string extension) => BuildConfigPath(PortableConfigDirectory, extension);
    public static string GetUserConfigPathForExtension(string extension) => BuildConfigPath(ResolveUserConfigDirectory(ensureDirectoryExists: true), extension);

    public static void DeleteOtherConfigVariants(string pathToKeep)
    {
        string? directory = Path.GetDirectoryName(pathToKeep);
        if (string.IsNullOrEmpty(directory))
            return;

        string fullKeepPath = Path.GetFullPath(pathToKeep);
        foreach (var ext in ConfigExtensions)
        {
            string candidate = BuildConfigPath(directory, ext);
            if (!Path.GetFullPath(candidate).Equals(fullKeepPath, StringComparison.OrdinalIgnoreCase) && File.Exists(candidate))
                File.Delete(candidate);
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
    public static void SaveInMemoryState()
    {
        SaveToPath(SavePath);
    }

    /// <summary>
    /// Auto-save helper for live game changes such as enabling/disabling mods.
    /// </summary>
    public static void SnapshotCurrentStateAndSave()
    {
        SnapshotCurrentState();
        SaveInMemoryState();
    }

    /// <summary>
    /// Writes the current live game state to a specific save path.
    /// </summary>
    public static void SaveCurrentStateToPath(string path)
    {
        SnapshotCurrentState();
        SaveToPath(path);
    }

    public static void SaveToPath(string path)
    {
        try
        {
            SetActiveConfigExtensionFromPath(path);

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
            File.WriteAllText(path, json);
        }
        catch (Exception ex)
        {
            ModLogger.Error("Failed to save mod profiles: " + ex.Message);
        }
    }

    public static void LoadProfiles()
    {
        try
        {
            string savePath = SavePath;
            if (File.Exists(savePath))
            {
                SetActiveConfigExtensionFromPath(savePath);
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
            ModLogger.Error($"Profile format corrupted:\n{ex}");
            Profiles.Add(new ModProfile { Name = "Default" });
            NormalizeProfileIndex();
        }
        catch (IOException ex)
        {
            ModLogger.Error($"Unable to read profile save file. It may be locked by another program.\n{ex}");
            Profiles.Add(new ModProfile { Name = "Default" });
            NormalizeProfileIndex();
        }
        catch (Exception ex)
        {
            ModLogger.Error($"Failed to load mod profiles:\n{ex}");
            Profiles.Add(new ModProfile { Name = "Default" });
            NormalizeProfileIndex();
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
                        var docOpts = new JsonDocumentOptions { CommentHandling = JsonCommentHandling.Skip, AllowTrailingCommas = true };
                        using var doc = JsonDocument.Parse(content, docOpts);
                        if (doc.RootElement.TryGetProperty("id", out var idProp) && idProp.ValueKind == JsonValueKind.String)
                        {
                            string id = idProp.GetString() ?? "";
                            if (!string.IsNullOrEmpty(id))
                            {
                                // Skip files like RouteSuggestConfig.json if they contain a different mod's ID.
                                // STS2 mod manifests should have a filename that matches the mod ID.
                                if (!System.IO.Path.GetFileNameWithoutExtension(file).Equals(id, System.StringComparison.OrdinalIgnoreCase))
                                    continue;

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
