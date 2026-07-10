using System.Collections.ObjectModel;
using System.Reflection;
using System.Text.Json;

namespace BetterModMenu.Data;

internal static class BmmText
{
    public const string ProfileLabel = "PROFILE.LABEL";
    public const string ProfileTooltip = "PROFILE.TOOLTIP";
    public const string NewProfileTooltip = "PROFILE.NEW.TOOLTIP";
    public const string RenameProfileTooltip = "PROFILE.RENAME.TOOLTIP";
    public const string DeleteProfileTooltip = "PROFILE.DELETE.TOOLTIP";
    public const string PortableMode = "TOOLBAR.PORTABLE_MODE";
    public const string PortableModeTooltip = "TOOLBAR.PORTABLE_MODE.TOOLTIP";
    public const string Backup = "TOOLBAR.BACKUP";
    public const string BackupTooltip = "TOOLBAR.BACKUP.TOOLTIP";
    public const string Load = "TOOLBAR.LOAD";
    public const string LoadTooltip = "TOOLBAR.LOAD.TOOLTIP";
    public const string Csv = "TOOLBAR.CSV";
    public const string CsvTooltip = "TOOLBAR.CSV.TOOLTIP";
    public const string Logs = "TOOLBAR.LOGS";
    public const string LogsTooltip = "TOOLBAR.LOGS.TOOLTIP";
    public const string Style = "TOOLBAR.STYLE";
    public const string StyleTooltip = "TOOLBAR.STYLE.TOOLTIP";
    public const string Help = "TOOLBAR.HELP";
    public const string HelpTooltip = "TOOLBAR.HELP.TOOLTIP";
    public const string Cloud = "TOOLBAR.CLOUD";
    public const string CloudTooltip = "TOOLBAR.CLOUD.TOOLTIP";
    public const string SearchPlaceholder = "SEARCH.PLACEHOLDER";
    public const string SearchTooltip = "SEARCH.TOOLTIP";
    public const string SearchResultTooltip = "SEARCH.RESULT_TOOLTIP";
    public const string SearchResultFoundFormat = "SEARCH.RESULT.FOUND_FORMAT";
    public const string GroupLabel = "GROUP.LABEL";
    public const string GroupTooltip = "GROUP.TOOLTIP";
    public const string GroupUnassigned = "GROUP.UNASSIGNED";
    public const string GroupNamePlaceholder = "GROUP.NAME_PLACEHOLDER";
    public const string GroupNameTooltip = "GROUP.NAME_TOOLTIP";
    public const string AddGroup = "GROUP.ADD";
    public const string AddGroupTooltip = "GROUP.ADD.TOOLTIP";
    public const string RowMoveOrderTooltip = "ROW.MOVE_ORDER.TOOLTIP";
    public const string RowGroupDropdownTooltip = "ROW.GROUP_DROPDOWN.TOOLTIP";
    public const string GameplayImpactTooltip = "GAMEPLAY_IMPACT.TOOLTIP";
    public const string GroupEnableAllTooltip = "GROUP.ENABLE_ALL.TOOLTIP";
    public const string GroupDisableAllTooltip = "GROUP.DISABLE_ALL.TOOLTIP";
    public const string GroupRenameTooltip = "GROUP.RENAME.TOOLTIP";
    public const string GroupMoveUpTooltip = "GROUP.MOVE_UP.TOOLTIP";
    public const string GroupMoveDownTooltip = "GROUP.MOVE_DOWN.TOOLTIP";
    public const string GroupDeleteTooltip = "GROUP.DELETE.TOOLTIP";
    public const string GroupShowTooltip = "GROUP.SHOW.TOOLTIP";
    public const string GroupHideTooltip = "GROUP.HIDE.TOOLTIP";
    public const string DetailConfig = "DETAIL.CONFIG";
    public const string DetailConfigUnavailableTooltip = "DETAIL.CONFIG.UNAVAILABLE.TOOLTIP";
    public const string DetailConfigOpenTooltipFormat = "DETAIL.CONFIG.OPEN.TOOLTIP_FORMAT";
    public const string DetailProviderMod = "DETAIL.PROVIDER.MOD";
    public const string DetailSearchMatchTooltip = "DETAIL.SEARCH_MATCH.TOOLTIP";
    public const string DetailGameplayBadge = "DETAIL.GAMEPLAY.BADGE";
    public const string DetailNoMatchingMods = "DETAIL.NO_MATCHING_MODS";
    public const string SearchMatchModId = "SEARCH.MATCH.MOD_ID";
    public const string SearchMatchModName = "SEARCH.MATCH.MOD_NAME";
    public const string SearchMatchAuthor = "SEARCH.MATCH.AUTHOR";
    public const string SearchMatchVersion = "SEARCH.MATCH.VERSION";
    public const string SearchMatchGroup = "SEARCH.MATCH.GROUP";
    public const string SearchMatchState = "SEARCH.MATCH.STATE";
    public const string SearchMatchWorkshopId = "SEARCH.MATCH.WORKSHOP_ID";
    public const string SearchMatchWorkshopLink = "SEARCH.MATCH.WORKSHOP_LINK";
    public const string SearchMatchDescription = "SEARCH.MATCH.DESCRIPTION";
    public const string SearchMatchDependency = "SEARCH.MATCH.DEPENDENCY";
    public const string DialogLoadBackupTitle = "DIALOG.LOAD_BACKUP.TITLE";
    public const string DialogLoadBackupBody = "DIALOG.LOAD_BACKUP.BODY";
    public const string DialogLoadBackupBodyManyFormat = "DIALOG.LOAD_BACKUP.BODY_MANY_FORMAT";
    public const string DialogLoadBackupOrderTooltip = "DIALOG.LOAD_BACKUP.ORDER_TOOLTIP";
    public const string DialogLoadBackupNewerPageTooltip = "DIALOG.LOAD_BACKUP.NEWER_PAGE.TOOLTIP";
    public const string DialogLoadBackupOlderPageTooltip = "DIALOG.LOAD_BACKUP.OLDER_PAGE.TOOLTIP";
    public const string DialogRenameGroupTitle = "DIALOG.RENAME_GROUP.TITLE";
    public const string DialogRenameGroupHelp = "DIALOG.RENAME_GROUP.HELP";
    public const string DialogRenameProfileTitle = "DIALOG.RENAME_PROFILE.TITLE";
    public const string DialogRenameProfileHelp = "DIALOG.RENAME_PROFILE.HELP";
    public const string DialogCloudFolderTitle = "DIALOG.CLOUD_FOLDER.TITLE";
    public const string DialogCloudFolderHelp = "DIALOG.CLOUD_FOLDER.HELP";
    public const string LogCopyAll = "LOG.COPY_ALL";
    public const string LogCopyAllTooltip = "LOG.COPY_ALL.TOOLTIP";
    public const string LogOpenFolder = "LOG.OPEN_FOLDER";
    public const string LogOpenFolderTooltip = "LOG.OPEN_FOLDER.TOOLTIP";
    public const string LogLevels = "LOG.LEVELS";
    public const string LogLevelsTooltip = "LOG.LEVELS.TOOLTIP";
    public const string LogLevelDebug = "LOG.LEVEL.DEBUG";
    public const string LogLevelInfo = "LOG.LEVEL.INFO";
    public const string LogLevelWarn = "LOG.LEVEL.WARN";
    public const string LogLevelError = "LOG.LEVEL.ERROR";
    public const string LogLevelOther = "LOG.LEVEL.OTHER";
    public const string LogLevelTooltipFormat = "LOG.LEVEL.TOOLTIP_FORMAT";
    public const string LogFolderNotOpenedTitle = "LOG.FOLDER_NOT_OPENED.TITLE";
    public const string LogFolderNotOpenedGeneric = "LOG.FOLDER_NOT_OPENED.GENERIC";
    public const string LogFolderOsErrorFormat = "LOG.FOLDER_NOT_OPENED.OS_ERROR_FORMAT";
    public const string LogFolderNoFilePath = "LOG.FOLDER_NOT_OPENED.NO_FILE_PATH";
    public const string LogFolderFileMissing = "LOG.FOLDER_NOT_OPENED.FILE_MISSING";
    public const string LogFolderFolderMissing = "LOG.FOLDER_NOT_OPENED.FOLDER_MISSING";
    public const string LogFolderNoFolderPath = "LOG.FOLDER_NOT_OPENED.NO_FOLDER_PATH";
    public const string LogFolderFileManagerNotStarted = "LOG.FOLDER_NOT_OPENED.FILE_MANAGER_NOT_STARTED";
    public const string LogNotFoundTitle = "LOG.NOT_FOUND.TITLE";
    public const string LogNotFoundGeneric = "LOG.NOT_FOUND.GENERIC";
    public const string LogNotFoundKnownLocations = "LOG.NOT_FOUND.KNOWN_LOCATIONS";
    public const string LogNotFoundFileMissing = "LOG.NOT_FOUND.FILE_MISSING";
    public const string LogFileEmpty = "LOG.FILE_EMPTY";
    public const string TutorialTitleFormat = "TUTORIAL.TITLE_FORMAT";
    public const string TutorialIntro = "TUTORIAL.INTRO";
    public const string TutorialOpen = "TUTORIAL.OPEN";
    public const string TutorialProfiles = "TUTORIAL.PROFILES";
    public const string TutorialGroups = "TUTORIAL.GROUPS";
    public const string TutorialPortable = "TUTORIAL.PORTABLE";
    public const string TutorialBackup = "TUTORIAL.BACKUP";
    public const string TutorialStyle = "TUTORIAL.STYLE";
    public const string TutorialLogs = "TUTORIAL.LOGS";
    public const string TutorialCloud = "TUTORIAL.CLOUD";
    public const string BackupCreatedTitle = "BACKUP.CREATED.TITLE";
    public const string BackupCreatedMessageFormat = "BACKUP.CREATED.MESSAGE_FORMAT";
    public const string BackupNotCreatedTitle = "BACKUP.NOT_CREATED.TITLE";
    public const string BackupNoSaveMessage = "BACKUP.NO_SAVE.MESSAGE";
    public const string BackupFailedMessageFormat = "BACKUP.FAILED.MESSAGE_FORMAT";
    public const string BackupNotFoundTitle = "BACKUP.NOT_FOUND.TITLE";
    public const string BackupNoneFoundMessage = "BACKUP.NONE_FOUND.MESSAGE";
    public const string BackupFolderErrorFormat = "BACKUP.FOLDER_ERROR.MESSAGE_FORMAT";
    public const string BackupNotLoadedTitle = "BACKUP.NOT_LOADED.TITLE";
    public const string BackupNotLoadedMessage = "BACKUP.NOT_LOADED.MESSAGE";
    public const string BackupNotLoadedErrorFormat = "BACKUP.NOT_LOADED.ERROR_FORMAT";
    public const string BackupErrorFileMissing = "BACKUP.ERROR.FILE_MISSING";
    public const string BackupErrorNoProfiles = "BACKUP.ERROR.NO_PROFILES";
    public const string BackupReasonManual = "BACKUP.REASON.MANUAL";
    public const string BackupReasonAuto = "BACKUP.REASON.AUTO";
    public const string BackupReasonStartup = "BACKUP.REASON.STARTUP";
    public const string BackupReasonGeneric = "BACKUP.REASON.GENERIC";
    public const string BackupLoadedTitle = "BACKUP.LOADED.TITLE";
    public const string BackupLoadedMessageFormat = "BACKUP.LOADED.MESSAGE_FORMAT";
    public const string ExportFailedTitle = "EXPORT.FAILED.TITLE";
    public const string ExportFailedMessage = "EXPORT.FAILED.MESSAGE";
    public const string ExportFailedErrorFormat = "EXPORT.FAILED.ERROR_FORMAT";
    public const string CsvExportCreatedTitle = "CSV.CREATED.TITLE";
    public const string CsvExportCreatedMessageFormat = "CSV.CREATED.MESSAGE_FORMAT";
    public const string CloudBackupsTitle = "CLOUD.TITLE";
    public const string CloudOffMessage = "CLOUD.OFF.MESSAGE";
    public const string CloudOnMessageFormat = "CLOUD.ON.MESSAGE_FORMAT";
    public const string CloudSaveFailedFormat = "CLOUD.SAVE_FAILED.MESSAGE_FORMAT";
    public const string StyleTitle = "STYLE.TITLE";
    public const string StylePreviewMod = "STYLE.PREVIEW_MOD";
    public const string StyleEnabled = "STYLE.ENABLED";
    public const string StyleUseDefaults = "STYLE.USE_DEFAULTS";
    public const string StyleModKeyPlaceholder = "STYLE.MOD_KEY.PLACEHOLDER";
    public const string StyleApply = "STYLE.APPLY";
    public const string StyleDisableTag = "STYLE.DISABLE_TAG";
    public const string StyleResetTag = "STYLE.RESET_TAG";
    public const string StyleRemoveOverride = "STYLE.REMOVE_OVERRIDE";
    public const string StyleResetAll = "STYLE.RESET_ALL";
    public const string StyleSave = "STYLE.SAVE";
    public const string StyleCancel = "STYLE.CANCEL";
    public const string StyleApplyTagTooltip = "STYLE.APPLY_TAG.TOOLTIP";
    public const string StyleDisableTagTooltip = "STYLE.DISABLE_TAG.TOOLTIP";
    public const string StyleResetTagTooltip = "STYLE.RESET_TAG.TOOLTIP";
    public const string StyleApplyModTooltip = "STYLE.APPLY_MOD.TOOLTIP";
    public const string StyleRemoveModTooltip = "STYLE.REMOVE_MOD.TOOLTIP";
    public const string StyleResetAllTooltip = "STYLE.RESET_ALL.TOOLTIP";
    public const string StyleSaveTooltip = "STYLE.SAVE.TOOLTIP";
    public const string StyleCancelTooltip = "STYLE.CANCEL.TOOLTIP";
    public const string StyleStatusInvalidTagColor = "STYLE.STATUS.INVALID_TAG_COLOR";
    public const string StyleStatusTagColorStaged = "STYLE.STATUS.TAG_COLOR_STAGED";
    public const string StyleStatusChooseTag = "STYLE.STATUS.CHOOSE_TAG";
    public const string StyleStatusTagDisabled = "STYLE.STATUS.TAG_DISABLED";
    public const string StyleStatusTagReset = "STYLE.STATUS.TAG_RESET";
    public const string StyleStatusInvalidModColor = "STYLE.STATUS.INVALID_MOD_COLOR";
    public const string StyleStatusModOverrideStaged = "STYLE.STATUS.MOD_OVERRIDE_STAGED";
    public const string StyleStatusModOverrideRemoved = "STYLE.STATUS.MOD_OVERRIDE_REMOVED";
    public const string StyleStatusNoModOverride = "STYLE.STATUS.NO_MOD_OVERRIDE";
    public const string StyleStatusSelectedTagColorInvalid = "STYLE.STATUS.SELECTED_TAG_COLOR_INVALID";
    public const string StyleStatusTagCannotEdit = "STYLE.STATUS.TAG_CANNOT_EDIT";
    public const string StyleStatusModOverrideInvalid = "STYLE.STATUS.MOD_OVERRIDE_INVALID";
    public const string StyleStatusStylingEnabled = "STYLE.STATUS.STYLING_ENABLED";
    public const string StyleStatusStylingDisabled = "STYLE.STATUS.STYLING_DISABLED";
    public const string StyleStatusDefaultTagsEnabled = "STYLE.STATUS.DEFAULT_TAGS_ENABLED";
    public const string StyleStatusDefaultTagsDisabled = "STYLE.STATUS.DEFAULT_TAGS_DISABLED";
    public const string StyleStatusDefaultsStaged = "STYLE.STATUS.DEFAULTS_STAGED";
    public const string StyleSaveFailedTitle = "STYLE.SAVE_FAILED.TITLE";
    public const string StyleSaveFailedMessage = "STYLE.SAVE_FAILED.MESSAGE";
    public const string StyleSaveFailedErrorFormat = "STYLE.SAVE_FAILED.ERROR_FORMAT";
    public const string StyleRowEnabled = "STYLE.ROW.ENABLED";
    public const string StyleRowDefaultTags = "STYLE.ROW.DEFAULT_TAGS";
    public const string StyleRowWorkshopTag = "STYLE.ROW.WORKSHOP_TAG";
    public const string StyleRowTagColor = "STYLE.ROW.TAG_COLOR";
    public const string StyleRowTagPreview = "STYLE.ROW.TAG_PREVIEW";
    public const string StyleRowTagActions = "STYLE.ROW.TAG_ACTIONS";
    public const string StyleRowModKey = "STYLE.ROW.MOD_KEY";
    public const string StyleRowModColor = "STYLE.ROW.MOD_COLOR";
    public const string StyleRowModPreview = "STYLE.ROW.MOD_PREVIEW";
    public const string StyleRowModActions = "STYLE.ROW.MOD_ACTIONS";
    public const string StyleRowReset = "STYLE.ROW.RESET";
    public const string StyleTagDropdownTooltip = "STYLE.TAG_DROPDOWN.TOOLTIP";
    public const string StyleColorPreviewTooltip = "STYLE.COLOR_PREVIEW.TOOLTIP";
    public const string StyleNoValidColorTooltip = "STYLE.NO_VALID_COLOR.TOOLTIP";
    public const string ErrorNoWritableConfigPath = "ERROR.NO_WRITABLE_CONFIG_PATH";
    public const string ErrorNoBackupDirectory = "ERROR.NO_BACKUP_DIRECTORY";
    public const string ErrorNoExportDirectory = "ERROR.NO_EXPORT_DIRECTORY";
    public const string ErrorRestoredProfileSaveWriteFailed = "ERROR.RESTORED_PROFILE_SAVE_WRITE_FAILED";
}

