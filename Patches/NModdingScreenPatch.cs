using HarmonyLib;
using Godot;
using MegaCrit.Sts2.Core.Nodes.Screens.ModdingScreen;
using BetterModMenu.Data;
using MegaCrit.Sts2.Core.Saves;

namespace BetterModMenu.Patches;

[HarmonyPatch(typeof(NModdingScreen))]
public static class NModdingScreenPatch
{
    [HarmonyPatch(nameof(NModdingScreen.OnModEnabledOrDisabled))]
    [HarmonyPostfix]
    public static void Postfix_OnModEnabledOrDisabled(NModdingScreen __instance)
    {
        if (!ModdingScreenContext.IsAutoSaveSuppressed(__instance))
            ProfileManager.SnapshotCurrentStateAndSave();
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
                "Backup Created",
                "Saved copies of your Better Mod Menu profiles, groups, and current enabled-mod settings.\n\nBackup file:\n" + backupPath);
            return;
        }

        string message = string.IsNullOrWhiteSpace(ProfileManager.LastBackupError)
            ? "There is no Better Mod Menu save file to back up yet. Make or switch a profile first, then try Backup again."
            : "Backup could not finish. Your current settings were not changed.\n\nError:\n" + ProfileManager.LastBackupError;
        ModdingScreenDialogs.ShowInfoDialog(screen, "Backup Not Created", message);
        ProfileManager.ModLogger.Error("Manual backup was requested but no existing profile save could be backed up.");
    }

    private static void OnLoadBackupPressed()
    {
        if (!TryGetCurrentScreen(out var screen) || screen == null)
            return;

        if (!ProfileBackupService.TryListBackups(ProfileManager.SavePath, ProfileManager.ConfigExtensions, out var backups, out string? error))
        {
            string message = string.IsNullOrWhiteSpace(error)
                ? "No Better Mod Menu profile backups were found yet. Use Backup first, then Load can restore one."
                : "The backup folder could not be checked.\n\nError:\n" + error;
            ModdingScreenDialogs.ShowInfoDialog(screen, "Backup Not Found", message);
            return;
        }

        ModdingScreenDialogs.ShowBackupSelectionDialog(screen, backups, backupPath => RestoreBackup(screen, backupPath));
    }

    private static void RestoreBackup(NModdingScreen screen, string backupPath)
    {
        if (!ProfileManager.TryRestoreProfileBackup(backupPath, out string? error))
        {
            string message = string.IsNullOrWhiteSpace(error)
                ? "The backup could not be loaded. Your current profiles were not changed."
                : "The backup could not be loaded. Your current profiles were not changed.\n\nError:\n" + error;
            ModdingScreenDialogs.ShowInfoDialog(screen, "Backup Not Loaded", message);
            return;
        }

        RefreshProfileDropdown();
        ApplyProfileSelection(ProfileManager.CurrentProfileIndex, snapshotCurrentProfile: false);
        ModdingScreenDialogs.ShowInfoDialog(
            screen,
            "Backup Loaded",
            "Loaded this Better Mod Menu profile and group backup.\n\nRestart the game for every change to apply.\n\nBackup file:\n" + backupPath);
    }

    private static void OnExportModListPressed()
    {
        if (!TryGetCurrentScreen(out var screen) || screen == null)
            return;

        if (!ProfileManager.TryExportCurrentModList(out string exportPath))
        {
            string message = string.IsNullOrWhiteSpace(ProfileManager.LastPersistenceError)
                ? "The installed mod list could not be exported. Your mod setup was not changed."
                : "The installed mod list could not be exported. Your mod setup was not changed.\n\nError:\n" + ProfileManager.LastPersistenceError;
            ModdingScreenDialogs.ShowInfoDialog(screen, "Export Failed", message);
            return;
        }

        ModdingScreenDialogs.ShowInfoDialog(
            screen,
            "CSV Export Created",
            "Created a spreadsheet-friendly mod list with mod names, versions, enabled state, group names, and Steam Workshop links when available.\n\nCSV file:\n" + exportPath);
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

        ModdingScreenDialogs.ShowInfoDialog(screen, "Logs Not Found", error ?? "No known log file could be opened.");
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
                    ? "Cloud mirroring is off. Backups and CSV exports will stay only in the normal Better Mod Menu folders."
                    : "Cloud mirroring is on. New backups and CSV exports will also be copied to:\n" + ProfileManager.CloudBackups.Directory;
                ModdingScreenDialogs.ShowInfoDialog(screen, "Cloud Backups", message);
                return;
            }

            ModdingScreenDialogs.ShowInfoDialog(screen, "Cloud Backups", "Cloud backup settings could not be saved. Your previous cloud setting is still in use.\n\nError:\n" + ProfileManager.LastPersistenceError);
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
