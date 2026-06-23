using HarmonyLib;
using Godot;
using MegaCrit.Sts2.Core.Nodes.Screens.ModdingScreen;
using BetterModMenu.Data;
using MegaCrit.Sts2.Core.Saves;

namespace BetterModMenu.Patches;

[HarmonyPatch(typeof(NModdingScreen))]
public static class NModdingScreenPatch
{
    private static string T(string key, string fallback) => ModdingScreenText.Get(key, fallback);

    private static string F(string key, string fallback, params object[] args) => ModdingScreenText.Format(key, fallback, args);

    [HarmonyPatch(nameof(NModdingScreen.OnModEnabledOrDisabled))]
    [HarmonyPostfix]
    public static void Postfix_OnModEnabledOrDisabled(NModdingScreen __instance)
    {
        if (!ModdingScreenContext.IsAutoSaveSuppressed(__instance))
            ProfileManager.SnapshotCurrentStateAndSave();
    }

    [HarmonyPatch(nameof(NModdingScreen.OnRowSelected))]
    [HarmonyPostfix]
    public static void Postfix_OnRowSelected(NModdingScreen __instance, NModMenuRow row)
    {
        var session = ModdingScreenContext.GetSession(__instance);
        session.SelectedModId = row.Mod?.manifest?.id ?? string.Empty;
        ModdingScreenInfoPanelOps.Refresh(__instance, session);
    }

    [HarmonyPatch(nameof(NModdingScreen._Ready))]
    [HarmonyPostfix]
    public static void Postfix_Ready(NModdingScreen __instance)
    {
        ModdingScreenActionFlow.Ready(
            __instance,
            OnProfileSelected,
            OnNewProfilePressed,
            OnRenameProfilePressed,
            OnDelProfilePressed,
            OnPortableModeToggled,
            OnManualBackupPressed,
            OnLoadBackupPressed,
            OnExportModListPressed,
            OnViewLogsPressed,
            OnStyleEditorPressed,
            OnTutorialPressed,
            OnCloudBackupPressed,
            OnAddGroupRequested,
            RefreshGroupsUI,
            RenameGroup,
            MoveGroup,
            ToggleAllInGroup);
    }

    [HarmonyPatch(nameof(NModdingScreen._ExitTree))]
    [HarmonyPostfix]
    public static void Postfix_ExitTree(NModdingScreen __instance)
    {
        ModdingScreenContext.ReleaseScreen(__instance);
    }

    public static bool IsCurrentScreen(NModdingScreen? screen)
    {
        return ModdingScreenContext.IsCurrentScreen(screen);
    }

    internal static bool IsTickboxHandlerSuppressed(NModdingScreen screen)
    {
        return ModdingScreenContext.IsTickboxHandlerSuppressed(screen);
    }

    private static bool TryGetCurrentScreen(out NModdingScreen? screen)
    {
        return ModdingScreenContext.TryGetCurrentScreen(out screen);
    }

    private static void UpdateChromeLayout(NModdingScreen screen)
    {
        ModdingScreenActionFlow.UpdateChromeLayout(screen);
    }

    private static void OnPortableModeToggled(bool isToggled)
    {
        try
        {
            if (!ModdingPortableModeOps.SetPortableMode(isToggled))
                ProfileManager.ModLogger.Error("Portable mode state changed in the UI but could not be persisted.");
        }
        catch (System.Exception ex)
        {
            string action = isToggled ? "enable" : "disable";
            ProfileManager.ModLogger.Error($"Failed to {action} portable mode: {ex}");
        }
    }

    private static bool OnAddGroupRequested(string groupName)
    {
        if (!ModdingGroupStateOps.TryAddGroup(groupName))
            return false;

        RefreshGroupsUI();
        return true;
    }

    private static void OnManualBackupPressed()
    {
        if (!TryGetCurrentScreen(out var screen) || screen == null)
            return;

        if (ProfileManager.TryBackupExistingSave(ProfileBackupReason.Manual, out string backupPath))
        {
            ModdingScreenDialogs.ShowInfoDialog(
                screen,
                T(BmmText.BackupCreatedTitle, "Backup Created"),
                F(BmmText.BackupCreatedMessageFormat, "Saved copies of your Better Mod Menu profiles, groups, and current enabled-mod settings.\n\nBackup file:\n{0}", backupPath));
            return;
        }

        string backupError = ModdingScreenText.LocalizeKnownError(ProfileManager.LastBackupError);
        string message = string.IsNullOrWhiteSpace(ProfileManager.LastBackupError)
            ? T(BmmText.BackupNoSaveMessage, "There is no Better Mod Menu save file to back up yet. Make or switch a profile first, then try Backup again.")
            : F(BmmText.BackupFailedMessageFormat, "Backup could not finish. Your current settings were not changed.\n\nError:\n{0}", backupError);
        ModdingScreenDialogs.ShowInfoDialog(screen, T(BmmText.BackupNotCreatedTitle, "Backup Not Created"), message);
        ProfileManager.ModLogger.Error("Manual backup was requested but no existing profile save could be backed up.");
    }

