using HarmonyLib;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Modding;

namespace BetterModMenu;

[ModInitializer("Initialize")]
public static class Main
{
    public const string ModId = "BetterModMenu";
    public static Logger Logger { get; } = new(ModId, LogType.Generic);

    public static void Initialize()
    {
        Logger.Info("Initializing BetterModMenu...");
        Data.ProfileManager.LoadProfiles();
        Data.ProfileManager.BuildManifestCache();
        Data.ProfileManager.NormalizePersistedStateAndSaveIfNeeded();

        var harmony = new Harmony(ModId);
        harmony.PatchAll();
        
        Logger.Info("BetterModMenu initialized successfully.");
    }
}
