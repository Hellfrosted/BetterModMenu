using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Reflection;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Saves;

namespace BetterModMenu.Data;

public static class ProfileManager
{
    public const string ErrorRestoredProfileSaveCouldNotBeWritten = "The restored profile save could not be written.";

    public static readonly MegaCrit.Sts2.Core.Logging.Logger ModLogger = new("BetterModMenu", LogType.Generic);
    private const int AutomaticBackupRetentionCount = 12;
    private const string UnassignedGroupName = "Unassigned";
    private static readonly ProfileConfigPathResolver ConfigPaths = new("BetterModMenu", ".json5", ".jsonc", ".json");
    private static readonly HashSet<ProfileBackupReason> AutomaticBackupsThisProcess = new();
    public static string? LastPersistenceError { get; private set; }
    public static string? LastBackupError { get; private set; }

    public static string SavePath => ConfigPaths.SavePath;
    internal static string ExportDirectory => Path.Combine(Path.GetDirectoryName(SavePath) ?? string.Empty, "exports");
    internal static string BackupDirectory => Path.Combine(Path.GetDirectoryName(SavePath) ?? string.Empty, "backups");
    internal static IReadOnlyList<string> ConfigExtensions => ConfigPaths.ConfigExtensions;

    public static bool TryGetPortableConfigPath(out string path) => ConfigPaths.TryGetPortableConfigPath(out path);
    public static bool TryGetPortableConfigPathForExtension(string extension, out string path) => ConfigPaths.TryGetPortableConfigPathForExtension(extension, out path);
    public static string GetUserConfigPathForExtension(string extension) => ConfigPaths.GetUserConfigPathForExtension(extension);
    public static void DeleteOtherConfigVariants(string pathToKeep) => ConfigPaths.DeleteOtherConfigVariants(pathToKeep);

    private static ProfileSaveData State = new();
    public static List<ModProfile> Profiles { get => State.Profiles; set => State.Profiles = value; }
    public static int CurrentProfileIndex { get => State.CurrentProfileIndex; set => State.CurrentProfileIndex = value; }
    public static List<string> CustomGroups { get => State.CustomGroups; set => State.CustomGroups = value; }
    public static Dictionary<string, string> ModGroups { get => State.ModGroups; set => State.ModGroups = value; }
    public static HashSet<string> CollapsedGroups { get => State.CollapsedGroups; set => State.CollapsedGroups = value; }
    public static TutorialState Tutorial { get => State.Tutorial; set => State.Tutorial = value; }
    public static CloudBackupSettings CloudBackups { get => State.CloudBackups; set => State.CloudBackups = value; }
    public static ModNameStyleSettings ModNameStyles { get => State.ModNameStyles; set => State.ModNameStyles = value; }
    public static Dictionary<string, ModAnnotation> ModAnnotations { get => State.ModAnnotations; set => State.ModAnnotations = value; }
    public static Dictionary<string, bool> ModGameplayImpactCache { get; set; } = new();
    public static Dictionary<string, List<string>> ModWorkshopTagsCache { get; set; } = new();
    private static bool WorkshopTagCacheAttempted { get; set; }
    private static readonly WorkshopTagCacheCoordinator WorkshopTags = new(SteamWorkshopTagService.FetchTagsByPublishedFileIdAsync);

