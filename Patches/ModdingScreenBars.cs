using System;
using Godot;

namespace BetterModMenu.Patches;

internal sealed class TopBarControls(
    HBoxContainer bar,
    OptionButton profileDropdown,
    Button newProfileButton,
    Button renameProfileButton,
    Button deleteProfileButton)
{
    public HBoxContainer Bar { get; } = bar;
    public OptionButton ProfileDropdown { get; } = profileDropdown;
    public Button NewProfileButton { get; } = newProfileButton;
    public Button RenameProfileButton { get; } = renameProfileButton;
    public Button DeleteProfileButton { get; } = deleteProfileButton;

    public void SetCompact(bool isCompact)
    {
        var presentation = ModdingScreenLayoutRules.GetTopBarPresentation(isCompact);
        NewProfileButton.Text = presentation.NewProfile.Text;
        NewProfileButton.TooltipText = presentation.NewProfile.TooltipText;
        RenameProfileButton.Text = presentation.RenameProfile.Text;
        RenameProfileButton.TooltipText = presentation.RenameProfile.TooltipText;
        DeleteProfileButton.Text = presentation.DeleteProfile.Text;
        DeleteProfileButton.TooltipText = presentation.DeleteProfile.TooltipText;

        var minSize = new Vector2(presentation.ButtonWidth, ModdingScreenConstants.ToolbarControlHeight);
        NewProfileButton.CustomMinimumSize = minSize;
        RenameProfileButton.CustomMinimumSize = minSize;
        DeleteProfileButton.CustomMinimumSize = minSize;
    }
}

internal sealed class GroupBarControls(
    VBoxContainer bar,
    HBoxContainer primaryRow,
    HBoxContainer secondaryRow,
    CheckButton portableToggle,
    Button backupButton,
    Button loadBackupButton,
    Button exportButton,
    Button logsButton,
    Button tutorialButton,
    Button? cloudButton,
    Label groupLabel,
    LineEdit newGroupInput,
    Button newGroupButton,
    Control flexibleSpacer)
{
    public VBoxContainer Bar { get; } = bar;
    public HBoxContainer PrimaryRow { get; } = primaryRow;
    public HBoxContainer SecondaryRow { get; } = secondaryRow;
    public CheckButton PortableToggle { get; } = portableToggle;
    public Button BackupButton { get; } = backupButton;
    public Button LoadBackupButton { get; } = loadBackupButton;
    public Button ExportButton { get; } = exportButton;
    public Button LogsButton { get; } = logsButton;
    public Button TutorialButton { get; } = tutorialButton;
    public Button? CloudButton { get; } = cloudButton;
    public Label GroupLabel { get; } = groupLabel;
    public LineEdit NewGroupInput { get; } = newGroupInput;
    public Button NewGroupButton { get; } = newGroupButton;
    public Control FlexibleSpacer { get; } = flexibleSpacer;

    public void SetCompact(bool isCompact)
    {
        MoveChild(PortableToggle, PrimaryRow);
        MoveChild(BackupButton, PrimaryRow);
        MoveChild(LoadBackupButton, PrimaryRow);
        MoveChild(ExportButton, PrimaryRow);
        MoveChild(LogsButton, PrimaryRow);
        MoveChild(TutorialButton, PrimaryRow);
        if (CloudButton != null)
            MoveChild(CloudButton, PrimaryRow);
        MoveChild(FlexibleSpacer, PrimaryRow);
        FlexibleSpacer.Visible = !isCompact;

        if (isCompact)
        {
            SecondaryRow.Visible = true;
            MoveChild(GroupLabel, SecondaryRow);
            MoveChild(NewGroupInput, SecondaryRow);
            MoveChild(NewGroupButton, SecondaryRow);
            NewGroupInput.CustomMinimumSize = new Vector2(ModdingScreenConstants.GroupInputCompactWidth, ModdingScreenConstants.ToolbarControlHeight);
        }
        else
        {
            SecondaryRow.Visible = false;
            MoveChild(GroupLabel, PrimaryRow);
            MoveChild(NewGroupInput, PrimaryRow);
            MoveChild(NewGroupButton, PrimaryRow);
            NewGroupInput.CustomMinimumSize = new Vector2(ModdingScreenConstants.GroupInputWideWidth, ModdingScreenConstants.ToolbarControlHeight);
        }
    }

    private static void MoveChild(Control child, Container target)
    {
        if (child.GetParent() == target)
            return;

        child.Reparent(target);
    }
}

internal static class ModdingScreenBars
{
    public static TopBarControls CreateTopBar(
        Action<long> onProfileSelected,
        Action onNewProfilePressed,
        Action onRenameProfilePressed,
        Action onDeleteProfilePressed)
    {
        var topBar = new HBoxContainer();
        topBar.Name = "BetterModMenuTopBar";

        var profileLabel = new Label
        {
            Text = "Profile:",
            TooltipText = "Saved enabled/disabled mod setup."
        };
        ModdingScreenVanillaStyle.ApplyLabel(profileLabel, muted: true);
        topBar.AddChild(profileLabel);

        var profileDropdown = new OptionButton
        {
            CustomMinimumSize = new Vector2(120, 0),
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
        };
        ModdingScreenVanillaStyle.ApplyOptionButton(profileDropdown);
        profileDropdown.ItemSelected += index => onProfileSelected(index);
        topBar.AddChild(profileDropdown);

        var newProfileBtn = new Button { Text = "+ New", CustomMinimumSize = new Vector2(ModdingScreenConstants.TopBarButtonMinWidth, 0) };
        ModdingScreenVanillaStyle.ApplyButton(newProfileBtn);
        newProfileBtn.Pressed += onNewProfilePressed;
        topBar.AddChild(newProfileBtn);

        var renameProfileBtn = new Button { Text = "Rename", CustomMinimumSize = new Vector2(ModdingScreenConstants.TopBarButtonMinWidth, 0) };
        ModdingScreenVanillaStyle.ApplyButton(renameProfileBtn);
        renameProfileBtn.Pressed += onRenameProfilePressed;
        topBar.AddChild(renameProfileBtn);

        var delProfileBtn = new Button { Text = "Del", CustomMinimumSize = new Vector2(ModdingScreenConstants.TopBarButtonMinWidth, 0) };
        ModdingScreenVanillaStyle.ApplyButton(delProfileBtn);
        delProfileBtn.Pressed += onDeleteProfilePressed;
        topBar.AddChild(delProfileBtn);

        return new TopBarControls(topBar, profileDropdown, newProfileBtn, renameProfileBtn, delProfileBtn);
    }

