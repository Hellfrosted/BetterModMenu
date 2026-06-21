using Godot;

namespace BetterModMenu.Patches;

internal static class ModdingScreenVanillaStyle
{
    private static readonly Color GoldBorderColor = new(0.86f, 0.62f, 0.27f, 0.95f);
    private static readonly Color TextColor = new(0.92f, 0.86f, 0.74f, 1f);
    private static readonly Color MutedTextColor = new(0.72f, 0.66f, 0.58f, 1f);
    private static readonly Color LogPanelColor = new(0.035f, 0.032f, 0.03f, 0.98f);
    private static readonly Color ToolbarPanelColor = new(0.17f, 0.16f, 0.15f, 0.96f);

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

    public static void ApplyLabel(Label label, bool muted = false)
    {
        label.AddThemeColorOverride("font_color", muted ? MutedTextColor : TextColor);
        label.VerticalAlignment = VerticalAlignment.Center;
    }

    public static void ApplyLineEdit(LineEdit input)
    {
        input.CustomMinimumSize = new Vector2(input.CustomMinimumSize.X, 30);
    }

    public static void ApplyOptionButton(OptionButton button)
    {
        ApplyButton(button);
    }

    public static void ApplyLogPanel(Control control)
    {
        control.AddThemeStyleboxOverride("panel", BuildPanelBox(LogPanelColor, GoldBorderColor, 1, 0));
    }

    public static void ApplyLogToolbarPanel(Control control)
    {
        control.AddThemeStyleboxOverride("panel", BuildPanelBox(ToolbarPanelColor, new Color(1f, 0.95f, 0.86f, 0.18f), 1, 4));
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
}