internal sealed record BmmLanguage(string Code, string EnglishName);

internal static class BmmLocalization
{
    public const string EnglishLanguageCode = "eng";

    private static readonly BmmLanguage[] Languages =
    [
        new("eng", "English"),
        new("fra", "French"),
        new("ita", "Italian"),
        new("deu", "German"),
        new("esp", "Spanish - Spain"),
        new("jpn", "Japanese"),
        new("kor", "Korean"),
        new("pol", "Polish"),
        new("ptb", "Portuguese - Brazil"),
        new("rus", "Russian"),
        new("zhs", "Simplified Chinese"),
        new("spa", "Spanish - Latin America"),
        new("tha", "Thai"),
        new("tur", "Turkish"),
        new("vie", "Vietnamese")
    ];

    public static IReadOnlyList<BmmLanguage> SupportedLanguages => Languages;

    public static IReadOnlyList<string> SupportedLanguageCodes => Languages.Select(language => language.Code).ToArray();

    public static string NormalizeLanguageCode(string? language)
    {
        if (string.IsNullOrWhiteSpace(language))
            return EnglishLanguageCode;

        string text = language.Trim().Replace('-', '_').ToLowerInvariant();
        return text switch
        {
            "english" or "en" or "en_us" or "en_gb" or "eng" => "eng",
            "french" or "fr" or "fr_fr" or "fra" => "fra",
            "italian" or "it" or "it_it" or "ita" => "ita",
            "german" or "de" or "de_de" or "deu" => "deu",
            "spanish" or "spanish_spain" or "es" or "es_es" or "esp" => "esp",
            "japanese" or "ja" or "ja_jp" or "jpn" => "jpn",
            "koreana" or "korean" or "ko" or "ko_kr" or "kor" => "kor",
            "polish" or "pl" or "pl_pl" or "pol" => "pol",
            "brazilian" or "portuguese_brazil" or "pt" or "pt_br" or "ptb" => "ptb",
            "russian" or "ru" or "ru_ru" or "rus" => "rus",
            "schinese" or "simplified_chinese" or "zh" or "zh_cn" or "zh_hans" or "zh_sg" or "zhs" => "zhs",
            "latam" or "spanish_latin_america" or "es_419" or "es_mx" or "es_ar" or "es_cl" or "es_co" or "spa" => "spa",
            "thai" or "th" or "th_th" or "tha" => "tha",
            "turkish" or "tr" or "tr_tr" or "tur" => "tur",
            "vietnamese" or "vi" or "vi_vn" or "vie" => "vie",
            _ => text
        };
    }

