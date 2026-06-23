using BetterModMenu.Data;
using Godot;
using MegaCrit.Sts2.Core.Localization;
using System.Reflection;

namespace BetterModMenu.Patches;

internal static class ModdingScreenText
{
    private const string ResourceFolder = "BetterModMenu.Localization";
    private static string? loadedLanguage;
    private static BmmLocalizationCatalog? catalog;

    public static string CurrentLanguageCode => ResolveCurrentLanguageCode();

    public static string Get(string key, string fallback)
    {
        return GetCatalog().Get(key, fallback);
    }

    public static string Format(string key, string fallback, params object[] args)
    {
        string template = Get(key, fallback);
        try
        {
            return string.Format(template, args);
        }
        catch (FormatException)
        {
            return string.Format(fallback, args);
        }
    }

    public static string LocalizeKnownError(string? error)
    {
        if (string.IsNullOrWhiteSpace(error))
            return string.Empty;

        return error switch
        {
            LogViewerService.ErrorNoLogFileFound => Get(BmmText.LogNotFoundKnownLocations, error),
            LogViewerService.ErrorLogFileDoesNotExist => Get(BmmText.LogNotFoundFileMissing, error),
            LogFolderOpenRules.ErrorNoLogFilePath => Get(BmmText.LogFolderNoFilePath, error),
            LogFolderOpenRules.ErrorLogFileNoLongerExists => Get(BmmText.LogFolderFileMissing, error),
            LogFolderOpenRules.ErrorLogFolderNoLongerExists => Get(BmmText.LogFolderFolderMissing, error),
            LogFolderOpenRules.ErrorNoFolderPath => Get(BmmText.LogFolderNoFolderPath, error),
            LogFolderOpenRules.ErrorFileManagerDidNotStart => Get(BmmText.LogFolderFileManagerNotStarted, error),
            ProfileSaveStorage.ErrorBackupFileNotFound => Get(BmmText.BackupErrorFileMissing, error),
            ProfileSaveStorage.ErrorBackupFileNoProfiles => Get(BmmText.BackupErrorNoProfiles, error),
            ProfileSaveStorage.ErrorNoWritableConfigPath => Get(BmmText.ErrorNoWritableConfigPath, error),
            ModSettingsBackupService.ErrorNoBackupDirectory => Get(BmmText.ErrorNoBackupDirectory, error),
            ModListExportBuilder.ErrorNoExportDirectory => Get(BmmText.ErrorNoExportDirectory, error),
            ProfileManager.ErrorRestoredProfileSaveCouldNotBeWritten => Get(BmmText.ErrorRestoredProfileSaveWriteFailed, error),
            _ => error
        };
    }

    private static BmmLocalizationCatalog GetCatalog()
    {
        string language = CurrentLanguageCode;
        if (catalog != null && string.Equals(loadedLanguage, language, StringComparison.OrdinalIgnoreCase))
            return catalog;

        catalog = BmmLocalization.LoadFromEmbeddedResources(Assembly.GetExecutingAssembly(), ResourceFolder, language);
        loadedLanguage = language;
        return catalog;
    }

    private static string ResolveCurrentLanguageCode()
    {
        try
        {
            string? language = LocManager.Instance?.Language;
            if (!string.IsNullOrWhiteSpace(language))
                return BmmLocalization.NormalizeLanguageCode(language);
        }
        catch
        {
            // Fall back to Godot's locale below.
        }

        try
        {
            return BmmLocalization.NormalizeLanguageCode(TranslationServer.GetLocale());
        }
        catch
        {
            return BmmLocalization.EnglishLanguageCode;
        }
    }
}
