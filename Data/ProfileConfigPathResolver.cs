using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Godot;
using MegaCrit.Sts2.Core.Saves;

namespace BetterModMenu.Data;

internal sealed class ProfileConfigPathResolver
{
    private readonly string _modId;
    private readonly string[] _configExtensions;
    private string _activeConfigExtension = ".json";

    public ProfileConfigPathResolver(string modId, params string[] configExtensions)
    {
        _modId = modId;
        _configExtensions = configExtensions;
    }

    public IReadOnlyList<string> ConfigExtensions => _configExtensions;

    public string PortableConfigDirectory => TryResolvePortableConfigDirectory(out string directory) ? directory : string.Empty;

    public string PortableConfigPath => TryGetPortableConfigPath(out string path) ? path : string.Empty;

    public string UserConfigDirectory => ResolveUserConfigDirectory(ensureDirectoryExists: false);

    public string UserConfigPath => ResolveConfigPath(ResolveUserConfigDirectory(ensureDirectoryExists: true));

    public string SavePath
    {
        get
        {
            if (TryGetPortableConfigPath(out string portablePath) && File.Exists(portablePath))
                return portablePath;

            return UserConfigPath;
        }
    }

    public string GetPortableConfigPathForExtension(string extension)
    {
        return TryGetPortableConfigPathForExtension(extension, out string path) ? path : string.Empty;
    }

    public string GetUserConfigPathForExtension(string extension)
    {
        return BuildConfigPath(ResolveUserConfigDirectory(ensureDirectoryExists: true), extension);
    }

    public void SetActiveConfigExtensionFromPath(string path)
    {
        _activeConfigExtension = NormalizeConfigExtension(Path.GetExtension(path));
    }

    public bool TryGetPortableConfigPath(out string path)
    {
        if (TryResolvePortableConfigDirectory(out string directory))
        {
            path = ResolveConfigPath(directory);
            return true;
        }

        path = string.Empty;
        return false;
    }

    public bool TryGetPortableConfigPathForExtension(string extension, out string path)
    {
        if (TryResolvePortableConfigDirectory(out string directory))
        {
            path = BuildConfigPath(directory, extension);
            return true;
        }

        path = string.Empty;
        return false;
    }

    public void DeleteOtherConfigVariants(string pathToKeep)
    {
        string? directory = Path.GetDirectoryName(pathToKeep);
        if (string.IsNullOrEmpty(directory))
            return;

        string fullKeepPath = Path.GetFullPath(pathToKeep);
        foreach (string extension in _configExtensions)
        {
            string candidate = BuildConfigPath(directory, extension);
            if (!Path.GetFullPath(candidate).Equals(fullKeepPath, StringComparison.OrdinalIgnoreCase) && File.Exists(candidate))
                File.Delete(candidate);
        }
    }

    private string NormalizeConfigExtension(string extension)
    {
        if (string.IsNullOrEmpty(extension))
            return _activeConfigExtension;

        string normalized = extension.StartsWith(".") ? extension.ToLowerInvariant() : "." + extension.ToLowerInvariant();
        return _configExtensions.Contains(normalized) ? normalized : _activeConfigExtension;
    }

    private string BuildConfigPath(string directory, string extension)
    {
        return Path.Combine(directory, "mod_profiles" + NormalizeConfigExtension(extension));
    }

    private string? FindExistingConfigPath(string directory)
    {
        if (string.IsNullOrEmpty(directory) || !Directory.Exists(directory))
            return null;

        foreach (string extension in _configExtensions)
        {
            string path = BuildConfigPath(directory, extension);
            if (File.Exists(path))
                return path;
        }

        return null;
    }

    private string ResolveConfigPath(string directory, bool ensureDirectoryExists = false)
    {
        if (ensureDirectoryExists && !string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            Directory.CreateDirectory(directory);

        string? existingPath = FindExistingConfigPath(directory);
        if (!string.IsNullOrEmpty(existingPath))
            return existingPath;

        return BuildConfigPath(directory, _activeConfigExtension);
    }

    private string ResolveUserConfigDirectory(bool ensureDirectoryExists)
    {
        string userPath = UserDataPathProvider.GetAccountScopedBasePath($"mod_data/{_modId}");
        string absolutePath = ProjectSettings.GlobalizePath(userPath);
        if (ensureDirectoryExists && !Directory.Exists(absolutePath))
            Directory.CreateDirectory(absolutePath);

        return absolutePath;
    }

    private bool TryResolvePortableConfigDirectory(out string directory)
    {
        directory = string.Empty;
        var mod = MegaCrit.Sts2.Core.Modding.ModManager.Mods.FirstOrDefault(candidate => candidate.manifest?.id == _modId);
        string path = (mod != null && !string.IsNullOrEmpty(mod.path))
            ? mod.path
            : (Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) ?? string.Empty);

        if (string.IsNullOrWhiteSpace(path))
            return false;

        if (Directory.Exists(path))
        {
            directory = Path.GetFullPath(path);
            return true;
        }

        if (File.Exists(path))
        {
            string? parentDirectory = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(parentDirectory) && Directory.Exists(parentDirectory))
            {
                directory = Path.GetFullPath(parentDirectory);
                return true;
            }
        }

        return false;
    }
}