    public static BmmLocalizationCatalog LoadFromEmbeddedResources(Assembly assembly, string resourceFolder, string language)
    {
        string normalizedLanguage = NormalizeLanguageCode(language);
        var english = LoadEmbeddedJson(assembly, resourceFolder, EnglishLanguageCode);
        var selected = normalizedLanguage == EnglishLanguageCode
            ? english
            : LoadEmbeddedJson(assembly, resourceFolder, normalizedLanguage);

        return new BmmLocalizationCatalog(normalizedLanguage, english, selected);
    }

    public static IReadOnlyDictionary<string, string> LoadJsonFile(string path)
    {
        using var stream = File.OpenRead(path);
        return LoadJson(stream);
    }

    public static IReadOnlyList<string> FindCoverageErrors(
        IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> catalogs)
    {
        var errors = new List<string>();
        if (!catalogs.TryGetValue(EnglishLanguageCode, out var english))
        {
            errors.Add("Missing English localization catalog.");
            return errors;
        }

        foreach (var language in Languages)
        {
            if (!catalogs.TryGetValue(language.Code, out var catalog))
            {
                errors.Add(language.Code + " is missing a localization catalog.");
                continue;
            }

            foreach (string key in english.Keys.OrderBy(key => key, StringComparer.Ordinal))
            {
                if (!catalog.TryGetValue(key, out string? value))
                {
                    errors.Add(language.Code + " is missing key " + key + ".");
                    continue;
                }

                if (string.IsNullOrWhiteSpace(value))
                    errors.Add(language.Code + " has an empty value for key " + key + ".");
            }

            foreach (string extraKey in catalog.Keys.Except(english.Keys).OrderBy(key => key, StringComparer.Ordinal))
                errors.Add(language.Code + " has unknown key " + extraKey + ".");
        }

        return errors;
    }

