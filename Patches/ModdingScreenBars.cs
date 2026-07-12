using System;
using BetterModMenu.Data;
using Godot;

namespace BetterModMenu.Patches;

internal sealed class TopBarControls(
    HBoxContainer bar,
    Label profileLabel,
    OptionButton profileDropdown,
    Button newProfileButton,
    Button renameProfileButton,
    Button deleteProfileButton)
{
    public HBoxContainer Bar { get; } = bar;
    public Label ProfileLabel { get; } = profileLabel;
    public OptionButton ProfileDropdown { get; } = profileDropdown;
    public Button NewProfileButton { get; } = newProfileButton;
    public Button RenameProfileButton { get; } = renameProfileButton;
    public Button DeleteProfileButton { get; } = deleteProfileButton;

    public void SetCompact(bool isCompact)
    {
        var presentation = ModdingScreenLayoutRules.GetTopBarPresentation(isCompact);
        ProfileLabel.Visible = !isCompact;
        ProfileDropdown.CustomMinimumSize = new Vector2(
            isCompact ? ModdingScreenConstants.TopBarDropdownCompactWidth : ModdingScreenConstants.TopBarDropdownWidth,
            ModdingScreenConstants.ToolbarControlHeight);
        NewProfileButton.Text = presentation.NewProfile.Text;
        NewProfileButton.Icon = ModdingScreenIcons.Get(ModdingScreenIcon.FilePlus);
        NewProfileButton.TooltipText = ModdingScreenText.Get(presentation.NewProfile.TooltipKey, "New profile: copy the current enabled/disabled mods into a separate saved setup.");
        NewProfileButton.AccessibilityName = NewProfileButton.TooltipText;
        RenameProfileButton.Text = presentation.RenameProfile.Text;
        RenameProfileButton.Icon = ModdingScreenIcons.Get(ModdingScreenIcon.FilePenLine);
        RenameProfileButton.TooltipText = ModdingScreenText.Get(presentation.RenameProfile.TooltipKey, "Rename profile: change the selected profile's name without changing its mods.");
        RenameProfileButton.AccessibilityName = RenameProfileButton.TooltipText;
        DeleteProfileButton.Text = presentation.DeleteProfile.Text;
        DeleteProfileButton.Icon = ModdingScreenIcons.Get(ModdingScreenIcon.FileX);
        DeleteProfileButton.TooltipText = ModdingScreenText.Get(presentation.DeleteProfile.TooltipKey, "Delete profile: remove the selected saved setup. Your installed mod files stay installed.");
        DeleteProfileButton.AccessibilityName = DeleteProfileButton.TooltipText;

        var minSize = new Vector2(presentation.ButtonWidth, ModdingScreenConstants.ToolbarControlHeight);
        NewProfileButton.CustomMinimumSize = minSize;
        RenameProfileButton.CustomMinimumSize = minSize;
        DeleteProfileButton.CustomMinimumSize = minSize;
    }
}

