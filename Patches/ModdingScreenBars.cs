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
        NewProfileButton.Text = isCompact ? "New" : "+ New";
        RenameProfileButton.Text = isCompact ? "Ren" : "Rename";

        float buttonWidth = isCompact
            ? ModdingScreenConstants.TopBarButtonCompactWidth
            : ModdingScreenConstants.TopBarButtonMinWidth;
        var minSize = new Vector2(buttonWidth, 0);
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
        Label groupLabel,
        LineEdit newGroupInput,
        Button newGroupButton)
    {
        Bar = bar;
        PrimaryRow = primaryRow;
        SecondaryRow = secondaryRow;
        PortableToggle = portableToggle;
        GroupLabel = groupLabel;
        NewGroupInput = newGroupInput;
        NewGroupButton = newGroupButton;
    }

    public VBoxContainer Bar { get; }
    public HBoxContainer PrimaryRow { get; }
    public HBoxContainer SecondaryRow { get; }
    public CheckButton PortableToggle { get; }
    public Label GroupLabel { get; }
    public LineEdit NewGroupInput { get; }
    public Button NewGroupButton { get; }

    public void SetCompact(bool isCompact)
    {
        MoveChild(PortableToggle, PrimaryRow);
        if (isCompact)
        {
            SecondaryRow.Visible = true;
            MoveChild(GroupLabel, SecondaryRow);
            MoveChild(NewGroupInput, SecondaryRow);
            MoveChild(NewGroupButton, SecondaryRow);
            NewGroupInput.CustomMinimumSize = new Vector2(ModdingScreenConstants.GroupInputCompactWidth, 0);
        }
        else
        {
            SecondaryRow.Visible = false;
            MoveChild(GroupLabel, PrimaryRow);
            MoveChild(NewGroupInput, PrimaryRow);
            MoveChild(NewGroupButton, PrimaryRow);
            NewGroupInput.CustomMinimumSize = new Vector2(ModdingScreenConstants.GroupInputWideWidth, 0);
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
        topBar.AddChild(profileLabel);

        var profileDropdown = new OptionButton
        {
            CustomMinimumSize = new Vector2(120, 0),
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
        };
        profileDropdown.ItemSelected += index => onProfileSelected(index);
        topBar.AddChild(profileDropdown);

        var newProfileBtn = new Button { Text = "+ New", CustomMinimumSize = new Vector2(ModdingScreenConstants.TopBarButtonMinWidth, 0) };
        newProfileBtn.Pressed += onNewProfilePressed;
        topBar.AddChild(newProfileBtn);

        var renameProfileBtn = new Button { Text = "Rename", CustomMinimumSize = new Vector2(ModdingScreenConstants.TopBarButtonMinWidth, 0) };
        renameProfileBtn.Pressed += onRenameProfilePressed;
        topBar.AddChild(renameProfileBtn);

        var delProfileBtn = new Button { Text = "Del", CustomMinimumSize = new Vector2(ModdingScreenConstants.TopBarButtonMinWidth, 0) };
        delProfileBtn.Pressed += onDeleteProfilePressed;
        topBar.AddChild(delProfileBtn);

        return new TopBarControls(topBar, profileDropdown, newProfileBtn, renameProfileBtn, delProfileBtn);
    }

    public static GroupBarControls CreateGroupBar(
        bool portableModeEnabled,
        Action<bool> onPortableModeToggled,
        Func<string, bool> onAddGroupRequested)
    {
        var groupBar = new VBoxContainer { Name = "BetterModMenuGroupBar" };
        var primaryRow = new HBoxContainer();
        var secondaryRow = new HBoxContainer();
        groupBar.AddChild(primaryRow);
        groupBar.AddChild(secondaryRow);

        var portableToggle = new CheckButton { Text = "Portable Mode", ButtonPressed = portableModeEnabled };
        portableToggle.Toggled += isToggled => onPortableModeToggled(isToggled);
        primaryRow.AddChild(portableToggle);

        primaryRow.AddChild(new Control { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill });

        var groupLabel = new Label { Text = "Group:" };
        primaryRow.AddChild(groupLabel);

        var newGroupInput = new LineEdit { PlaceholderText = "Name...", CustomMinimumSize = new Vector2(ModdingScreenConstants.GroupInputWideWidth, 0) };
        primaryRow.AddChild(newGroupInput);

        var newGroupBtn = new Button { Text = "+ Add" };
        newGroupBtn.Pressed += () =>
        {
            if (onAddGroupRequested(newGroupInput.Text))
                newGroupInput.Text = "";
        };
        primaryRow.AddChild(newGroupBtn);

        return new GroupBarControls(groupBar, primaryRow, secondaryRow, portableToggle, groupLabel, newGroupInput, newGroupBtn);
    }
}