    public static GroupBarControls CreateGroupBar(
        bool portableModeEnabled,
        Action<bool> onPortableModeToggled,
        Action onManualBackupPressed,
        Action onLoadBackupPressed,
        Action onExportModListPressed,
        Action onViewLogsPressed,
        Action onTutorialPressed,
        Action onCloudBackupPressed,
        Func<string, bool> onAddGroupRequested)
    {
        var groupBar = new VBoxContainer { Name = "BetterModMenuGroupBar" };
        var primaryRow = new HBoxContainer();
        var secondaryRow = new HBoxContainer();
        groupBar.AddChild(primaryRow);
        groupBar.AddChild(secondaryRow);

        var portableToggle = new CheckButton
        {
            Text = "Portable Mode",
            ButtonPressed = portableModeEnabled,
            TooltipText = "Portable Mode: save Better Mod Menu data beside the mod files instead of the normal game save folder."
        };
        ModdingScreenVanillaStyle.ApplyButton(portableToggle);
        portableToggle.Toggled += isToggled => onPortableModeToggled(isToggled);
        primaryRow.AddChild(portableToggle);

        var backupButton = new Button
        {
            Text = "Backup",
            TooltipText = "Backup: save copies of your profiles, groups, and current enabled-mod settings."
        };
        ModdingScreenVanillaStyle.ApplyButton(backupButton);
        backupButton.Pressed += onManualBackupPressed;
        primaryRow.AddChild(backupButton);

        var loadBackupButton = new Button
        {
            Text = "Load",
            TooltipText = "Load: choose a Better Mod Menu profile and group backup to restore."
        };
        ModdingScreenVanillaStyle.ApplyButton(loadBackupButton);
        loadBackupButton.Pressed += onLoadBackupPressed;
        primaryRow.AddChild(loadBackupButton);

        var exportButton = new Button
        {
            Text = "CSV",
            TooltipText = "CSV: export a spreadsheet-friendly installed-mod list with versions, enabled state, and group names."
        };
        ModdingScreenVanillaStyle.ApplyButton(exportButton);
        exportButton.Pressed += onExportModListPressed;
        primaryRow.AddChild(exportButton);

        var logsButton = new Button { Text = "Logs", TooltipText = "Open recent BetterModMenu/TTSMM log output" };
        ModdingScreenVanillaStyle.ApplyButton(logsButton);
        logsButton.Pressed += onViewLogsPressed;
        primaryRow.AddChild(logsButton);

        var tutorialButton = new Button { Text = "Help", TooltipText = "Help: explain what each Better Mod Menu control does." };
        ModdingScreenVanillaStyle.ApplyButton(tutorialButton);
        tutorialButton.Pressed += onTutorialPressed;
        primaryRow.AddChild(tutorialButton);

        Button? cloudButton = null;
#if BETTERMODMENU_CLOUD_FEATURES
        cloudButton = new Button
        {
            Text = "Cloud",
            TooltipText = "Cloud: choose a synced folder where backups and CSV exports should also be copied."
        };
        ModdingScreenVanillaStyle.ApplyButton(cloudButton);
        cloudButton.Pressed += onCloudBackupPressed;
        primaryRow.AddChild(cloudButton);
#endif

        var flexibleSpacer = new Control { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        primaryRow.AddChild(flexibleSpacer);

        var groupLabel = new Label
        {
            Text = "Group:",
            TooltipText = "Custom labels for organizing mods."
        };
        ModdingScreenVanillaStyle.ApplyLabel(groupLabel, muted: true);
        primaryRow.AddChild(groupLabel);

        var newGroupInput = new LineEdit
        {
            PlaceholderText = "Group name...",
            TooltipText = "Type a new group name, then press Add.",
            CustomMinimumSize = new Vector2(ModdingScreenConstants.GroupInputWideWidth, 0)
        };
        ModdingScreenVanillaStyle.ApplyLineEdit(newGroupInput);
        primaryRow.AddChild(newGroupInput);

        var newGroupBtn = new Button
        {
            Text = "+ Add",
            TooltipText = "Add this group name to the mod list."
        };
        ModdingScreenVanillaStyle.ApplyButton(newGroupBtn);
        newGroupBtn.Pressed += () =>
        {
            if (onAddGroupRequested(newGroupInput.Text))
                newGroupInput.Text = "";
        };
        primaryRow.AddChild(newGroupBtn);

        return new GroupBarControls(groupBar, primaryRow, secondaryRow, portableToggle, backupButton, loadBackupButton, exportButton, logsButton, tutorialButton, cloudButton, groupLabel, newGroupInput, newGroupBtn, flexibleSpacer);
    }
}