    public static void ResetState()
    {
        State = new();
        ModGameplayImpactCache = new();
        ModWorkshopTagsCache = new();
        WorkshopTagCacheAttempted = false;
        WorkshopTags.Reset();
        LastPersistenceError = null;
        LastBackupError = null;
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

    public static ModAnnotation GetModAnnotation(string modId)
    {
        return !string.IsNullOrWhiteSpace(modId) && ModAnnotations.TryGetValue(modId, out ModAnnotation? annotation)
            ? annotation
            : new ModAnnotation();
    }

    public static bool TrySaveModAnnotation(string modId, string? alias, string? notes, out string? error)
    {
        error = null;
        if (string.IsNullOrWhiteSpace(modId))
        {
            error = "No mod is selected.";
            return false;
        }

        if (!ModAnnotationRules.TryNormalize(alias, notes, out ModAnnotation annotation, out error))
            return false;

        bool hadPrevious = ModAnnotations.TryGetValue(modId, out ModAnnotation? previous);
        if (string.IsNullOrEmpty(annotation.Alias) && string.IsNullOrEmpty(annotation.Notes))
            ModAnnotations.Remove(modId);
        else
            ModAnnotations[modId] = annotation;

        if (SaveInMemoryState())
            return true;

        if (hadPrevious && previous != null)
            ModAnnotations[modId] = previous;
        else
            ModAnnotations.Remove(modId);

        error = LastPersistenceError;
        return false;
    }

    public static bool SnapshotCurrentStateAndSave()
    {
        SnapshotCurrentState();
        return SaveInMemoryState();
    }

    public static bool SaveCurrentStateToPath(string path)
    {
        SnapshotCurrentState();
        return SaveToPath(path);
    }

    public static bool SaveToPath(string path)
    {
        LastPersistenceError = null;
        if (ProfileSaveStorage.TryWrite(path, State, out string? error))
        {
            ConfigPaths.SetActiveConfigExtensionFromPath(path);
            return true;
        }

        LastPersistenceError = error;
        try
        {
            throw new InvalidOperationException(error ?? "Failed to save mod profiles.");
        }
        catch (Exception ex)
        {
            ModLogger.Error($"Failed to save mod profiles to '{path}':\n{ex}");
        }

        return false;
    }

    public static void LoadProfiles()
    {
        try
        {
            ResetState();
            string savePath = SavePath;
            if (File.Exists(savePath))
                BackupExistingSaveOnce(savePath, ProfileBackupReason.RunStart, out _);

            ApplySaveData(ProfileSaveStorage.LoadOrDefault(savePath));
            if (File.Exists(savePath))
                ConfigPaths.SetActiveConfigExtensionFromPath(savePath);
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

    internal static bool BackupExistingSave(ProfileBackupReason reason)
    {
        return TryBackupExistingSave(reason, out _);
    }

    internal static bool TryBackupExistingSave(ProfileBackupReason reason, out string backupPath)
    {
        return BackupExistingSave(SavePath, reason, out backupPath);
    }

    internal static bool TryBackupResumeOnce(out string backupPath)
    {
        return BackupExistingSaveOnce(SavePath, ProfileBackupReason.Resume, out backupPath);
    }

    internal static bool TryRestoreProfileBackup(string backupPath, out string? error)
    {
        error = null;
        var previousState = State;

        if (!TryReadProfileSaveData(backupPath, out var restoredState, out error))
        {
            LastBackupError = error;
            return false;
        }

        ApplySaveData(restoredState);
        if (SaveInMemoryState())
        {
            LastBackupError = null;
            return true;
        }

        error = LastPersistenceError ?? ErrorRestoredProfileSaveCouldNotBeWritten;
        LastBackupError = error;
        ApplySaveData(previousState);
        SaveInMemoryState();
        return false;
    }

    internal static bool TryExportModList(IEnumerable<InstalledModExportInput> mods, out string exportPath)
    {
        var rows = ModListExportBuilder.BuildRows(mods, ModGroups, UnassignedGroupName, ModAnnotations);
        if (ModListExportBuilder.TryWriteCsv(ExportDirectory, rows, DateTimeOffset.UtcNow, out exportPath, out string? error))
        {
            MirrorCloudBackup(CloudBackupKind.ModList, exportPath);
            return true;
        }

        LastPersistenceError = error;
        if (!string.IsNullOrEmpty(error))
            ModLogger.Error($"Failed to export mod list: {error}");

        return false;
    }

    internal static bool TryExportCurrentModList(out string exportPath)
    {
        var inputs = ModListExportInputCollector.Collect(ConfigExtensions);
        return TryExportModList(inputs, out exportPath);
    }

    internal static bool TryReadLogViewerContent(out string title, out string content, out string logPath, out string? error)
    {
        return LogViewerService.TryReadLatestLog(
            EnumerateLogCandidatePaths(),
            LogViewerService.DefaultMaxLines,
            LogViewerService.DefaultMaxChars,
            out title,
            out content,
            out logPath,
            out error);
    }

    internal static bool ShouldShowTutorial(string currentVersion)
    {
        return TutorialStateRules.ShouldShowTutorial(Tutorial, currentVersion);
    }

    internal static void MarkTutorialSeenAndSave(string currentVersion)
    {
        TutorialStateRules.MarkSeen(Tutorial, currentVersion);
        SaveInMemoryState();
    }

    internal static bool SaveCloudBackupDirectory(string directory)
    {
        CloudBackups = CloudBackupSettingsRules.WithDirectory(CloudBackups, directory);
        return SaveInMemoryState();
    }

    private static bool BackupExistingSave(string savePath, ProfileBackupReason reason)
    {
        return BackupExistingSave(savePath, reason, out _);
    }

    private static void ApplySaveData(ProfileSaveData saveData)
    {
        State = ProfileSaveStorage.Normalize(saveData);
        NormalizeProfileIndex();
    }

    private static bool TryReadProfileSaveData(string path, out ProfileSaveData saveData, out string? error)
    {
        return ProfileSaveStorage.TryReadExisting(path, out saveData, out error);
    }

    private static bool BackupExistingSave(string savePath, ProfileBackupReason reason, out string backupPath)
    {
        bool backedUpSettings = BackupCurrentModSettings(reason, out string settingsBackupPath);
        if (ProfileBackupService.TryBackupExistingSave(savePath, reason, DateTimeOffset.UtcNow, out backupPath, out string? error))
        {
            LastBackupError = null;
            MirrorCloudBackup(CloudBackupKind.ProfileSettings, backupPath);
            if (reason != ProfileBackupReason.Manual &&
                !ProfileBackupService.TryPruneAutomaticBackups(savePath, ConfigExtensions, AutomaticBackupRetentionCount, out string? pruneError) &&
                !string.IsNullOrEmpty(pruneError))
            {
                ModLogger.Error($"Failed to prune automatic profile backups: {pruneError}");
            }
            return true;
        }

        if (backedUpSettings && string.IsNullOrEmpty(error))
        {
            LastBackupError = null;
            backupPath = settingsBackupPath;
            return true;
        }

        LastBackupError = error;
        if (!string.IsNullOrEmpty(error))
            ModLogger.Error($"Failed to create {reason} profile backup for '{savePath}': {error}");

        return false;
    }

    private static void MirrorCloudBackup(CloudBackupKind kind, string sourcePath)
    {
#if BETTERMODMENU_CLOUD_FEATURES
        if (!CloudBackupService.TryMirrorFile(CloudBackups, kind, sourcePath, out _, out string? error) && !string.IsNullOrEmpty(error))
            ModLogger.Error($"Failed to mirror BetterModMenu backup to cloud directory: {error}");
#endif
    }

    private static bool BackupCurrentModSettings(ProfileBackupReason reason, out string settingsBackupPath)
    {
        var inputs = ModListExportInputCollector.Collect(ConfigExtensions);
        if (ModSettingsBackupService.TryWriteSnapshot(BackupDirectory, inputs, reason, DateTimeOffset.UtcNow, out settingsBackupPath, out string? error))
        {
            MirrorCloudBackup(CloudBackupKind.ModSettings, settingsBackupPath);
            if (reason != ProfileBackupReason.Manual &&
                !ModSettingsBackupService.TryPruneAutomaticBackups(BackupDirectory, AutomaticBackupRetentionCount, out string? pruneError) &&
                !string.IsNullOrEmpty(pruneError))
            {
                ModLogger.Error($"Failed to prune automatic mod-settings backups: {pruneError}");
            }
            return true;
        }

        if (!string.IsNullOrEmpty(error))
            ModLogger.Error($"Failed to create {reason} mod-settings backup: {error}");

        return false;
    }

    private static bool BackupExistingSaveOnce(string savePath, ProfileBackupReason reason, out string backupPath)
    {
        backupPath = string.Empty;
        if (!BackupTriggerRules.ShouldCreateAutomaticBackup(AutomaticBackupsThisProcess, reason))
            return false;

        if (!BackupExistingSave(savePath, reason, out backupPath))
            return false;

        BackupTriggerRules.MarkAutomaticBackupCreated(AutomaticBackupsThisProcess, reason);
        return true;
    }

    private static IEnumerable<string> EnumerateLogCandidatePaths()
    {
        string? configDirectory = Path.GetDirectoryName(SavePath);
        string assemblyDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? string.Empty;
        return LogViewerService.BuildCandidatePaths(new[] { configDirectory, assemblyDirectory });
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

    public static void BuildWorkshopTagCache()
    {
        WorkshopTags.Reset();
        WorkshopTagCacheAttempted = ProfileWorkshopTagCacheBuilder.Rebuild(ModWorkshopTagsCache, ModLogger);
        if (WorkshopTagCacheAttempted && ProfileWorkshopTagCacheBuilder.GetInstallations() is { } installations)
            WorkshopTags.Seed(installations, ModWorkshopTagsCache);
    }

    public static void BuildWorkshopTagCacheIfNeeded() => BuildWorkshopTagCacheIfNeeded(ModNameStyles);

    private static void BuildWorkshopTagCacheIfNeeded(ModNameStyleSettings settings)
    {
        if (WorkshopTagCacheAttempted || !ModNameStyleRules.RequiresWorkshopTags(settings))
            return;

        BuildWorkshopTagCache();
    }

    internal static bool RequestWorkshopTags(ModNameStyleSettings settings, bool retryFailure = false)
    {
        if (!ModNameStyleRules.RequiresWorkshopTags(settings))
            return false;
        var installations = ProfileWorkshopTagCacheBuilder.GetInstallations();
        if (installations == null || !WorkshopTags.Request(installations, retryFailure))
            return false;

        bool hadCachedTags = ModWorkshopTagsCache.Count > 0;
        ModWorkshopTagsCache.Clear();
        WorkshopTagCacheAttempted = false;
        return hadCachedTags;
    }

    internal static bool PublishWorkshopTags()
    {
        if (WorkshopTags.TryPublish(out string? error))
        {
            ModWorkshopTagsCache = new Dictionary<string, List<string>>(WorkshopTags.Tags, StringComparer.Ordinal);
            WorkshopTagCacheAttempted = true;
            return true;
        }
        if (error != null)
            ModLogger.Error($"Failed to read Steam Workshop tags: {error}");
        return false;
    }

    public static IEnumerable<string> GetWorkshopTags(string modId)
    {
        if (!string.IsNullOrWhiteSpace(modId) && ModWorkshopTagsCache.TryGetValue(modId, out var tags))
        {
            foreach (string tag in tags)
                yield return tag;
        }
    }
}
