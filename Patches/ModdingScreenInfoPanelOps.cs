using BetterModMenu.Data;
using Godot;
using MegaCrit.Sts2.Core.Nodes.Screens.ModdingScreen;

namespace BetterModMenu.Patches;

internal static class ModdingScreenInfoPanelOps
{
    private const string ActionRootName = "BetterModMenuSelectedModActions";
    private const string MatchReasonName = "BetterModMenuSearchMatchReason";
    private const string ActionRowName = "BetterModMenuDetailActionRow";
    private const string ConfigButtonName = "BetterModMenuConfigButton";
    private const string AnnotationButtonName = "BetterModMenuAnnotationButton";
    private const string GameplayBadgeName = "BetterModMenuGameplayBadge";

    public static void Refresh(NModdingScreen screen, ModdingScreenSession session)
    {
        var infoContainer = screen.GetNodeOrNull<Control>("%ModInfoContainer");
        if (infoContainer == null)
            return;

        var root = EnsureActionRoot(infoContainer);
        var reasonLabel = root.GetNode<Label>(MatchReasonName);
        var configButton = root.GetNode<Button>(ConfigButtonName);
        var annotationButton = root.GetNode<Button>(AnnotationButtonName);
        var gameplayBadge = configButton.GetNode<Label>(GameplayBadgeName);

        string selectedModId = session.SelectedModId;
        var provider = string.IsNullOrWhiteSpace(selectedModId)
            ? ModConfigProviderKind.None
            : ModConfigProviderAdapter.GetProvider(selectedModId);
        string providerName = GetProviderName(provider);

        bool affectsGameplay = ProfileManager.ModGameplayImpactCache.TryGetValue(selectedModId, out bool cachedImpact) && cachedImpact;
        configButton.Text = ModdingScreenText.Get(BmmText.DetailConfig, "Config");
        annotationButton.Text = ModdingScreenText.Get(BmmText.DetailAnnotation, "Alias / Notes");
        gameplayBadge.Visible = affectsGameplay;
        reasonLabel.Text = BuildMatchReason(session, selectedModId);
        reasonLabel.Visible = !string.IsNullOrWhiteSpace(reasonLabel.Text);
        UpdateActionRootLayout(root, reasonLabel.Visible);
        bool hasConfigProvider = provider != ModConfigProviderKind.None;
        ModdingScreenVanillaStyle.ApplyDetailActionAvailability(configButton, hasConfigProvider);
        bool hasSelection = !string.IsNullOrWhiteSpace(selectedModId);
        ModdingScreenVanillaStyle.ApplyDetailActionAvailability(annotationButton, hasSelection);
        annotationButton.TooltipText = BuildAnnotationTooltip(
            hasSelection ? ProfileManager.GetModAnnotation(selectedModId) : null);
        configButton.TooltipText = provider == ModConfigProviderKind.None
            ? BuildConfigTooltip(ModdingScreenText.Get(
                BmmText.DetailConfigUnavailableTooltip,
                "No RitsuLib or BaseLib config is available for the selected mod."), affectsGameplay)
            : BuildConfigTooltip(ModdingScreenText.Format(
                BmmText.DetailConfigOpenTooltipFormat,
                "Open this mod's {0} config.",
                providerName), affectsGameplay);
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

        var reason = new Label
        {
            Name = MatchReasonName,
            Text = "",
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
            TooltipText = ModdingScreenText.Get(BmmText.DetailSearchMatchTooltip, "Why the selected mod matched the current search."),
            CustomMinimumSize = new Vector2(0, ModdingScreenConstants.DetailStatusLineHeight),
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
        };
        ModdingScreenVanillaStyle.ApplyLabel(reason, muted: true);
        root.AddChild(reason);

        var actionRow = new HBoxContainer
        {
            Name = ActionRowName,
            CustomMinimumSize = new Vector2(0, ModdingScreenConstants.DetailConfigButtonHeight),
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
        };
        actionRow.AddThemeConstantOverride("separation", ModdingScreenConstants.DetailActionGap);
        root.AddChild(actionRow);

        var config = new Button
        {
            Name = ConfigButtonName,
            Text = ModdingScreenText.Get(BmmText.DetailConfig, "Config"),
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
            ClipContents = true,
            CustomMinimumSize = new Vector2(0, ModdingScreenConstants.DetailConfigButtonHeight),
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
        };
        ModdingScreenVanillaStyle.ApplyDetailActionButton(config);
        MatchDetailPanelFont(infoContainer, config);

        var gameplayBadge = new Label
        {
            Name = GameplayBadgeName,
            Text = ModdingScreenText.Get(BmmText.DetailGameplayBadge, "Affects gameplay"),
            Visible = false,
            MouseFilter = Control.MouseFilterEnum.Ignore,
            ClipText = true
        };
        gameplayBadge.SetAnchorsPreset(Control.LayoutPreset.TopLeft);
        gameplayBadge.OffsetLeft = 10;
        gameplayBadge.OffsetTop = 6;
        gameplayBadge.OffsetRight = 230;
        gameplayBadge.OffsetBottom = 28;
        ModdingScreenVanillaStyle.ApplyDetailActionBadge(gameplayBadge);
        config.AddChild(gameplayBadge);

        config.Pressed += () =>
        {
            var screen = ModdingScreenNodeOps.FindOwningScreen(config);
            if (screen == null)
                return;

            var session = ModdingScreenContext.GetSession(screen);
            var provider = ModConfigProviderAdapter.GetProvider(session.SelectedModId);
            ModConfigProviderAdapter.Open(screen, session.SelectedModId, provider);
        };
        actionRow.AddChild(config);

        var annotation = new Button
        {
            Name = AnnotationButtonName,
            Text = ModdingScreenText.Get(BmmText.DetailAnnotation, "Alias / Notes"),
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
            ClipContents = true,
            CustomMinimumSize = new Vector2(0, ModdingScreenConstants.DetailConfigButtonHeight),
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
        };
        ModdingScreenVanillaStyle.ApplyDetailActionButton(annotation);
        MatchDetailPanelFont(infoContainer, annotation);
        annotation.Pressed += () =>
        {
            var screen = ModdingScreenNodeOps.FindOwningScreen(annotation);
            if (screen == null)
                return;

            var session = ModdingScreenContext.GetSession(screen);
            string modId = session.SelectedModId;
            if (string.IsNullOrWhiteSpace(modId))
                return;

            ModdingScreenDialogs.ShowModAnnotationDialog(
                screen,
                FindSelectedModName(screen, modId),
                ProfileManager.GetModAnnotation(modId),
                (alias, notes) =>
                {
                    if (!ProfileManager.TrySaveModAnnotation(modId, alias, notes, out string? error))
                    {
                        ModdingScreenDialogs.ShowInfoDialog(
                            screen,
                            ModdingScreenText.Get(BmmText.AnnotationSaveFailedTitle, "Notes Not Saved"),
                            ModdingScreenText.Format(
                                BmmText.AnnotationSaveFailedMessageFormat,
                                "Better Mod Menu could not save the alias or notes.\n\n{0}",
                                error ?? "Unknown error."));
                        return false;
                    }

                    NModdingScreenPatch.RefreshGroupsUI();
                    return true;
                });
        };
        actionRow.AddChild(annotation);

        infoContainer.AddChild(root);
        return root;
    }