    private static void OnLoadBackupPressed()
    {
        if (!TryGetCurrentScreen(out var screen) || screen == null)
            return;

        if (!ProfileBackupService.TryListBackups(ProfileManager.SavePath, ProfileManager.ConfigExtensions, out var backups, out string? error))
        {
            string backupListError = ModdingScreenText.LocalizeKnownError(error);
            string message = string.IsNullOrWhiteSpace(error)
                ? T(BmmText.BackupNoneFoundMessage, "No Better Mod Menu profile backups were found yet. Use Backup first, then Load can restore one.")
                : F(BmmText.BackupFolderErrorFormat, "The backup folder could not be checked.\n\nError:\n{0}", backupListError);
            ModdingScreenDialogs.ShowInfoDialog(screen, T(BmmText.BackupNotFoundTitle, "Backup Not Found"), message);
            return;
        }

        ModdingScreenDialogs.ShowBackupSelectionDialog(screen, backups, backupPath => RestoreBackup(screen, backupPath));
    }

    private static void RestoreBackup(NModdingScreen screen, string backupPath)
    {
        if (!ProfileManager.TryRestoreProfileBackup(backupPath, out string? error))
        {
            string restoreError = ModdingScreenText.LocalizeKnownError(error);
            string message = string.IsNullOrWhiteSpace(error)
                ? T(BmmText.BackupNotLoadedMessage, "The backup could not be loaded. Your current profiles were not changed.")
                : F(BmmText.BackupNotLoadedErrorFormat, "The backup could not be loaded. Your current profiles were not changed.\n\nError:\n{0}", restoreError);
            ModdingScreenDialogs.ShowInfoDialog(screen, T(BmmText.BackupNotLoadedTitle, "Backup Not Loaded"), message);
            return;
        }

        RefreshProfileDropdown();
        ApplyProfileSelection(ProfileManager.CurrentProfileIndex, snapshotCurrentProfile: false);
        ModdingScreenDialogs.ShowInfoDialog(
            screen,
            T(BmmText.BackupLoadedTitle, "Backup Loaded"),
            F(BmmText.BackupLoadedMessageFormat, "Loaded this Better Mod Menu profile and group backup.\n\nRestart the game for every change to apply.\n\nBackup file:\n{0}", backupPath));
    }

    private static void OnExportModListPressed()
    {
        if (!TryGetCurrentScreen(out var screen) || screen == null)
            return;

        if (!ProfileManager.TryExportCurrentModList(out string exportPath))
        {
            string exportError = ModdingScreenText.LocalizeKnownError(ProfileManager.LastPersistenceError);
            string message = string.IsNullOrWhiteSpace(ProfileManager.LastPersistenceError)
                ? T(BmmText.ExportFailedMessage, "The installed mod list could not be exported. Your mod setup was not changed.")
                : F(BmmText.ExportFailedErrorFormat, "The installed mod list could not be exported. Your mod setup was not changed.\n\nError:\n{0}", exportError);
            ModdingScreenDialogs.ShowInfoDialog(screen, T(BmmText.ExportFailedTitle, "Export Failed"), message);
            return;
        }

        ModdingScreenDialogs.ShowInfoDialog(
            screen,
            T(BmmText.CsvExportCreatedTitle, "CSV Export Created"),
            F(BmmText.CsvExportCreatedMessageFormat, "Created a spreadsheet-friendly mod list with mod names, versions, enabled state, group names, and Steam Workshop links when available.\n\nCSV file:\n{0}", exportPath));
        ProfileManager.ModLogger.Info($"Exported BetterModMenu mod list to '{exportPath}'.");
    }

    private static void OnViewLogsPressed()
    {
        if (!TryGetCurrentScreen(out var screen) || screen == null)
            return;

        if (ProfileManager.TryReadLogViewerContent(out string title, out string content, out string logPath, out string? error))
        {
            ModdingScreenDialogs.ShowLogDialog(screen, title, content, logPath);
            return;
        }

        ModdingScreenDialogs.ShowInfoDialog(
            screen,
            T(BmmText.LogNotFoundTitle, "Logs Not Found"),
            string.IsNullOrWhiteSpace(error)
                ? T(BmmText.LogNotFoundGeneric, "No known log file could be opened.")
                : ModdingScreenText.LocalizeKnownError(error));
    }

    private static void OnStyleEditorPressed()
    {
        if (!TryGetCurrentScreen(out var screen) || screen == null)
            return;

        ModNameStyleEditorDialog.Show(screen, () => RefreshModNameStyles(screen));
    }

    private static void RefreshModNameStyles(NModdingScreen screen)
    {
        if (!ModdingScreenContext.IsCurrentScreen(screen))
            return;

        var session = ModdingScreenContext.GetSession(screen);
        NModMenuRowPatch.RefreshVisibleModNames(screen);
        NModInfoContainerPatch.RefreshSelectedTitle(screen, session);
    }

    private static void OnTutorialPressed()
    {
        if (!TryGetCurrentScreen(out var screen) || screen == null)
            return;

        ModdingScreenDialogs.ShowTutorialDialog(screen, ModVersionProvider.CurrentVersion, () => { });
    }

