using BetterModMenu.Data;
using Godot;
using MegaCrit.Sts2.Core.Nodes.Screens.ModdingScreen;

namespace BetterModMenu.Patches;

internal static class ModNameStyleEditorDialog
{
    private const string PreviewModId = "BetterModMenu.StylePreview";
    private const string PreviewDisplayName = "Preview Mod";

    public static void Show(NModdingScreen screen, Action onSaved)
    {
        StyleEditorDialogLayout layout = GetLayoutForScreen(screen);
        var previousSettings = ModNameStyleEditorRules.Clone(ProfileManager.ModNameStyles);
        var workingSettings = ModNameStyleEditorRules.Clone(previousSettings);
        string initialModKey = PickInitialModKey(screen);
        string selectedTag = PickInitialTag(initialModKey);

        var popup = new ConfirmationDialog
        {
            Title = "Mod Name Style",
            DialogText = string.Empty
        };
        ModdingScreenVanillaStyle.ApplyDialogWindow(popup);

        var enabledToggle = CreateToggle("Enabled", workingSettings.Enabled);
        var defaultTagToggle = CreateToggle("Use Defaults", workingSettings.UseDefaultTagFormats);
        var tagDropdown = CreateTagDropdown(selectedTag, layout);
        var tagColorInput = CreateColorInput("#74a6ff", layout);
        var tagSwatch = CreateColorSwatch(layout);
        var tagPreview = CreatePreviewLabel(layout);
        var modKeyInput = new LineEdit
        {
            Text = initialModKey,
            PlaceholderText = "mod id or name",
            CustomMinimumSize = new Vector2(layout.SettingWidth, ModdingScreenConstants.ToolbarControlHeight),
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
        };
        ModdingScreenVanillaStyle.ApplyLineEdit(modKeyInput);
        var modColorInput = CreateColorInput("#ff77cc", layout);
        var modSwatch = CreateColorSwatch(layout);
        var modPreview = CreatePreviewLabel(layout);
        var statusLabel = CreateStatusLabel(layout);

        var applyTagButton = CreateEditorButton("Apply", layout, "Stage this color for the selected Workshop tag.");
        var disableTagButton = CreateEditorButton("Disable Tag", layout, "Disable coloring for the selected Workshop tag.");
        var resetTagButton = CreateEditorButton("Reset Tag", layout, "Remove the selected tag override or disabled state.");
        var applyModButton = CreateEditorButton("Apply", layout, "Stage this color for the entered mod id or name.");
        var removeModButton = CreateEditorButton("Remove Override", layout, "Remove the entered mod-specific color override.");
        var resetAllButton = CreateEditorButton("Reset All", layout, "Stage default mod-name style settings.");
        var saveButton = CreateEditorButton("Save", layout, "Save the staged style settings.");
        var cancelButton = CreateEditorButton("Cancel", layout, "Close without saving staged style changes.");

        void SetStatus(string text, bool error = false)
        {
            statusLabel.Text = text;
            statusLabel.Visible = !string.IsNullOrWhiteSpace(text);
            statusLabel.AddThemeColorOverride("font_color", error
                ? new Color(1f, 0.45f, 0.38f, 1f)
                : new Color(0.39f, 0.83f, 0.92f, 1f));
        }

        void RefreshAll()
        {
            enabledToggle.ButtonPressed = workingSettings.Enabled;
            defaultTagToggle.ButtonPressed = workingSettings.UseDefaultTagFormats;
            SelectTag(tagDropdown, selectedTag);
            RefreshTagControls();
            RefreshModControls();
        }

        void RefreshTagControls()
        {
            if (TryGetEffectiveTagColor(workingSettings, selectedTag, out string color))
            {
                tagColorInput.Text = color;
                SetSwatchColor(tagSwatch, color);
            }
            else
            {
                tagColorInput.Text = string.Empty;
                SetSwatchColor(tagSwatch, string.Empty);
            }

            string preview = ModNameStyleEditorRules.BuildPreviewBbCode(
                PreviewModId,
                PreviewDisplayName,
                new[] { selectedTag },
                workingSettings);
            tagPreview.ParseBbcode(preview);
        }

        void RefreshModControls()
        {
            string modKey = modKeyInput.Text.Trim();
            if (TryGetModOverrideColor(workingSettings, modKey, out string color))
            {
                modColorInput.Text = color;
                SetSwatchColor(modSwatch, color);
            }
            else
            {
                SetSwatchColor(modSwatch, modColorInput.Text);
            }

            string previewModKey = string.IsNullOrWhiteSpace(modKey) ? PreviewModId : modKey;
            string displayName = FindDisplayName(screen, previewModKey);
            string preview = ModNameStyleEditorRules.BuildPreviewBbCode(
                previewModKey,
                displayName,
                GetWorkshopTags(previewModKey),
                workingSettings);
            modPreview.ParseBbcode(preview);
        }

        void ApplyTagColor()
        {
            if (!ModNameStyleEditorRules.TrySetTagColor(workingSettings, selectedTag, tagColorInput.Text, out string supportedTag, out string normalizedColor))
            {
                SetStatus("Use #rgb, #rrggbb, or #rrggbbaa for tag colors.", error: true);
                SetSwatchColor(tagSwatch, tagColorInput.Text);
                return;
            }

            selectedTag = supportedTag;
            tagColorInput.Text = normalizedColor;
            SetStatus("Tag color staged.");
            RefreshAll();
        }

        void DisableSelectedTag()
        {
            if (!ModNameStyleEditorRules.TryDisableTagColor(workingSettings, selectedTag, out string supportedTag))
            {
                SetStatus("Choose a supported Workshop tag first.", error: true);
                return;
            }

            selectedTag = supportedTag;
            SetStatus("Tag disabled in staged settings.");
            RefreshAll();
        }

        void ResetSelectedTag()
        {
            if (!ModNameStyleEditorRules.TryResetTagColor(workingSettings, selectedTag, out string supportedTag))
            {
                SetStatus("Choose a supported Workshop tag first.", error: true);
                return;
            }

            selectedTag = supportedTag;
            SetStatus("Tag reset in staged settings.");
            RefreshAll();
        }

        void ApplyModColor()
        {
            if (!ModNameStyleEditorRules.TrySetModColor(workingSettings, modKeyInput.Text, modColorInput.Text, out string normalizedColor))
            {
                SetStatus("Enter a mod id or name and a valid hex color.", error: true);
                SetSwatchColor(modSwatch, modColorInput.Text);
                return;
            }

            modColorInput.Text = normalizedColor;
            SetStatus("Mod override staged.");
            RefreshModControls();
        }

        void RemoveModOverride()
        {
            if (ModNameStyleEditorRules.RemoveModColor(workingSettings, modKeyInput.Text))
            {
                modColorInput.Text = string.Empty;
                SetStatus("Mod override removed from staged settings.");
                RefreshModControls();
                return;
            }

            SetStatus("No override exists for that mod.", error: true);
        }

        bool TryStageCurrentInputsForSave()
        {
            if (!string.IsNullOrWhiteSpace(tagColorInput.Text))
            {
                if (!ModNameStyleEditorRules.TryNormalizeColor(tagColorInput.Text, out string normalizedTagColor))
                {
                    SetStatus("The selected tag color is not valid. Use #rgb, #rrggbb, or #rrggbbaa.", error: true);
                    SetSwatchColor(tagSwatch, tagColorInput.Text);
                    return false;
                }

                bool alreadyEffective =
                    TryGetEffectiveTagColor(workingSettings, selectedTag, out string effectiveTagColor) &&
                    ModNameStyleEditorRules.TryNormalizeColor(effectiveTagColor, out string normalizedEffectiveTagColor) &&
                    string.Equals(normalizedEffectiveTagColor, normalizedTagColor, StringComparison.OrdinalIgnoreCase);
                if (!alreadyEffective)
                {
                    string colorToStage = normalizedTagColor;
                    if (!ModNameStyleEditorRules.TrySetTagColor(
                            workingSettings,
                            selectedTag,
                            colorToStage,
                            out string stagedTag,
                            out string stagedColor))
                    {
                        SetStatus("The selected Workshop tag cannot be edited.", error: true);
                        return false;
                    }

                    selectedTag = stagedTag;
                    normalizedTagColor = stagedColor;
                }

                tagColorInput.Text = normalizedTagColor;
            }

            if (string.IsNullOrWhiteSpace(modColorInput.Text))
                return true;

            if (!ModNameStyleEditorRules.TrySetModColor(workingSettings, modKeyInput.Text, modColorInput.Text, out string normalizedModColor))
            {
                SetStatus("The mod override needs a mod id or name and a valid hex color.", error: true);
                SetSwatchColor(modSwatch, modColorInput.Text);
                return false;
            }

            modColorInput.Text = normalizedModColor;
            return true;
        }

        enabledToggle.Toggled += pressed =>
        {
            workingSettings.Enabled = pressed;
            SetStatus(pressed ? "Mod-name styling staged as enabled." : "Mod-name styling staged as disabled.");
            RefreshAll();
        };
        defaultTagToggle.Toggled += pressed =>
        {
            workingSettings.UseDefaultTagFormats = pressed;
            SetStatus(pressed ? "Default tag colors staged as enabled." : "Default tag colors staged as disabled.");
            RefreshAll();
        };
        tagDropdown.ItemSelected += index =>
        {
            selectedTag = tagDropdown.GetItemText((int)index);
            SetStatus(string.Empty);
            RefreshAll();
        };
        tagColorInput.TextSubmitted += _ => ApplyTagColor();
        tagColorInput.TextChanged += text => SetSwatchColor(tagSwatch, text);
        applyTagButton.Pressed += ApplyTagColor;
        disableTagButton.Pressed += DisableSelectedTag;
        resetTagButton.Pressed += ResetSelectedTag;
        modKeyInput.TextChanged += _ =>
        {
            SetStatus(string.Empty);
            RefreshModControls();
        };
        modColorInput.TextSubmitted += _ => ApplyModColor();
        modColorInput.TextChanged += text => SetSwatchColor(modSwatch, text);
        applyModButton.Pressed += ApplyModColor;
        removeModButton.Pressed += RemoveModOverride;
        resetAllButton.Pressed += () =>
        {
            ModNameStyleEditorRules.ResetToDefaults(workingSettings);
            selectedTag = ModNameStyleRules.GetDefaultTagFormats().Keys.First();
            SetStatus("Defaults staged.");
            RefreshAll();
        };
        saveButton.Pressed += () =>
        {
            if (!TryStageCurrentInputsForSave())
                return;

            if (SaveWorkingSettings(screen, previousSettings, workingSettings, onSaved))
                popup.Hide();
        };
        cancelButton.Pressed += () => popup.Hide();

        var body = CreateEditorBody(
            layout,
            enabledToggle,
            defaultTagToggle,
            tagDropdown,
            CreateColorControl(tagColorInput, tagSwatch, applyTagButton, layout),
            WrapPreview(tagPreview, layout),
            CreateButtonStrip(layout, disableTagButton, resetTagButton),
            modKeyInput,
            CreateColorControl(modColorInput, modSwatch, applyModButton, layout),
            WrapPreview(modPreview, layout),
            CreateButtonStrip(layout, removeModButton),
            CreateButtonStrip(layout, resetAllButton, saveButton, cancelButton),
            statusLabel);

        popup.AddChild(body);
        ApplyDialogButtons(popup, layout.ButtonFontSize);
        popup.GetOkButton().Visible = false;
        popup.GetCancelButton().Visible = false;
        screen.AddChild(popup);
        RefreshAll();
        popup.PopupCentered(new Vector2I(layout.PopupWidth, layout.PopupHeight));
    }