    private static void UpdateActionRootLayout(VBoxContainer root, bool reasonVisible)
    {
        float contentHeight = ModdingScreenConstants.DetailConfigButtonHeight;
        if (reasonVisible)
            contentHeight += ModdingScreenConstants.DetailStatusLineHeight + ModdingScreenConstants.DetailActionGap;

        root.OffsetTop = -(contentHeight + ModdingScreenConstants.DetailActionBottomInset);
    }

    private static string BuildConfigTooltip(string baseText, bool affectsGameplay)
    {
        return affectsGameplay
            ? baseText + "\n" + ModdingScreenText.Get(BmmText.GameplayImpactTooltip, "This mod affects gameplay.")
            : baseText;
    }

    private static string BuildAnnotationTooltip(ModAnnotation? annotation)
    {
        string tooltip = ModdingScreenText.Get(
            BmmText.DetailAnnotationTooltip,
            "Add a personal alias or notes for the selected mod.");
        if (annotation == null)
            return tooltip;

        if (!string.IsNullOrWhiteSpace(annotation.Alias))
            tooltip += "\n" + ModdingScreenText.Get(BmmText.DialogAnnotationAlias, "Alias") + ": " + annotation.Alias;
        if (!string.IsNullOrWhiteSpace(annotation.Notes))
        {
            string preview = annotation.Notes.Split('\n')[0];
            if (preview.Length > 100)
                preview = preview[..97] + "...";
            tooltip += "\n" + ModdingScreenText.Get(BmmText.DialogAnnotationNotes, "Notes") + ": " + preview;
        }

        return tooltip;
    }

    private static string FindSelectedModName(NModdingScreen screen, string modId)
    {
        var rowContainer = ModdingScreenNodeOps.GetModRowContainer(screen);
        var row = rowContainer?.GetChildren()
            .OfType<NModMenuRow>()
            .FirstOrDefault(candidate => string.Equals(
                candidate.Mod?.manifest?.id,
                modId,
                StringComparison.OrdinalIgnoreCase));
        string displayName = row?.Mod?.manifest?.name ?? string.Empty;
        return string.IsNullOrWhiteSpace(displayName) ? modId : displayName;
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
            return ModdingScreenText.Get(BmmText.DetailNoMatchingMods, "No matching mods.");

        if (!session.SearchResults.TryGetValue(selectedModId, out var result))
            return string.Empty;

        return string.IsNullOrWhiteSpace(result.MatchReasonKey)
            ? result.MatchReason
            : ModdingScreenText.Get(result.MatchReasonKey, result.MatchReason);
    }

    private static string GetProviderName(ModConfigProviderKind provider)
    {
        return provider switch
        {
            ModConfigProviderKind.RitsuLib => "RitsuLib",
            ModConfigProviderKind.BaseLib => "BaseLib",
            _ => ModdingScreenText.Get(BmmText.DetailProviderMod, "mod")
        };
    }
}
