using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

namespace BetterModMenu.Data;

internal readonly record struct LoadedModInfo(string Id, string Path);

internal static class Sts2ModManagerCompat
{
    private const BindingFlags StaticMemberFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;
    private const BindingFlags InstanceMemberFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

    public static IEnumerable<LoadedModInfo> GetLoadedMods()
    {
        object? mods =
            GetStaticMemberValue(typeof(MegaCrit.Sts2.Core.Modding.ModManager), "Mods") ??
            GetStaticMemberValue(typeof(MegaCrit.Sts2.Core.Modding.ModManager), "LoadedMods");

        if (mods is not IEnumerable loadedMods)
            yield break;

        foreach (object? mod in loadedMods)
        {
            if (mod == null)
                continue;

            object? manifest = GetMemberValue(mod, "manifest") ?? GetMemberValue(mod, "Manifest");
            string id = GetStringMemberValue(manifest, "id") ?? GetStringMemberValue(manifest, "Id") ?? string.Empty;
            string path = GetStringMemberValue(mod, "path") ?? GetStringMemberValue(mod, "Path") ?? string.Empty;

            if (!string.IsNullOrWhiteSpace(id) || !string.IsNullOrWhiteSpace(path))
                yield return new LoadedModInfo(id, path);
        }
    }

    public static bool TryGetModPath(string modId, out string path)
    {
        foreach (var mod in GetLoadedMods())
        {
            if (!string.Equals(mod.Id, modId, StringComparison.Ordinal) || string.IsNullOrWhiteSpace(mod.Path))
                continue;

            path = mod.Path;
            return true;
        }

        path = string.Empty;
        return false;
    }

    private static object? GetStaticMemberValue(Type type, string memberName)
    {
        try
        {
            var property = type.GetProperty(memberName, StaticMemberFlags);
            if (property != null)
                return property.GetValue(null);

            var field = type.GetField(memberName, StaticMemberFlags);
            return field?.GetValue(null);
        }
        catch
        {
            return null;
        }
    }

    private static object? GetMemberValue(object instance, string memberName)
    {
        try
        {
            var type = instance.GetType();
            var field = type.GetField(memberName, InstanceMemberFlags);
            if (field != null)
                return field.GetValue(instance);

            var property = type.GetProperty(memberName, InstanceMemberFlags);
            return property?.GetValue(instance);
        }
        catch
        {
            return null;
        }
    }

    private static string? GetStringMemberValue(object? instance, string memberName)
    {
        if (instance == null)
            return null;

        object? value = GetMemberValue(instance, memberName);
        return value as string;
    }
}
