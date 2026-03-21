using Godot;
using MegaCrit.Sts2.Core.Modding;
using MegaCrit.Sts2.Core.Logging;
using HarmonyLib;

namespace BetterModMenu;

[ModInitializer("Initialize")]
public static class Main
{
    public const string ModId = "BetterModMenu";
    public static MegaCrit.Sts2.Core.Logging.Logger Logger { get; } = new(ModId, LogType.Generic);

    public static void Initialize()
    {
        Logger.Info("Initializing BetterModMenu...");
        Data.ProfileManager.LoadProfiles();

        var harmony = new Harmony(ModId);
        harmony.PatchAll();
        
        Logger.Info("BetterModMenu initialized successfully.");
    }
}