    private static bool SaveWorkingSettings(
        NModdingScreen screen,
        ModNameStyleSettings previousSettings,
        ModNameStyleSettings workingSettings,
        Action onSaved)
    {
        ProfileManager.ModNameStyles = ModNameStyleEditorRules.Clone(workingSettings);
        if (ProfileManager.SaveInMemoryState())
        {
            onSaved();
            return true;
        }

        ProfileManager.ModNameStyles = previousSettings;
        string error = string.IsNullOrWhiteSpace(ProfileManager.LastPersistenceError)
            ? "The style settings could not be saved. Your previous settings are still in memory."
            : "The style settings could not be saved. Your previous settings are still in memory.\n\nError:\n" + ProfileManager.LastPersistenceError;
        ModdingScreenDialogs.ShowInfoDialog(screen, "Styles Not Saved", error);
        return false;
    }

    private static Control CreateEditorBody(
        StyleEditorDialogLayout layout,
        Control enabledToggle,
        Control defaultTagToggle,
        Control tagDropdown,
        Control tagColorControl,
        Control tagPreview,
        Control tagActions,
        Control modKeyInput,
        Control modColorControl,
        Control modPreview,
        Control modActions,
        Control resetActions,
        Label statusLabel)
    {
        var shell = new PanelContainer
        {
            CustomMinimumSize = new Vector2(layout.PanelWidth, layout.ScrollHeight + 42),
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            SizeFlagsVertical = Control.SizeFlags.ExpandFill
        };
        ModdingScreenVanillaStyle.ApplyDialogPanel(shell);

        var outerMargins = new MarginContainer();
        outerMargins.AddThemeConstantOverride("margin_left", 12);
        outerMargins.AddThemeConstantOverride("margin_right", 12);
        outerMargins.AddThemeConstantOverride("margin_top", 10);
        outerMargins.AddThemeConstantOverride("margin_bottom", 10);

        var stack = new VBoxContainer
        {
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            SizeFlagsVertical = Control.SizeFlags.ExpandFill
        };
        stack.AddThemeConstantOverride("separation", 8);

        var scroll = new ScrollContainer
        {
            CustomMinimumSize = new Vector2(layout.PanelWidth - 36, layout.ScrollHeight),
            HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled,
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            SizeFlagsVertical = Control.SizeFlags.ExpandFill
        };
        var rows = new VBoxContainer
        {
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
        };
        rows.AddThemeConstantOverride("separation", 4);
        rows.AddChild(CreateEditorRow("Enabled", enabledToggle, layout));
        rows.AddChild(CreateEditorRow("Default Tags", defaultTagToggle, layout));
        rows.AddChild(CreateEditorRow("Workshop Tag", tagDropdown, layout));
        rows.AddChild(CreateEditorRow("Tag Color", tagColorControl, layout));
        rows.AddChild(CreateEditorRow("Tag Preview", tagPreview, layout));
        rows.AddChild(CreateEditorRow("Tag Actions", tagActions, layout));
        rows.AddChild(CreateEditorRow("Mod Key", modKeyInput, layout));
        rows.AddChild(CreateEditorRow("Mod Color", modColorControl, layout));
        rows.AddChild(CreateEditorRow("Mod Preview", modPreview, layout));
        rows.AddChild(CreateEditorRow("Mod Actions", modActions, layout));
        rows.AddChild(CreateEditorRow("Reset", resetActions, layout));
        scroll.AddChild(rows);

        stack.AddChild(scroll);
        stack.AddChild(statusLabel);
        outerMargins.AddChild(stack);
        shell.AddChild(outerMargins);
        return shell;
    }

