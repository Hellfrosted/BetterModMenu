using System.Text.Json;

namespace BetterModMenu.Data;

internal static class ProfileSaveStorage
{
    public const string ErrorNoWritableConfigPath = "No writable config path could be resolved.";
    public const string ErrorBackupFileNotFound = "Backup file was not found.";
    public const string ErrorBackupFileNoProfiles = "Backup file does not contain any profiles.";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true
    };

    public static ProfileSaveData Normalize(ProfileSaveData saveData)
    {
        saveData.Profiles ??= new();
        saveData.CustomGroups ??= new();
        saveData.ModGroups ??= new();
        saveData.CollapsedGroups ??= new();
        saveData.Tutorial ??= new();
        saveData.CloudBackups ??= new();
        saveData.ModNameStyles = NormalizeModNameStyleSettings(saveData.ModNameStyles);
        saveData.ModAnnotations = ModAnnotationRules.NormalizeDictionary(saveData.ModAnnotations);
        return saveData;
    }

    private static ModNameStyleSettings NormalizeModNameStyleSettings(ModNameStyleSettings? settings)
    {
        if (settings == null)
            return new ModNameStyleSettings();

        settings.TagFormats ??= new();
        settings.TagPriority ??= new();
        settings.DisabledTags ??= new(StringComparer.OrdinalIgnoreCase);
        settings.ModFormats ??= new();
        return settings;
    }

    public static bool TryWrite(string path, ProfileSaveData saveData, out string? error)
    {
        error = null;
        string? tempPath = null;

        try
        {
            if (string.IsNullOrWhiteSpace(path))
                throw new InvalidOperationException(ErrorNoWritableConfigPath);

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
            try
            {
                if (!string.IsNullOrEmpty(tempPath) && File.Exists(tempPath))
                    File.Delete(tempPath);
            }
            catch (Exception cleanupError)
            {
                error += $" Temporary file cleanup failed: {cleanupError.Message}";
            }
            return false;
        }
    }

    public static ProfileSaveData LoadOrDefault(string path)
    {
        if (!File.Exists(path))
            return new ProfileSaveData();

        string json = File.ReadAllText(path);
        return TryReadSaveDataWithProfiles(json, out var saveData) ? saveData : new ProfileSaveData();
    }

    public static bool TryReadExisting(string path, out ProfileSaveData saveData, out string? error)
    {
        saveData = new ProfileSaveData();
        error = null;

        try
        {
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            {
                error = ErrorBackupFileNotFound;
                return false;
            }

            string json = File.ReadAllText(path);
            if (TryReadSaveDataWithProfiles(json, out saveData))
                return true;

            error = ErrorBackupFileNoProfiles;
            return false;
        }
        catch (Exception ex)
        {
            error = ex.Message;
            return false;
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
