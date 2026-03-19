using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using MegaCrit.Sts2.Core.Logging;

namespace BettermodmanagerUI.Data;

public class ModProfile
{
    public string Name { get; set; } = "Default";
    public List<string> LoadOrder { get; set; } = new();
    public Dictionary<string, string> ModGroups { get; set; } = new();
    public HashSet<string> DisabledGroups { get; set; } = new();
    public HashSet<string> DisabledMods { get; set; } = new();
}

public static class ProfileManager
{
    private static readonly Logger ModLogger = new("BetterModManagerUI", LogType.Generic);
    private static readonly string SavePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "SlayTheSpire2", "mod_profiles.json");

    public static List<ModProfile> Profiles { get; set; } = new();
    public static int CurrentProfileIndex { get; set; } = 0;

    public static ModProfile CurrentProfile
    {
        get
        {
            if (Profiles.Count == 0) Profiles.Add(new ModProfile { Name = "Default" });
            if (CurrentProfileIndex >= Profiles.Count) CurrentProfileIndex = 0;
            return Profiles[CurrentProfileIndex];
        }
    }

    public static void LoadProfiles()
    {
        try
        {
            if (File.Exists(SavePath))
            {
                var json = File.ReadAllText(SavePath);
                var loaded = JsonSerializer.Deserialize<List<ModProfile>>(json);
                if (loaded != null)
                {
                    Profiles = loaded;
                }
            }
            if (Profiles.Count == 0)
            {
                Profiles.Add(new ModProfile { Name = "Default" });
            }
        }
        catch (Exception ex)
        {
            ModLogger.Error("Failed to load mod profiles: " + ex.Message);
            Profiles.Add(new ModProfile { Name = "Default" });
        }
    }

    public static void SaveProfiles()
    {
        try
        {
            var folder = Path.GetDirectoryName(SavePath);
            if (!string.IsNullOrEmpty(folder) && !Directory.Exists(folder))
            {
                Directory.CreateDirectory(folder);
            }

            var options = new JsonSerializerOptions { WriteIndented = true };
            var json = JsonSerializer.Serialize(Profiles, options);
            File.WriteAllText(SavePath, json);
        }
        catch (Exception ex)
        {
            ModLogger.Error("Failed to save mod profiles: " + ex.Message);
        }
    }
}