    private static void OnCloudBackupPressed()
    {
#if BETTERMODMENU_CLOUD_FEATURES
        if (!TryGetCurrentScreen(out var screen) || screen == null)
            return;

        ModdingScreenDialogs.ShowCloudBackupDialog(screen, ProfileManager.CloudBackups.Directory, directory =>
        {
            if (ProfileManager.SaveCloudBackupDirectory(directory))
            {
                string message = string.IsNullOrWhiteSpace(ProfileManager.CloudBackups.Directory)
                    ? T(BmmText.CloudOffMessage, "Cloud mirroring is off. Backups and CSV exports will stay only in the normal Better Mod Menu folders.")
                    : F(BmmText.CloudOnMessageFormat, "Cloud mirroring is on. New backups and CSV exports will also be copied to:\n{0}", ProfileManager.CloudBackups.Directory);
                ModdingScreenDialogs.ShowInfoDialog(screen, T(BmmText.CloudBackupsTitle, "Cloud Backups"), message);
                return;
            }

            ModdingScreenDialogs.ShowInfoDialog(
                screen,
                T(BmmText.CloudBackupsTitle, "Cloud Backups"),
                F(BmmText.CloudSaveFailedFormat, "Cloud backup settings could not be saved. Your previous cloud setting is still in use.\n\nError:\n{0}", ModdingScreenText.LocalizeKnownError(ProfileManager.LastPersistenceError)));
        });
#endif
    }

    public static void RefreshGroupsUI()
    {
        if (!TryGetCurrentScreen(out var screen) || screen == null)
            return;

        ModdingScreenActionFlow.RefreshGroupsUI(
            screen,
            RefreshGroupsUI,
            RenameGroup,
            MoveGroup,
            ToggleAllInGroup);
    }

    private static void MoveGroup(string grpName, int direction)
    {
        if (ModdingGroupStateOps.TryMoveGroup(grpName, direction))
            RefreshGroupsUI();
    }

    private static void RenameGroup(string oldName)
    {
        if (!TryGetCurrentScreen(out var screen) || screen == null)
            return;

        ModdingScreenDialogs.ShowRenameGroupDialog(screen, oldName, newName =>
        {
            if (ModdingGroupStateOps.TryRenameGroup(oldName, newName))
                RefreshGroupsUI();
        });
    }

    public static void MoveModOrder(string modId, int direction)
    {
        if (ModdingScreenListOps.TryMoveModOrder(modId, direction))
            RefreshGroupsUI();
    }

    private static void ToggleAllInGroup(string groupName, bool isToggled)
    {
        if (!TryGetCurrentScreen(out var screen) || screen == null)
            return;

        var modRowContainer = ModdingScreenNodeOps.GetModRowContainer(screen);
        if (modRowContainer == null)
            return;

        using var suppression = new ModdingScreenSuppressionScope(screen);
        if (ModdingScreenListOps.ApplyToggleAllInGroup(modRowContainer, groupName, isToggled))
        {
            ProfileManager.SaveInMemoryState();
            SaveManager.Instance?.SaveSettings();
            screen.OnModEnabledOrDisabled();
        }
    }

    private static void RefreshProfileDropdown()
    {
        if (!TryGetCurrentScreen(out var screen) || screen == null)
            return;

        ModdingScreenActionFlow.RefreshProfileDropdown(screen);
    }

    private static void OnProfileSelected(long index)
    {
        ApplyProfileSelection((int)index, snapshotCurrentProfile: true);
    }

    private static void ApplyProfileSelection(int index, bool snapshotCurrentProfile)
    {
        if (!ModdingScreenProfileOps.TryApplyProfileSelection(index, snapshotCurrentProfile, out var profile) || profile == null)
            return;

        RefreshProfileDropdown();

        if (TryGetCurrentScreen(out var screen) && screen != null)
        {
            using var suppression = new ModdingScreenSuppressionScope(screen);
            screen.OnModEnabledOrDisabled();

            var modRowContainer = ModdingScreenNodeOps.GetModRowContainer(screen);
            if (modRowContainer != null)
            {
                ModdingScreenProfileOps.SyncTickboxesForProfile(modRowContainer, profile);
                RefreshGroupsUI();
            }
        }
    }

    private static void OnNewProfilePressed()
    {
        ModdingScreenProfileOps.CreateNewProfileFromCurrentState();
        RefreshProfileDropdown();
        RefreshGroupsUI();
    }

    private static void OnRenameProfilePressed()
    {
        if (!TryGetCurrentScreen(out var screen) || screen == null)
            return;

        ModdingScreenDialogs.ShowRenameProfileDialog(screen, ProfileManager.CurrentProfile.Name, newName =>
        {
            if (ModdingScreenProfileOps.TryRenameCurrentProfile(newName))
            {
                RefreshProfileDropdown();
            }
        });
    }

    private static void OnDelProfilePressed()
    {
        int? replacementIndex = ModdingScreenProfileOps.DeleteCurrentProfile();
        if (replacementIndex.HasValue)
            ApplyProfileSelection(replacementIndex.Value, snapshotCurrentProfile: false);
    }
}
