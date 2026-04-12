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
        ModdingScreenContext.TrackCurrentScreen(__instance);
        var session = ModdingScreenContext.GetSession(__instance);
        ModdingScreenChromeOps.PrepareScreen(
            __instance,
            session,
            OnProfileSelected,
            OnNewProfilePressed,
            OnRenameProfilePressed,
            OnDelProfilePressed,
            OnPortableModeToggled,
            OnAddGroupRequested);

        RefreshProfileDropdown();
        RefreshGroupsUI();
        UpdateChromeLayout(__instance);
        Callable.From(() =>
        {
            if (IsCurrentScreen(__instance))
            {
                RefreshGroupsUI();
                UpdateChromeLayout(__instance);
            }
        }).CallDeferred();
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
        ModdingScreenChromeOps.UpdateLayout(screen, ModdingScreenContext.GetSession(screen));
    }

    private static void OnPortableModeToggled(bool isToggled)
    {
        try
        {
            if (!ModdingScreenStateOps.SetPortableMode(isToggled))
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
        if (!ModdingScreenStateOps.TryAddGroup(groupName))
            return false;

        RefreshGroupsUI();
        return true;
    }

    public static void RefreshGroupsUI()
    {
        if (!TryGetCurrentScreen(out var screen) || screen == null)
            return;

        ModdingScreenChromeOps.RefreshGroupsUI(
            screen,
            ModdingScreenContext.GetSession(screen),
            RefreshGroupsUI,
            RenameGroup,
            MoveGroup,
            ToggleAllInGroup);
    }

    private static void MoveGroup(string grpName, int direction)
    {
        if (ModdingScreenStateOps.TryMoveGroup(grpName, direction))
            RefreshGroupsUI();
    }

    private static void RenameGroup(string oldName)
    {
        if (!TryGetCurrentScreen(out var screen) || screen == null)
            return;

        ModdingScreenDialogs.ShowRenameGroupDialog(screen, oldName, newName =>
        {
            if (ModdingScreenStateOps.TryRenameGroup(oldName, newName))
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

        using var autoSaveSuppression = new ModdingScreenSuppressionScope(screen, suppressAutoSave: true, suppressTickboxes: false);
        using var tickboxSuppression = new ModdingScreenSuppressionScope(screen, suppressAutoSave: false, suppressTickboxes: true);
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
            using var autoSaveSuppression = new ModdingScreenSuppressionScope(screen, suppressAutoSave: true, suppressTickboxes: false);
            using var tickboxSuppression = new ModdingScreenSuppressionScope(screen, suppressAutoSave: false, suppressTickboxes: true);
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