    private static IReadOnlyDictionary<string, string> LoadEmbeddedJson(Assembly assembly, string resourceFolder, string language)
    {
        string resourceName = resourceFolder + "." + language + ".json";
        using Stream? stream = assembly.GetManifestResourceStream(resourceName);
        if (stream == null)
            return new ReadOnlyDictionary<string, string>(new Dictionary<string, string>(StringComparer.Ordinal));

        return LoadJson(stream);
    }

    private static IReadOnlyDictionary<string, string> LoadJson(Stream stream)
    {
        var entries = JsonSerializer.Deserialize<Dictionary<string, string>>(stream);
        return new ReadOnlyDictionary<string, string>(entries ?? new Dictionary<string, string>(StringComparer.Ordinal));
    }
}

internal sealed class BmmLocalizationCatalog(
    string language,
    IReadOnlyDictionary<string, string> english,
    IReadOnlyDictionary<string, string> selected)
{
    public string Language { get; } = language;

    public string Get(string key, string fallback)
    {
        if (selected.TryGetValue(key, out string? value) && !string.IsNullOrWhiteSpace(value))
            return value;

        if (english.TryGetValue(key, out string? englishValue) && !string.IsNullOrWhiteSpace(englishValue))
            return englishValue;

        return fallback;
    }
}
