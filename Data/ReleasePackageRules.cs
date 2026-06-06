namespace BetterModMenu.Data;

internal static class ReleasePackageRules
{
    public const string CloudFeatureDefine = "BETTERMODMENU_CLOUD_FEATURES";
    public const string CloudPackageSuffix = "_cloud";

    public static string GetPackageBaseName(string assemblyName, string version, bool includeCloudFeatures)
    {
        string suffix = includeCloudFeatures ? CloudPackageSuffix : string.Empty;
        return $"{assemblyName}_v{version}{suffix}";
    }

    public static string GetCloudFeatureConstants(bool includeCloudFeatures)
    {
        return includeCloudFeatures ? CloudFeatureDefine : string.Empty;
    }

    public static bool IsCloudPackageFileName(string fileName)
    {
        return fileName.EndsWith($"{CloudPackageSuffix}.zip", StringComparison.OrdinalIgnoreCase);
    }

    public static bool IsDefaultPackageFileName(string fileName)
    {
        return fileName.StartsWith("BetterModMenu_v", StringComparison.OrdinalIgnoreCase) &&
            fileName.EndsWith(".zip", StringComparison.OrdinalIgnoreCase) &&
            !IsCloudPackageFileName(fileName);
    }
}