    private static HBoxContainer CreateEditorRow(string labelText, Control settingControl, StyleEditorDialogLayout layout)
    {
        var row = new HBoxContainer
        {
            CustomMinimumSize = new Vector2(0, layout.RowHeight),
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
        };
        row.AddThemeConstantOverride("separation", 10);

        var label = new Label
        {
            Text = labelText,
            CustomMinimumSize = new Vector2(layout.LabelWidth, layout.RowHeight),
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
            SizeFlagsHorizontal = Control.SizeFlags.Fill
        };
        ModdingScreenVanillaStyle.ApplyLabel(label, muted: true);
        label.AddThemeFontSizeOverride("font_size", layout.BodyFontSize);
        row.AddChild(label);

        settingControl.CustomMinimumSize = new Vector2(
            Mathf.Max(settingControl.CustomMinimumSize.X, layout.SettingWidth),
            Mathf.Max(settingControl.CustomMinimumSize.Y, layout.RowHeight));
        settingControl.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        row.AddChild(settingControl);
        return row;
    }

    private static CheckButton CreateToggle(string text, bool pressed)
    {
        var toggle = new CheckButton
        {
            Text = text,
            ButtonPressed = pressed,
            CustomMinimumSize = new Vector2(324, ModdingScreenConstants.ToolbarControlHeight)
        };
        ModdingScreenVanillaStyle.ApplyButton(toggle);
        return toggle;
    }

