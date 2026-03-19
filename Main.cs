using Godot;
using MegaCrit.Sts2.Core.Modding;
using MegaCrit.Sts2.Core.Logging;
using HarmonyLib;

namespace BettermodmanagerUI;

[ModInitializer("Initialize")]
public static class Main
{
    public const string ModId = "BettermodmanagerUI";
    public static MegaCrit.Sts2.Core.Logging.Logger Logger { get; } = new(ModId, LogType.Generic);

    public static void Initialize()
    {
        Logger.Info("Initializing BettermodmanagerUI...");
        
        // Load existing profiles from JSON (Optional, if you want it early)
        Data.ProfileManager.LoadProfiles();

        // Apply Harmony Patches
        var harmony = new Harmony(ModId);
        harmony.PatchAll();
        
        Logger.Info("BettermodmanagerUI initialized successfully.");
    }
}
