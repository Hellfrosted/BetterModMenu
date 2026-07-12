using Godot;

namespace BetterModMenu.Patches;

internal static class ModdingScreenVanillaStyle
{
    private const int DialogTitleHeight = 38;

    private static readonly Color GoldBorderColor = new(0.86f, 0.62f, 0.27f, 0.95f);
    private static readonly Color TextColor = new(0.92f, 0.86f, 0.74f, 1f);
    private static readonly Color MutedTextColor = new(0.72f, 0.66f, 0.58f, 1f);
    private static readonly Color CyanAccentColor = new(0.39f, 0.83f, 0.92f, 0.92f);
    private static readonly Color LogPanelColor = new(0.035f, 0.032f, 0.03f, 0.98f);
    private static readonly Color ToolbarPanelColor = new(0.17f, 0.16f, 0.15f, 0.96f);
    private static readonly Color DialogPanelColor = new(0.07f, 0.06f, 0.052f, 0.99f);
    private static readonly Color DialogInsetColor = new(0.025f, 0.024f, 0.023f, 0.97f);

    public static void ApplyGroupHeader(HBoxContainer header)
    {
        header.CustomMinimumSize = new Vector2(0, 34);
    }

    public static void ApplyButton(Button button)
    {
        button.CustomMinimumSize = new Vector2(
            Mathf.Max(button.CustomMinimumSize.X, 64),
            Mathf.Max(button.CustomMinimumSize.Y, 30));
        button.AddThemeColorOverride("font_color", TextColor);
        button.AddThemeColorOverride("font_hover_color", TextColor);
        button.AddThemeColorOverride("font_pressed_color", TextColor);
        button.AddThemeColorOverride("font_focus_color", TextColor);
    }

    public static void ApplyDetailActionButton(Button button)
    {
        ApplyButton(button);
        button.AddThemeFontSizeOverride("font_size", ModdingScreenConstants.DetailConfigButtonFontSize);
    }

    public static void ApplyDetailActionBadge(Label label)
    {
        ApplyLabel(label, muted: true);
        label.AddThemeFontSizeOverride("font_size", ModdingScreenConstants.DetailConfigBadgeFontSize);
    }

    public static void ApplyDetailActionAvailability(Button button, bool available)
    {
        button.Disabled = !available;
        button.Modulate = available
            ? new Color(1f, 1f, 1f, 1f)
            : new Color(0.48f, 0.48f, 0.48f, 0.72f);
    }

    public static void ApplySmallButton(Button button)
    {
        ApplyButton(button);
        button.CustomMinimumSize = new Vector2(Mathf.Max(button.CustomMinimumSize.X, 42), 28);
    }

    public static void ApplyIconButton(Button button)
    {
        ApplyButton(button);
        button.CustomMinimumSize = new Vector2(
            ModdingScreenConstants.GroupHeaderIconButtonSize,
            ModdingScreenConstants.GroupHeaderIconButtonSize);
        button.Alignment = HorizontalAlignment.Center;
        button.IconAlignment = HorizontalAlignment.Center;
        button.VerticalIconAlignment = VerticalAlignment.Center;
        button.ExpandIcon = false;
        button.ClipText = true;
        button.AddThemeConstantOverride("h_separation", 0);
        button.AddThemeConstantOverride("icon_max_width", ModdingScreenConstants.GroupHeaderIconSize);
    }

    public static void ApplyLabel(Label label, bool muted = false)
    {
        label.AddThemeColorOverride("font_color", muted ? MutedTextColor : TextColor);
        label.VerticalAlignment = VerticalAlignment.Center;
    }

    public static void ApplyLineEdit(LineEdit input)
    {
        input.CustomMinimumSize = new Vector2(input.CustomMinimumSize.X, 30);
        input.AddThemeColorOverride("font_color", TextColor);
        input.AddThemeColorOverride("font_placeholder_color", MutedTextColor);
        input.AddThemeStyleboxOverride("normal", BuildPanelBox(new Color(0.04f, 0.038f, 0.034f, 0.98f), new Color(1f, 0.95f, 0.86f, 0.2f), 1, 0));
        input.AddThemeStyleboxOverride("focus", BuildPanelBox(new Color(0.045f, 0.043f, 0.039f, 0.98f), CyanAccentColor, 1, 2));
        input.AddThemeStyleboxOverride("read_only", BuildPanelBox(new Color(0.035f, 0.033f, 0.03f, 0.9f), new Color(1f, 0.95f, 0.86f, 0.12f), 1, 0));
    }