    private static OptionButton CreateTagDropdown(string selectedTag, StyleEditorDialogLayout layout)
    {
        var dropdown = new OptionButton
        {
            CustomMinimumSize = new Vector2(layout.SettingWidth, ModdingScreenConstants.ToolbarControlHeight),
            TooltipText = "Supported Steam Workshop tag."
        };
        ModdingScreenVanillaStyle.ApplyOptionButton(dropdown);
        foreach (string tag in ModNameStyleRules.GetDefaultTagFormats().Keys)
            dropdown.AddItem(tag);
        SelectTag(dropdown, selectedTag);
        return dropdown;
    }

    private static LineEdit CreateColorInput(string placeholder, StyleEditorDialogLayout layout)
    {
        var input = new LineEdit
        {
            PlaceholderText = placeholder,
            CustomMinimumSize = new Vector2(Mathf.Max(220, layout.SettingWidth - 180), ModdingScreenConstants.ToolbarControlHeight),
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
        };
        ModdingScreenVanillaStyle.ApplyLineEdit(input);
        return input;
    }

    private static RichTextLabel CreatePreviewLabel(StyleEditorDialogLayout layout)
    {
        var preview = new RichTextLabel
        {
            BbcodeEnabled = true,
            FitContent = false,
            ScrollActive = false,
            ContextMenuEnabled = false,
            SelectionEnabled = false,
            CustomMinimumSize = new Vector2(layout.SettingWidth, layout.PreviewHeight),
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            SizeFlagsVertical = Control.SizeFlags.ExpandFill
        };
        ModdingScreenVanillaStyle.ApplyPreviewText(preview, layout.BodyFontSize);
        return preview;
    }

