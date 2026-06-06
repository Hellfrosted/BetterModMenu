using Godot;

namespace BetterModMenu.Patches;

internal static class ModdingScreenVanillaStyle
{
    private static readonly Color GoldBorderColor = new(0.86f, 0.62f, 0.27f, 0.95f);
    private static readonly Color TextColor = new(0.92f, 0.86f, 0.74f, 1f);
    private static readonly Color MutedTextColor = new(0.72f, 0.66f, 0.58f, 1f);

    public static void ApplyGroupHeader(HBoxContainer header)
    {
        header.CustomMinimumSize = new Vector2(0, 34);
    }

    public static void ApplyButton(Button button)
    {
        button.CustomMinimumSize = new Vector2(Mathf.Max(button.CustomMinimumSize.X, 64), 30);
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
        control.AddThemeStyleboxOverride("panel", BuildPanelBox(new Color(0.035f, 0.032f, 0.03f, 0.96f), GoldBorderColor, 1));
    }

    private static StyleBoxFlat BuildPanelBox(Color background, Color border, int borderWidth)
    {
        var style = new StyleBoxFlat
        {
            BgColor = background,
            BorderColor = border,
            CornerDetail = 1,
            ShadowColor = new Color(0f, 0f, 0f, 0.55f),
            ShadowSize = 4,
            ShadowOffset = new Vector2(1, 2)
        };
        style.SetBorderWidthAll(borderWidth);
        style.SetCornerRadiusAll(5);
        style.SetContentMarginAll(6);
        return style;
    }
}
