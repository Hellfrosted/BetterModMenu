using System;
using Godot;

namespace BetterModMenu.Patches;

internal static class ModdingScreenBars
{
    public static (HBoxContainer Bar, OptionButton ProfileDropdown) CreateTopBar(
        Control? titleNode,
        Control? scrollContainer,
        Action<long> onProfileSelected,
        Action onNewProfilePressed,
        Action onRenameProfilePressed,
        Action onDeleteProfilePressed)
    {
        var topBar = new HBoxContainer();

        if (titleNode != null && scrollContainer != null)
        {
            float leftPanelRight = scrollContainer.GlobalPosition.X + scrollContainer.Size.X;
            topBar.Position = new Vector2(
                titleNode.GlobalPosition.X + titleNode.Size.X + 10,
                titleNode.GlobalPosition.Y
            );
            topBar.Size = new Vector2(
                leftPanelRight - (titleNode.GlobalPosition.X + titleNode.Size.X + 10) - 30,
                titleNode.Size.Y
            );
        }
        else
        {
            topBar.Position = new Vector2(300, 55);
            topBar.Size = new Vector2(200, 30);
        }

        var profileLabel = new Label { Text = "Profile:" };
        topBar.AddChild(profileLabel);

        var profileDropdown = new OptionButton { CustomMinimumSize = new Vector2(120, 0) };
        profileDropdown.ItemSelected += index => onProfileSelected(index);
        topBar.AddChild(profileDropdown);

        var newProfileBtn = new Button { Text = "+ New" };
        newProfileBtn.Pressed += onNewProfilePressed;
        topBar.AddChild(newProfileBtn);

        var renameProfileBtn = new Button { Text = "Rename" };
        renameProfileBtn.Pressed += onRenameProfilePressed;
        topBar.AddChild(renameProfileBtn);

        var delProfileBtn = new Button { Text = "Del" };
        delProfileBtn.Pressed += onDeleteProfilePressed;
        topBar.AddChild(delProfileBtn);

        return (topBar, profileDropdown);
    }

    public static HBoxContainer CreateGroupBar(
        Control? modInfoPanel,
        bool portableModeEnabled,
        Action<bool> onPortableModeToggled,
        Func<string, bool> onAddGroupRequested)
    {
        var groupBar = new HBoxContainer();

        if (modInfoPanel != null)
        {
            groupBar.Position = new Vector2(
                modInfoPanel.GlobalPosition.X,
                modInfoPanel.GlobalPosition.Y - 35
            );
            groupBar.Size = new Vector2(modInfoPanel.Size.X, 28);
        }
        else
        {
            groupBar.Position = new Vector2(550, 30);
            groupBar.Size = new Vector2(400, 28);
        }
        groupBar.Alignment = BoxContainer.AlignmentMode.Begin;

        var portableToggle = new CheckButton { Text = "Portable Mode", ButtonPressed = portableModeEnabled };
        portableToggle.Toggled += isToggled => onPortableModeToggled(isToggled);
        groupBar.AddChild(portableToggle);

        groupBar.AddChild(new Control { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill });

        var groupLabel = new Label { Text = "Group:" };
        groupBar.AddChild(groupLabel);

        var newGroupInput = new LineEdit { PlaceholderText = "Name...", CustomMinimumSize = new Vector2(140, 0) };
        groupBar.AddChild(newGroupInput);

        var newGroupBtn = new Button { Text = "+ Add" };
        newGroupBtn.Pressed += () =>
        {
            if (onAddGroupRequested(newGroupInput.Text))
                newGroupInput.Text = "";
        };
        groupBar.AddChild(newGroupBtn);

        return groupBar;
    }
}