    private static Label CreateStatusLabel(StyleEditorDialogLayout layout)
    {
        var label = new Label
        {
            Text = string.Empty,
            Visible = false,
            CustomMinimumSize = new Vector2(0, 28),
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
        };
        ModdingScreenVanillaStyle.ApplyLabel(label);
        label.AddThemeFontSizeOverride("font_size", layout.BodyFontSize);
        return label;
    }

    private static Button CreateEditorButton(string text, StyleEditorDialogLayout layout, string tooltip)
    {
        var button = new Button
        {
            Text = text,
            TooltipText = tooltip,
            CustomMinimumSize = new Vector2(112, ModdingScreenConstants.ToolbarControlHeight)
        };
        ModdingScreenVanillaStyle.ApplyButton(button);
        button.AddThemeFontSizeOverride("font_size", layout.ButtonFontSize);
        return button;
    }

    private static PanelContainer CreateColorSwatch(StyleEditorDialogLayout layout)
    {
        var swatch = new PanelContainer
        {
            CustomMinimumSize = new Vector2(layout.SwatchSize, layout.SwatchSize),
            TooltipText = "Color preview."
        };
        ModdingScreenVanillaStyle.ApplySwatchPanel(swatch);

        var fill = new ColorRect
        {
            Name = "Fill",
            Color = new Color(0.16f, 0.16f, 0.16f, 1f),
            MouseFilter = Control.MouseFilterEnum.Ignore
        };
        fill.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        fill.OffsetLeft = 4;
        fill.OffsetRight = -4;
        fill.OffsetTop = 4;
        fill.OffsetBottom = -4;
        swatch.AddChild(fill);
        return swatch;
    }

