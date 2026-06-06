using System;
using Godot;

namespace BetterModMenu.Patches;

internal sealed class TopBarControls
{
    public TopBarControls(HBoxContainer bar, OptionButton profileDropdown, Button newProfileButton, Button renameProfileButton, Button deleteProfileButton)
    {
        Bar = bar;
        ProfileDropdown = profileDropdown;
        NewProfileButton = newProfileButton;
        RenameProfileButton = renameProfileButton;
        DeleteProfileButton = deleteProfileButton;
    }

    public HBoxContainer Bar { get; }
    public OptionButton ProfileDropdown { get; }
    public Button NewProfileButton { get; }
    public Button RenameProfileButton { get; }
    public Button DeleteProfileButton { get; }

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

internal sealed class GroupBarControls
{
    public GroupBarControls(
        VBoxContainer bar,
        HBoxContainer primaryRow,
        HBoxContainer secondaryRow,
        CheckButton portableToggle,
        Button backupButton,
        Button exportButton,
        Button logsButton,
        Button tutorialButton,
        Button gameVersionButton,
        Button? cloudButton,
        Label groupLabel,
        LineEdit newGroupInput,
        Button newGroupButton,
        Control flexibleSpacer)
    {
        Bar = bar;
        PrimaryRow = primaryRow;
        SecondaryRow = secondaryRow;
        PortableToggle = portableToggle;
        BackupButton = backupButton;
        ExportButton = exportButton;
        LogsButton = logsButton;
        TutorialButton = tutorialButton;
        GameVersionButton = gameVersionButton;
        CloudButton = cloudButton;
        GroupLabel = groupLabel;
        NewGroupInput = newGroupInput;
        NewGroupButton = newGroupButton;
        FlexibleSpacer = flexibleSpacer;
    }

    public VBoxContainer Bar { get; }
    public HBoxContainer PrimaryRow { get; }
    public HBoxContainer SecondaryRow { get; }
    public CheckButton PortableToggle { get; }
    public Button BackupButton { get; }
    public Button ExportButton { get; }
    public Button LogsButton { get; }
    public Button TutorialButton { get; }
    public Button GameVersionButton { get; }
    public Button? CloudButton { get; }
    public Label GroupLabel { get; }
    public LineEdit NewGroupInput { get; }
    public Button NewGroupButton { get; }
    public Control FlexibleSpacer { get; }

    public void SetCompact(bool isCompact)
    {
        MoveChild(PortableToggle, PrimaryRow);
        MoveChild(BackupButton, PrimaryRow);
        MoveChild(ExportButton, PrimaryRow);
        MoveChild(LogsButton, PrimaryRow);
        MoveChild(TutorialButton, PrimaryRow);
        MoveChild(GameVersionButton, PrimaryRow);
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

        var profileLabel = new Label { Text = "Profile:" };
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
        Action onExportModListPressed,
        Action onViewLogsPressed,
        Action onTutorialPressed,
        Action onGameVersionPressed,
        Action onCloudBackupPressed,
        Func<string, bool> onAddGroupRequested)
    {
        var groupBar = new VBoxContainer { Name = "BetterModMenuGroupBar" };
        var primaryRow = new HBoxContainer();
        var secondaryRow = new HBoxContainer();
        groupBar.AddChild(primaryRow);
        groupBar.AddChild(secondaryRow);

        var portableToggle = new CheckButton { Text = "Portable Mode", ButtonPressed = portableModeEnabled };
        ModdingScreenVanillaStyle.ApplyButton(portableToggle);
        portableToggle.Toggled += isToggled => onPortableModeToggled(isToggled);
        primaryRow.AddChild(portableToggle);

        var backupButton = new Button { Text = "Backup", TooltipText = "Back up BetterModMenu profile settings" };
        ModdingScreenVanillaStyle.ApplyButton(backupButton);
        backupButton.Pressed += onManualBackupPressed;
        primaryRow.AddChild(backupButton);

        var exportButton = new Button { Text = "CSV", TooltipText = "Export installed mod list as CSV" };
        ModdingScreenVanillaStyle.ApplyButton(exportButton);
        exportButton.Pressed += onExportModListPressed;
        primaryRow.AddChild(exportButton);

        var logsButton = new Button { Text = "Logs", TooltipText = "Open recent BetterModMenu/TTSMM log output" };
        ModdingScreenVanillaStyle.ApplyButton(logsButton);
        logsButton.Pressed += onViewLogsPressed;
        primaryRow.AddChild(logsButton);

        var tutorialButton = new Button { Text = "Help", TooltipText = "Reopen the BetterModMenu tutorial" };
        ModdingScreenVanillaStyle.ApplyButton(tutorialButton);
        tutorialButton.Pressed += onTutorialPressed;
        primaryRow.AddChild(tutorialButton);

        var gameVersionButton = new Button { Text = "Game", TooltipText = "Show configured SteamCMD game-version download command" };
        ModdingScreenVanillaStyle.ApplyButton(gameVersionButton);
        gameVersionButton.Pressed += onGameVersionPressed;
        primaryRow.AddChild(gameVersionButton);

        Button? cloudButton = null;
#if BETTERMODMENU_CLOUD_FEATURES
        cloudButton = new Button { Text = "Cloud", TooltipText = "Configure cloud-synced backup mirror directory" };
        ModdingScreenVanillaStyle.ApplyButton(cloudButton);
        cloudButton.Pressed += onCloudBackupPressed;
        primaryRow.AddChild(cloudButton);
#endif

        var flexibleSpacer = new Control { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        primaryRow.AddChild(flexibleSpacer);

        var groupLabel = new Label { Text = "Group:" };
        ModdingScreenVanillaStyle.ApplyLabel(groupLabel, muted: true);
        primaryRow.AddChild(groupLabel);

        var newGroupInput = new LineEdit { PlaceholderText = "Name...", CustomMinimumSize = new Vector2(ModdingScreenConstants.GroupInputWideWidth, 0) };
        ModdingScreenVanillaStyle.ApplyLineEdit(newGroupInput);
        primaryRow.AddChild(newGroupInput);

        var newGroupBtn = new Button { Text = "+ Add" };
        ModdingScreenVanillaStyle.ApplyButton(newGroupBtn);
        newGroupBtn.Pressed += () =>
        {
            if (onAddGroupRequested(newGroupInput.Text))
                newGroupInput.Text = "";
        };
        primaryRow.AddChild(newGroupBtn);

        return new GroupBarControls(groupBar, primaryRow, secondaryRow, portableToggle, backupButton, exportButton, logsButton, tutorialButton, gameVersionButton, cloudButton, groupLabel, newGroupInput, newGroupBtn, flexibleSpacer);
    }
}
