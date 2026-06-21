using System.Collections;
using System.Reflection;
using BetterModMenu.Data;
using MegaCrit.Sts2.Core.Nodes.Screens.ModdingScreen;

namespace BetterModMenu.Patches;

internal static class ModConfigProviderAdapter
{
    private const string RitsuFrameworkTypeName = "STS2RitsuLib.RitsuLibFramework";
    private const string RitsuNavigatorTypeName = "STS2RitsuLib.Settings.ModSettingsNavigator";
    private const string BaseLibConfigTypeName = "BaseLib.Config.BaseLibConfig";
    private const string BaseLibRegistryTypeName = "BaseLib.Config.ModConfigRegistry";
    private const string BaseLibSubmenuTypeName = "BaseLib.Config.UI.NModConfigSubmenu";

    public static ModConfigProviderKind GetProvider(string modId)
    {
        if (string.IsNullOrWhiteSpace(modId))
            return ModConfigProviderKind.None;

        try
        {
            return ModConfigProviderRules.SelectProvider(
                HasRitsuLibConfig(modId),
                HasBaseLibConfig(modId));
        }
        catch (Exception ex)
        {
            ProfileManager.ModLogger.Error($"Failed to detect config provider for '{modId}':\n{ex}");
            return ModConfigProviderKind.None;
        }
    }

    public static void Open(NModdingScreen screen, string modId, ModConfigProviderKind provider)
    {
        if (string.IsNullOrWhiteSpace(modId) || provider == ModConfigProviderKind.None)
            return;

        try
        {
            if (provider == ModConfigProviderKind.RitsuLib && TryOpenRitsuLib(modId))
                return;

            if (provider == ModConfigProviderKind.BaseLib && TryOpenBaseLib(screen, modId))
                return;
        }
        catch (Exception ex)
        {
            ProfileManager.ModLogger.Error($"Failed to open config for '{modId}':\n{ex}");
        }
    }

    private static bool HasRitsuLibConfig(string modId)
    {
        var frameworkType = FindType(RitsuFrameworkTypeName);
        var getRegistered = frameworkType?.GetMethod("GetRegisteredModSettings", BindingFlags.Public | BindingFlags.Static);
        if (getRegistered?.Invoke(null, null) is not IEnumerable pages)
            return false;

        foreach (var page in pages)
        {
            if (page == null)
                continue;

            var pageModId = page.GetType().GetProperty("ModId")?.GetValue(page) as string;
            if (string.Equals(pageModId, modId, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }

    private static bool HasBaseLibConfig(string modId)
    {
        object? config = GetBaseLibConfig(modId);
        if (config == null)
            return false;

        var visibleMethod = config.GetType().GetMethod("HasVisibleSettings", BindingFlags.Public | BindingFlags.Instance);
        return visibleMethod?.Invoke(config, null) is not bool visible || visible;
    }

    private static object? GetBaseLibConfig(string modId)
    {
        var registryType = FindType(BaseLibRegistryTypeName);
        var getMethod = registryType?.GetMethod("Get", BindingFlags.Public | BindingFlags.Static, [typeof(string)]);
        return getMethod?.Invoke(null, [modId]);
    }

    private static bool TryOpenRitsuLib(string modId)
    {
        var navigatorType = FindType(RitsuNavigatorTypeName);
        var requestOpen = navigatorType?.GetMethod("RequestOpenByIds", BindingFlags.Public | BindingFlags.Static);
        if (requestOpen == null)
            return false;

        requestOpen.Invoke(null, [modId, null, null, null]);
        return true;
    }

    private static bool TryOpenBaseLib(NModdingScreen screen, string modId)
    {
        if (GetBaseLibConfig(modId) == null)
            return false;

        var baseLibConfigType = FindType(BaseLibConfigTypeName);
        var lastModProperty = baseLibConfigType?.GetProperty("LastModConfigModId", BindingFlags.Public | BindingFlags.Static);
        lastModProperty?.SetValue(null, modId);

        var submenuType = FindType(BaseLibSubmenuTypeName);
        object? stack = FindField(screen.GetType(), "_stack")?.GetValue(screen);
        var pushMethod = stack?.GetType().GetMethod("PushSubmenuType", BindingFlags.Public | BindingFlags.Instance, [typeof(Type)]);
        if (submenuType == null || pushMethod == null)
            return false;

        pushMethod.Invoke(stack, [submenuType]);
        return true;
    }

    private static Type? FindType(string fullName)
    {
        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            var type = assembly.GetType(fullName, throwOnError: false, ignoreCase: false);
            if (type != null)
                return type;
        }

        return null;
    }

    private static FieldInfo? FindField(Type type, string name)
    {
        var current = type;
        while (current != null)
        {
            var field = current.GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (field != null)
                return field;

            current = current.BaseType;
        }

        return null;
    }
}