    private static HBoxContainer CreateColorControl(LineEdit input, PanelContainer swatch, Button applyButton, StyleEditorDialogLayout layout)
    {
        var row = new HBoxContainer
        {
            CustomMinimumSize = new Vector2(layout.SettingWidth, layout.RowHeight),
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
        };
        row.AddThemeConstantOverride("separation", 8);
        row.AddChild(input);
        row.AddChild(swatch);
        row.AddChild(applyButton);
        return row;
    }

    private static HBoxContainer CreateButtonStrip(StyleEditorDialogLayout layout, params Button[] buttons)
    {
        var strip = new HBoxContainer
        {
            CustomMinimumSize = new Vector2(layout.SettingWidth, layout.RowHeight),
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
        };
        strip.AddThemeConstantOverride("separation", 8);
        foreach (var button in buttons)
            strip.AddChild(button);
        return strip;
    }

    private static PanelContainer WrapPreview(RichTextLabel preview, StyleEditorDialogLayout layout)
    {
        var panel = new PanelContainer
        {
            CustomMinimumSize = new Vector2(layout.SettingWidth, layout.PreviewHeight),
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
        };
        ModdingScreenVanillaStyle.ApplyDialogInsetPanel(panel);
        panel.AddChild(preview);
        return panel;
    }

    private static void ApplyDialogButtons(ConfirmationDialog popup, int fontSize)
    {
        var okButton = popup.GetOkButton();
        ModdingScreenVanillaStyle.ApplyButton(okButton);
        okButton.AddThemeFontSizeOverride("font_size", fontSize);
        okButton.CustomMinimumSize = new Vector2(Mathf.Max(okButton.CustomMinimumSize.X, 96), 40);

        var cancelButton = popup.GetCancelButton();
        ModdingScreenVanillaStyle.ApplyButton(cancelButton);
        cancelButton.AddThemeFontSizeOverride("font_size", fontSize);
        cancelButton.CustomMinimumSize = new Vector2(Mathf.Max(cancelButton.CustomMinimumSize.X, 96), 40);
    }

    private static StyleEditorDialogLayout GetLayoutForScreen(NModdingScreen screen)
    {
        var viewportSize = screen.GetViewportRect().Size;
        return ModdingScreenDialogRules.FitStyleEditorDialogToViewport(
            ModdingScreenDialogRules.GetPreferredStyleEditorDialogLayout(),
            (int)viewportSize.X,
            (int)viewportSize.Y);
    }

    private static void SelectTag(OptionButton dropdown, string tag)
    {
        for (int i = 0; i < dropdown.ItemCount; i++)
        {
            if (string.Equals(dropdown.GetItemText(i), tag, StringComparison.OrdinalIgnoreCase))
            {
                dropdown.Select(i);
                return;
            }
        }

        if (dropdown.ItemCount > 0)
            dropdown.Select(0);
    }

    private static void SetSwatchColor(PanelContainer swatch, string color)
    {
        var fill = swatch.GetNodeOrNull<ColorRect>("Fill");
        if (fill == null)
            return;

        if (TryParseColor(color, out Color parsedColor))
        {
            fill.Color = parsedColor;
            swatch.TooltipText = color.Trim();
            return;
        }

        fill.Color = new Color(0.16f, 0.16f, 0.16f, 1f);
        swatch.TooltipText = "No valid color selected.";
    }

    private static bool TryParseColor(string color, out Color parsedColor)
    {
        parsedColor = default;
        if (!ModNameStyleEditorRules.TryNormalizeColor(color, out string normalizedColor) ||
            !Color.HtmlIsValid(normalizedColor))
        {
            return false;
        }

        parsedColor = Color.FromHtml(normalizedColor);
        return true;
    }