internal sealed class GroupBarControls(
    VBoxContainer bar,
    HBoxContainer searchBar,
    HBoxContainer primaryRow,
    HBoxContainer secondaryRow,
    CheckButton portableToggle,
    Button backupButton,
    Button loadBackupButton,
    Button exportButton,
    Button logsButton,
    Button styleButton,
    Button tutorialButton,
    Button? cloudButton,
    LineEdit searchInput,
    Label searchResultLabel,
    Label groupLabel,
    LineEdit newGroupInput,
    Button newGroupButton,
    Control flexibleSpacer)
{
    public VBoxContainer Bar { get; } = bar;
    public HBoxContainer SearchBar { get; } = searchBar;
    public HBoxContainer PrimaryRow { get; } = primaryRow;
    public HBoxContainer SecondaryRow { get; } = secondaryRow;
    public CheckButton PortableToggle { get; } = portableToggle;
    public Button BackupButton { get; } = backupButton;
    public Button LoadBackupButton { get; } = loadBackupButton;
    public Button ExportButton { get; } = exportButton;
    public Button LogsButton { get; } = logsButton;
    public Button StyleButton { get; } = styleButton;
    public Button TutorialButton { get; } = tutorialButton;
    public Button? CloudButton { get; } = cloudButton;
    public LineEdit SearchInput { get; } = searchInput;
    public Label SearchResultLabel { get; } = searchResultLabel;
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
        MoveChild(StyleButton, PrimaryRow);
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
            Text = ModdingScreenText.Get(BmmText.ProfileLabel, "Profile:"),
            TooltipText = ModdingScreenText.Get(BmmText.ProfileTooltip, "Saved enabled/disabled mod setup.")
        };
        ModdingScreenVanillaStyle.ApplyLabel(profileLabel, muted: true);
        topBar.AddChild(profileLabel);

        var profileDropdown = new OptionButton
        {
            CustomMinimumSize = new Vector2(120, 0),
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            FitToLongestItem = false,
            ClipText = true,
            TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis
        };
        ModdingScreenVanillaStyle.ApplyOptionButton(profileDropdown);
        profileDropdown.ItemSelected += index => onProfileSelected(index);
        topBar.AddChild(profileDropdown);

        var newProfileBtn = new Button { Icon = ModdingScreenIcons.Get(ModdingScreenIcon.FilePlus) };
        ApplyProfileIconButton(newProfileBtn);
        newProfileBtn.Pressed += onNewProfilePressed;
        topBar.AddChild(newProfileBtn);

        var renameProfileBtn = new Button { Icon = ModdingScreenIcons.Get(ModdingScreenIcon.FilePenLine) };
        ApplyProfileIconButton(renameProfileBtn);
        renameProfileBtn.Pressed += onRenameProfilePressed;
        topBar.AddChild(renameProfileBtn);

        var delProfileBtn = new Button { Icon = ModdingScreenIcons.Get(ModdingScreenIcon.FileX) };
        ApplyProfileIconButton(delProfileBtn);
        delProfileBtn.Pressed += onDeleteProfilePressed;
        topBar.AddChild(delProfileBtn);

        return new TopBarControls(topBar, profileLabel, profileDropdown, newProfileBtn, renameProfileBtn, delProfileBtn);
    }

    private static void ApplyProfileIconButton(Button button)
    {
        ModdingScreenVanillaStyle.ApplyIconButton(button);
        button.CustomMinimumSize = new Vector2(ModdingScreenConstants.TopBarButtonCompactWidth, ModdingScreenConstants.ToolbarControlHeight);
    }

    public static GroupBarControls CreateGroupBar(
        bool portableModeEnabled,
        Action<bool> onPortableModeToggled,
        Action onManualBackupPressed,
        Action onLoadBackupPressed,
        Action onExportModListPressed,
        Action onViewLogsPressed,
        Action onStyleEditorPressed,
        Action onTutorialPressed,
        Action onCloudBackupPressed,
        Action<string> onSearchChanged,
        Func<string, bool> onAddGroupRequested)
    {
        var groupBar = new VBoxContainer { Name = "BetterModMenuGroupBar" };
        var searchBar = new HBoxContainer
        {
            Name = "BetterModMenuSearchBar",
            CustomMinimumSize = new Vector2(0, ModdingScreenConstants.SearchBarHeight)
        };
        var primaryRow = new HBoxContainer();
        var secondaryRow = new HBoxContainer();
        groupBar.AddChild(primaryRow);
        groupBar.AddChild(secondaryRow);

        var portableToggle = new CheckButton
        {
            Text = ModdingScreenText.Get(BmmText.PortableMode, "Portable Mode"),
            ButtonPressed = portableModeEnabled,
            TooltipText = ModdingScreenText.Get(BmmText.PortableModeTooltip, "Portable Mode: save Better Mod Menu data beside the mod files instead of the normal game save folder.")
        };
        ModdingScreenVanillaStyle.ApplyButton(portableToggle);
        portableToggle.Toggled += isToggled => onPortableModeToggled(isToggled);
        primaryRow.AddChild(portableToggle);

        var backupButton = new Button
        {
            Text = ModdingScreenText.Get(BmmText.Backup, "Backup"),
            TooltipText = ModdingScreenText.Get(BmmText.BackupTooltip, "Backup: save copies of your profiles, groups, and current enabled-mod settings.")
        };
        ModdingScreenVanillaStyle.ApplyButton(backupButton);
        backupButton.Pressed += onManualBackupPressed;
        primaryRow.AddChild(backupButton);

        var loadBackupButton = new Button
        {
            Text = ModdingScreenText.Get(BmmText.Load, "Load"),
            TooltipText = ModdingScreenText.Get(BmmText.LoadTooltip, "Load: choose a Better Mod Menu profile and group backup to restore.")
        };
        ModdingScreenVanillaStyle.ApplyButton(loadBackupButton);
        loadBackupButton.Pressed += onLoadBackupPressed;
        primaryRow.AddChild(loadBackupButton);

        var exportButton = new Button
        {
            Text = ModdingScreenText.Get(BmmText.Csv, "CSV"),
            TooltipText = ModdingScreenText.Get(BmmText.CsvTooltip, "CSV: export installed mods with versions, enabled state, group names, and Steam Workshop links when available.")
        };
        ModdingScreenVanillaStyle.ApplyButton(exportButton);
        exportButton.Pressed += onExportModListPressed;
        primaryRow.AddChild(exportButton);

        var logsButton = new Button
        {
            Text = ModdingScreenText.Get(BmmText.Logs, "Logs"),
            TooltipText = ModdingScreenText.Get(BmmText.LogsTooltip, "Open full Better Mod Menu log output with warnings and errors highlighted.")
        };
        ModdingScreenVanillaStyle.ApplyButton(logsButton);
        logsButton.Pressed += onViewLogsPressed;
        primaryRow.AddChild(logsButton);

        var styleButton = new Button
        {
            Text = ModdingScreenText.Get(BmmText.Style, "Style"),
            TooltipText = ModdingScreenText.Get(BmmText.StyleTooltip, "Customize in-game mod name colors by Steam Workshop tag or individual mod.")
        };
        ModdingScreenVanillaStyle.ApplyButton(styleButton);
        styleButton.Pressed += onStyleEditorPressed;
        primaryRow.AddChild(styleButton);

        var tutorialButton = new Button
        {
            Text = ModdingScreenText.Get(BmmText.Help, "Help"),
            TooltipText = ModdingScreenText.Get(BmmText.HelpTooltip, "Help: explain what each Better Mod Menu control does.")
        };
        ModdingScreenVanillaStyle.ApplyButton(tutorialButton);
        tutorialButton.Pressed += onTutorialPressed;
        primaryRow.AddChild(tutorialButton);

        Button? cloudButton = null;
#if BETTERMODMENU_CLOUD_FEATURES
        cloudButton = new Button
        {
            Text = ModdingScreenText.Get(BmmText.Cloud, "Cloud"),
            TooltipText = ModdingScreenText.Get(BmmText.CloudTooltip, "Cloud: choose a synced folder where backups and CSV exports should also be copied.")
        };
        ModdingScreenVanillaStyle.ApplyButton(cloudButton);
        cloudButton.Pressed += onCloudBackupPressed;
        primaryRow.AddChild(cloudButton);
#endif

        var flexibleSpacer = new Control { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        var searchInput = new LineEdit
        {
            PlaceholderText = ModdingScreenText.Get(BmmText.SearchPlaceholder, "Search mods..."),
            TooltipText = ModdingScreenText.Get(BmmText.SearchTooltip, "Search by mod name, id, author, description, version, dependency, group, enabled state, or Steam Workshop id."),
            CustomMinimumSize = new Vector2(ModdingScreenConstants.SearchInputWidth, 0),
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
        };
        ModdingScreenVanillaStyle.ApplyLineEdit(searchInput);
        searchInput.TextChanged += text => onSearchChanged(text);
        searchBar.AddChild(searchInput);

        var searchResultLabel = new Label
        {
            Text = "",
            TooltipText = ModdingScreenText.Get(BmmText.SearchResultTooltip, "Search result count.")
        };
        ModdingScreenVanillaStyle.ApplyLabel(searchResultLabel, muted: true);
        searchBar.AddChild(searchResultLabel);

        primaryRow.AddChild(flexibleSpacer);

        var groupLabel = new Label
        {
            Text = ModdingScreenText.Get(BmmText.GroupLabel, "Group:"),
            TooltipText = ModdingScreenText.Get(BmmText.GroupTooltip, "Custom labels for organizing mods.")
        };
        ModdingScreenVanillaStyle.ApplyLabel(groupLabel, muted: true);
        primaryRow.AddChild(groupLabel);

        var newGroupInput = new LineEdit
        {
            PlaceholderText = ModdingScreenText.Get(BmmText.GroupNamePlaceholder, "Group name..."),
            TooltipText = ModdingScreenText.Get(BmmText.GroupNameTooltip, "Type a new group name, then press Add."),
            CustomMinimumSize = new Vector2(ModdingScreenConstants.GroupInputWideWidth, 0)
        };
        ModdingScreenVanillaStyle.ApplyLineEdit(newGroupInput);
        primaryRow.AddChild(newGroupInput);

        var newGroupBtn = new Button
        {
            Text = ModdingScreenText.Get(BmmText.AddGroup, "+ Add"),
            TooltipText = ModdingScreenText.Get(BmmText.AddGroupTooltip, "Add this group name to the mod list.")
        };
        ModdingScreenVanillaStyle.ApplyButton(newGroupBtn);
        newGroupBtn.Pressed += () =>
        {
            if (onAddGroupRequested(newGroupInput.Text))
                newGroupInput.Text = "";
        };
        primaryRow.AddChild(newGroupBtn);

        return new GroupBarControls(groupBar, searchBar, primaryRow, secondaryRow, portableToggle, backupButton, loadBackupButton, exportButton, logsButton, styleButton, tutorialButton, cloudButton, searchInput, searchResultLabel, groupLabel, newGroupInput, newGroupBtn, flexibleSpacer);
    }
}
