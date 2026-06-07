using System;
using BetterModMenu.Data;
using Godot;
using MegaCrit.Sts2.Core.Nodes.Screens.ModdingScreen;

namespace BetterModMenu.Patches;

internal static class ModdingScreenActionFlow
{
    public static void Ready(
        NModdingScreen screen,
        Action<long> onProfileSelected,
        Action onNewProfilePressed,
        Action onRenameProfilePressed,
        Action onDeleteProfilePressed,
        Action<bool> onPortableModeToggled,
        Action onManualBackupPressed,
        Action onLoadBackupPressed,
        Action onExportModListPressed,
        Action onViewLogsPressed,
        Action onTutorialPressed,
        Action onCloudBackupPressed,
        Func<string, bool> onAddGroupRequested,
        Action refreshGroupsUI,
        Action<string> renameGroup,
        Action<string, int> moveGroup,
        Action<string, bool> toggleAllInGroup)
    {
        ModdingScreenContext.TrackCurrentScreen(screen);
        var session = ModdingScreenContext.GetSession(screen);
        ModdingScreenChromeOps.PrepareScreen(
            screen,
            session,
            onProfileSelected,
            onNewProfilePressed,
            onRenameProfilePressed,
            onDeleteProfilePressed,
            onPortableModeToggled,
            onManualBackupPressed,
            onLoadBackupPressed,
            onExportModListPressed,
            onViewLogsPressed,
            onTutorialPressed,
            onCloudBackupPressed,
            onAddGroupRequested);

        RefreshProfileDropdown(screen);
        RefreshGroupsUI(screen, refreshGroupsUI, renameGroup, moveGroup, toggleAllInGroup);
        UpdateChromeLayout(screen);
        Callable.From(() =>
        {
            if (ModdingScreenContext.IsCurrentScreen(screen))
            {
                RefreshGroupsUI(screen, refreshGroupsUI, renameGroup, moveGroup, toggleAllInGroup);
                UpdateChromeLayout(screen);
                ProfileManager.TryBackupResumeOnce(out _);
                MaybeShowTutorial(screen);
            }
        }).CallDeferred();
    }

    public static void UpdateChromeLayout(NModdingScreen screen)
    {
        ModdingScreenChromeOps.UpdateLayout(screen, ModdingScreenContext.GetSession(screen));
    }

    public static void RefreshGroupsUI(
        NModdingScreen screen,
        Action refreshGroupsUI,
        Action<string> renameGroup,
        Action<string, int> moveGroup,
        Action<string, bool> toggleAllInGroup)
    {
        ModdingScreenChromeOps.RefreshGroupsUI(
            screen,
            ModdingScreenContext.GetSession(screen),
            refreshGroupsUI,
            renameGroup,
            moveGroup,
            toggleAllInGroup);
    }

    public static void RefreshProfileDropdown(NModdingScreen screen)
    {
        var topBarControls = ModdingScreenContext.GetSession(screen).TopBarControls;
        if (topBarControls == null || !GodotObject.IsInstanceValid(topBarControls.ProfileDropdown))
            return;

        ProfileManager.NormalizeProfileIndex();
        topBarControls.ProfileDropdown.Clear();
        for (int i = 0; i < ProfileManager.Profiles.Count; i++)
            topBarControls.ProfileDropdown.AddItem(ProfileManager.Profiles[i].Name, i);

        topBarControls.ProfileDropdown.Select(ProfileManager.CurrentProfileIndex);
        UpdateChromeLayout(screen);
    }

    private static void MaybeShowTutorial(NModdingScreen screen)
    {
        string currentVersion = ModVersionProvider.CurrentVersion;
        if (!ProfileManager.ShouldShowTutorial(currentVersion))
            return;

        ModdingScreenDialogs.ShowTutorialDialog(screen, currentVersion, () =>
        {
            ProfileManager.MarkTutorialSeenAndSave(currentVersion);
        });
    }
}