    public static void ApplyTextEdit(TextEdit input)
    {
        input.AddThemeColorOverride("font_color", TextColor);
        input.AddThemeColorOverride("font_placeholder_color", MutedTextColor);
        input.AddThemeStyleboxOverride("normal", BuildPanelBox(new Color(0.04f, 0.038f, 0.034f, 0.98f), new Color(1f, 0.95f, 0.86f, 0.2f), 1, 0));
        input.AddThemeStyleboxOverride("focus", BuildPanelBox(new Color(0.045f, 0.043f, 0.039f, 0.98f), CyanAccentColor, 1, 2));
        input.AddThemeStyleboxOverride("read_only", BuildPanelBox(new Color(0.035f, 0.033f, 0.03f, 0.9f), new Color(1f, 0.95f, 0.86f, 0.12f), 1, 0));
    }

    public static void ApplyOptionButton(OptionButton button)
    {
        ApplyButton(button);
        button.GetPopup().PopupWindow = true;
    }

    public static void ApplyLogPanel(Control control)
    {
        control.AddThemeStyleboxOverride("panel", BuildPanelBox(LogPanelColor, GoldBorderColor, 1, 0));
    }

    public static void ApplyLogToolbarPanel(Control control)
    {
        control.AddThemeStyleboxOverride("panel", BuildPanelBox(ToolbarPanelColor, new Color(1f, 0.95f, 0.86f, 0.18f), 1, 4));
    }

    public static void ApplyDialogWindow(AcceptDialog popup)
    {
        popup.PopupWindow = true;
        popup.AddThemeColorOverride("title_color", TextColor);
        popup.AddThemeColorOverride("title_outline_modulate", new Color(0f, 0f, 0f, 0.82f));
        popup.AddThemeConstantOverride("title_height", DialogTitleHeight);
        popup.AddThemeConstantOverride("title_outline_size", 2);
        popup.AddThemeConstantOverride("close_v_offset", 25);
        popup.AddThemeStyleboxOverride("embedded_border", BuildDialogWindowBox());
        popup.AddThemeStyleboxOverride("embedded_unfocused_border", BuildDialogWindowBox());
    }

    public static void ApplyDialogPanel(Control control)
    {
        control.AddThemeStyleboxOverride("panel", BuildPanelBox(DialogPanelColor, GoldBorderColor, 2, 8));
    }

    public static void ApplyDialogInsetPanel(Control control)
    {
        control.AddThemeStyleboxOverride("panel", BuildPanelBox(DialogInsetColor, new Color(1f, 0.95f, 0.86f, 0.22f), 1, 0));
    }

    public static void ApplyDialogToolbarPanel(Control control)
    {
        control.AddThemeStyleboxOverride("panel", BuildPanelBox(ToolbarPanelColor, new Color(0.88f, 0.71f, 0.42f, 0.52f), 1, 4));
    }

    public static void ApplySwatchPanel(Control control)
    {
        control.AddThemeStyleboxOverride("panel", BuildPanelBox(new Color(0.02f, 0.02f, 0.02f, 1f), CyanAccentColor, 1, 0));
    }

    public static void ApplyPreviewText(RichTextLabel label, int fontSize)
    {
        label.AddThemeColorOverride("default_color", TextColor);
        label.AddThemeFontSizeOverride("normal_font_size", fontSize);
        label.AddThemeFontSizeOverride("bold_font_size", fontSize);
        label.AddThemeFontSizeOverride("italics_font_size", fontSize);
        label.AddThemeFontSizeOverride("bold_italics_font_size", fontSize);
    }

    private static StyleBoxFlat BuildPanelBox(Color background, Color border, int borderWidth, int shadowSize)
    {
        var style = new StyleBoxFlat
        {
            BgColor = background,
            BorderColor = border,
            CornerDetail = 1,
            ShadowColor = new Color(0f, 0f, 0f, 0.55f),
            ShadowSize = shadowSize,
            ShadowOffset = new Vector2(1, 2)
        };
        style.SetBorderWidthAll(borderWidth);
        style.SetCornerRadiusAll(5);
        style.SetContentMarginAll(6);
        return style;
    }

    private static StyleBoxFlat BuildDialogWindowBox()
    {
        var style = BuildPanelBox(DialogPanelColor, GoldBorderColor, 2, 8);
        style.SetExpandMargin(Side.Top, DialogTitleHeight);
        return style;
    }
}
