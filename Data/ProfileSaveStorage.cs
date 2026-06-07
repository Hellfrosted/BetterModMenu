using System.Text.Json;

namespace BetterModMenu.Data;

internal static class ProfileSaveStorage
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true
    };

    public static ProfileSaveData Capture(
        List<ModProfile> profiles,
        int currentProfileIndex,
        List<string> customGroups,
        Dictionary<string, string> modGroups,
        HashSet<string> collapsedGroups,
        TutorialState tutorial,
        CloudBackupSettings cloudBackups)
    {
        return new ProfileSaveData
        {
            Profiles = profiles,
            CurrentProfileIndex = currentProfileIndex,
            CustomGroups = customGroups,
            ModGroups = modGroups,
            CollapsedGroups = collapsedGroups,
            Tutorial = tutorial,
            CloudBackups = cloudBackups
        };
    }

    public static ProfileSaveData Normalize(ProfileSaveData saveData)
    {
        return new ProfileSaveData
        {
            Profiles = saveData.Profiles ?? new(),
            CurrentProfileIndex = saveData.CurrentProfileIndex,
            CustomGroups = saveData.CustomGroups ?? new(),
            ModGroups = saveData.ModGroups ?? new(),
            CollapsedGroups = saveData.CollapsedGroups ?? new(),
            Tutorial = saveData.Tutorial ?? new(),
            CloudBackups = saveData.CloudBackups ?? new()
        };
    }

    public static bool TryWrite(string path, ProfileSaveData saveData, Action<string> setActiveConfigExtensionFromPath, out string? error)
    {
        error = null;
        string? tempPath = null;

        try
        {
            if (string.IsNullOrWhiteSpace(path))
                throw new InvalidOperationException("No writable config path could be resolved.");

            setActiveConfigExtensionFromPath(path);

            var folder = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(folder) && !Directory.Exists(folder))
                Directory.CreateDirectory(folder);

            string json = JsonSerializer.Serialize(saveData, JsonOptions);
            tempPath = path + ".tmp";
            File.WriteAllText(tempPath, json);
            File.Move(tempPath, path, true);
            return true;
        }
        catch (Exception ex)
        {
            error = ex.Message;
            if (!string.IsNullOrEmpty(tempPath) && File.Exists(tempPath))
                File.Delete(tempPath);
            return false;
        }
    }

    public static ProfileSaveData LoadOrDefault(string path, Action<string> setActiveConfigExtensionFromPath)
    {
        if (!File.Exists(path))
            return new ProfileSaveData();

        setActiveConfigExtensionFromPath(path);
        string json = File.ReadAllText(path);
        return ReadSaveDataOrDefault(json);
    }

    public static bool TryReadExisting(string path, out ProfileSaveData saveData, out string? error)
    {
        saveData = new ProfileSaveData();
        error = null;

        try
        {
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            {
                error = "Backup file was not found.";
                return false;
            }

            string json = File.ReadAllText(path);
            if (TryReadSaveDataWithProfiles(json, out saveData))
                return true;

            error = "Backup file does not contain any profiles.";
            return false;
        }
        catch (Exception ex)
        {
            error = ex.Message;
            return false;
        }
    }

    private static ProfileSaveData ReadSaveDataOrDefault(string json)
    {
        try
        {
            var loaded = JsonSerializer.Deserialize<ProfileSaveData>(json, JsonOptions);
            return loaded != null && loaded.Profiles != null && loaded.Profiles.Count > 0
                ? loaded
                : new ProfileSaveData();
        }
        catch
        {
            var legacy = JsonSerializer.Deserialize<List<ModProfile>>(json, JsonOptions);
            return legacy != null
                ? new ProfileSaveData { Profiles = legacy }
                : new ProfileSaveData();
        }
    }

    private static bool TryReadSaveDataWithProfiles(string json, out ProfileSaveData saveData)
    {
        saveData = new ProfileSaveData();
        try
        {
            var loaded = JsonSerializer.Deserialize<ProfileSaveData>(json, JsonOptions);
            if (loaded != null && loaded.Profiles != null && loaded.Profiles.Count > 0)
            {
                saveData = loaded;
                return true;
            }
        }
        catch (JsonException)
        {
            var legacy = JsonSerializer.Deserialize<List<ModProfile>>(json, JsonOptions);
            if (legacy != null && legacy.Count > 0)
            {
                saveData = new ProfileSaveData { Profiles = legacy };
                return true;
            }
        }

        return false;
    }
}
