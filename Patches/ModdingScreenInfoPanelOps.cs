using BetterModMenu.Data;
using Godot;
using MegaCrit.Sts2.Core.Nodes.Screens.ModdingScreen;

namespace BetterModMenu.Patches;

internal static class ModdingScreenInfoPanelOps
{
    private const string ActionRootName = "BetterModMenuSelectedModActions";
    private const string GameplayImpactName = "BetterModMenuGameplayImpact";
    private const string MatchReasonName = "BetterModMenuSearchMatchReason";
    private const string ConfigButtonName = "BetterModMenuConfigButton";

    public static void Refresh(NModdingScreen screen, ModdingScreenSession session)
    {
        var infoContainer = screen.GetNodeOrNull<Control>("%ModInfoContainer");
        if (infoContainer == null)
            return;

        var root = EnsureActionRoot(infoContainer);
        var gameplayImpactLabel = root.GetNode<Label>(GameplayImpactName);
        var reasonLabel = root.GetNode<Label>(MatchReasonName);
        var configButton = root.GetNode<Button>(ConfigButtonName);

        string selectedModId = session.SelectedModId;
        var provider = string.IsNullOrWhiteSpace(selectedModId)
            ? ModConfigProviderKind.None
            : ModConfigProviderAdapter.GetProvider(selectedModId);
        string providerName = GetProviderName(provider);

        bool affectsGameplay = ProfileManager.ModGameplayImpactCache.TryGetValue(selectedModId, out bool cachedImpact) && cachedImpact;
        gameplayImpactLabel.Visible = affectsGameplay;
        reasonLabel.Text = BuildMatchReason(session, selectedModId);
        reasonLabel.Visible = !string.IsNullOrWhiteSpace(reasonLabel.Text);
        bool hasConfigProvider = provider != ModConfigProviderKind.None;
        ModdingScreenVanillaStyle.ApplyDetailActionAvailability(configButton, hasConfigProvider);
        configButton.TooltipText = provider == ModConfigProviderKind.None
            ? "No RitsuLib or BaseLib config is available for the selected mod."
            : "Open this mod's " + providerName + " config.";
    }

    public static void ReserveDescriptionActionArea(Control? description)
    {
        if (description == null)
            return;

        description.ClipContents = true;
        if (description is Label label)
            label.ClipText = true;

        description.AnchorBottom = 1f;
        description.OffsetBottom = Mathf.Min(
            description.OffsetBottom,
            -ModdingScreenConstants.DetailDescriptionBottomInset);
    }

    private static VBoxContainer EnsureActionRoot(Control infoContainer)
    {
        if (infoContainer.GetNodeOrNull<VBoxContainer>(ActionRootName) is { } existing)
            return existing;

        var root = new VBoxContainer
        {
            Name = ActionRootName,
            MouseFilter = Control.MouseFilterEnum.Ignore
        };
        root.AddThemeConstantOverride("separation", ModdingScreenConstants.DetailActionGap);
        root.SetAnchorsPreset(Control.LayoutPreset.BottomWide);
        root.OffsetLeft = ModdingScreenConstants.DetailActionHorizontalInset;
        root.OffsetRight = -ModdingScreenConstants.DetailActionHorizontalInset;
        root.OffsetTop = -ModdingScreenConstants.DetailActionPanelHeight;
        root.OffsetBottom = -ModdingScreenConstants.DetailActionBottomInset;

        var gameplayImpact = new Label
        {
            Name = GameplayImpactName,
            Text = "Affects gameplay",
            Visible = false,
            TooltipText = "This mod affects gameplay.",
            CustomMinimumSize = new Vector2(0, ModdingScreenConstants.DetailStatusLineHeight),
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
        };
        ModdingScreenVanillaStyle.ApplyLabel(gameplayImpact, muted: false);
        gameplayImpact.AddThemeColorOverride("font_color", new Color(1f, 0.67f, 0.36f));
        gameplayImpact.AddThemeFontSizeOverride("font_size", ModdingScreenConstants.DetailStatusFontSize);
        root.AddChild(gameplayImpact);

        var reason = new Label
        {
            Name = MatchReasonName,
            Text = "",
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
            TooltipText = "Why the selected mod matched the current search.",
            CustomMinimumSize = new Vector2(0, ModdingScreenConstants.DetailStatusLineHeight),
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
        };
        ModdingScreenVanillaStyle.ApplyLabel(reason, muted: true);
        root.AddChild(reason);

        var config = new Button
        {
            Name = ConfigButtonName,
            Text = "Config",
            CustomMinimumSize = new Vector2(0, ModdingScreenConstants.DetailConfigButtonHeight),
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            SizeFlagsVertical = Control.SizeFlags.ExpandFill
        };
        ModdingScreenVanillaStyle.ApplyDetailActionButton(config);
        MatchDetailPanelFont(infoContainer, config);
        config.Pressed += () =>
        {
            var screen = ModdingScreenNodeOps.FindOwningScreen(config);
            if (screen == null)
                return;

            var session = ModdingScreenContext.GetSession(screen);
            var provider = ModConfigProviderAdapter.GetProvider(session.SelectedModId);
            ModConfigProviderAdapter.Open(screen, session.SelectedModId, provider);
        };
        root.AddChild(config);

        infoContainer.AddChild(root);
        return root;
    }

    private static void MatchDetailPanelFont(Control infoContainer, Button button)
    {
        var source = FindVanillaDetailLabel(infoContainer);
        if (source == null)
            return;

        button.AddThemeFontOverride("font", source.GetThemeFont("font"));
    }

    private static Label? FindVanillaDetailLabel(Node node)
    {
        foreach (Node child in node.GetChildren())
        {
            if (child.Name.ToString() == ActionRootName)
                continue;

            if (child is Label label)
                return label;

            var nested = FindVanillaDetailLabel(child);
            if (nested != null)
                return nested;
        }

        return null;
    }

    private static string BuildMatchReason(ModdingScreenSession session, string selectedModId)
    {
        if (string.IsNullOrWhiteSpace(session.SearchQuery))
            return string.Empty;

        if (string.IsNullOrWhiteSpace(selectedModId))
            return "No matching mods.";

        return session.SearchResults.TryGetValue(selectedModId, out var result)
            ? result.MatchReason
            : string.Empty;
    }

    private static string GetProviderName(ModConfigProviderKind provider)
    {
        return provider switch
        {
            ModConfigProviderKind.RitsuLib => "RitsuLib",
            ModConfigProviderKind.BaseLib => "BaseLib",
            _ => "mod"
        };
    }
}