    private static bool TryGetEffectiveTagColor(ModNameStyleSettings settings, string tag, out string color)
    {
        color = string.Empty;
        if (!ModNameStyleEditorRules.TryCanonicalizeTag(tag, out string supportedTag) ||
            IsTagDisabled(settings, supportedTag))
        {
            return false;
        }

        var tagOnlySettings = ModNameStyleEditorRules.Clone(settings);
        tagOnlySettings.ModFormats = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        return ModNameStyleRules.TryBuildSimpleColor(
            PreviewModId,
            PreviewDisplayName,
            new[] { supportedTag },
            tagOnlySettings,
            out color);
    }

    private static bool IsTagDisabled(ModNameStyleSettings settings, string supportedTag)
    {
        foreach (string tag in settings.DisabledTags ?? Enumerable.Empty<string>())
        {
            if (ModNameStyleEditorRules.TryCanonicalizeTag(tag, out string canonicalTag) &&
                string.Equals(canonicalTag, supportedTag, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private static bool TryGetModOverrideColor(ModNameStyleSettings settings, string modKey, out string color)
    {
        color = string.Empty;
        if (string.IsNullOrWhiteSpace(modKey) || settings.ModFormats == null)
            return false;

        foreach (var entry in settings.ModFormats)
        {
            if (string.Equals(entry.Key.Trim(), modKey.Trim(), StringComparison.OrdinalIgnoreCase) &&
                ModNameStyleEditorRules.TryNormalizeColor(entry.Value, out string normalizedColor))
            {
                color = normalizedColor;
                return true;
            }
        }

        return false;
    }

    private static string PickInitialModKey(NModdingScreen screen)
    {
        var session = ModdingScreenContext.GetSession(screen);
        if (!string.IsNullOrWhiteSpace(session.SelectedModId))
            return session.SelectedModId;

        return EnumerateVisibleRows(screen)
            .Select(row => row.Mod?.manifest?.id ?? string.Empty)
            .FirstOrDefault(id => !string.IsNullOrWhiteSpace(id)) ?? string.Empty;
    }

    private static string PickInitialTag(string modKey)
    {
        if (!string.IsNullOrWhiteSpace(modKey) &&
            ProfileManager.ModWorkshopTagsCache.TryGetValue(modKey, out var tags))
        {
            foreach (string tag in tags)
            {
                if (ModNameStyleEditorRules.TryCanonicalizeTag(tag, out string supportedTag))
                    return supportedTag;
            }
        }

        return ModNameStyleRules.GetDefaultTagFormats().Keys.First();
    }

    private static string FindDisplayName(NModdingScreen screen, string modKey)
    {
        foreach (var row in EnumerateVisibleRows(screen))
        {
            string id = row.Mod?.manifest?.id ?? string.Empty;
            string name = row.Mod?.manifest?.name ?? string.Empty;
            if (string.Equals(id, modKey, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(name, modKey, StringComparison.OrdinalIgnoreCase))
            {
                return string.IsNullOrWhiteSpace(name) ? id : name;
            }
        }

        return string.IsNullOrWhiteSpace(modKey) ? PreviewDisplayName : modKey;
    }

    private static IEnumerable<string> GetWorkshopTags(string modKey)
    {
        if (!string.IsNullOrWhiteSpace(modKey) &&
            ProfileManager.ModWorkshopTagsCache.TryGetValue(modKey, out var tags))
        {
            return tags;
        }

        return Enumerable.Empty<string>();
    }

    private static IEnumerable<NModMenuRow> EnumerateVisibleRows(NModdingScreen screen)
    {
        var modRowContainer = ModdingScreenNodeOps.GetModRowContainer(screen);
        return modRowContainer?.GetChildren().OfType<NModMenuRow>() ?? Enumerable.Empty<NModMenuRow>();
    }
}
