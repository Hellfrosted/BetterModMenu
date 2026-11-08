using Godot;
using HarmonyLib;
using System.Reflection;
using BetterModMenu.Data;
using BetterModMenu.Patches;
using MegaCrit.Sts2.Core.Nodes.Screens.ModdingScreen;

public partial class SceneSmoke : Node
{
    private NModdingScreen? _screen;
    private Harmony? _harmony;
    private int _stage;
    private static bool ScreenReadyCompleted;
    public static void MarkScreenReady() => ScreenReadyCompleted = true;
    public static bool SkipGameCallbacks() => false;
    public static bool StubLocale(ref string __result) { __result = "Smoke"; return false; }
    public static bool SkipBackup(ref bool __result) { __result = false; return false; }
    public override void _Ready()
    {
        try
        {
            string pack = System.Environment.GetEnvironmentVariable("STS2_PCK_PATH") ?? throw new Exception("Set STS2_PCK_PATH to the game pack");
            if (!ProjectSettings.LoadResourcePack(pack, false)) throw new Exception("Game pack could not be loaded");
            var bridge = typeof(GodotObject).Assembly.GetType("Godot.Bridge.ScriptManagerBridge", true)!;
            bridge.GetMethod("LookupScriptsInAssembly", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)!.Invoke(null, [typeof(NModdingScreen).Assembly]);
            var scene = ResourceLoader.Load<PackedScene>("res://scenes/screens/modding/modding_screen.tscn") ?? throw new Exception("Scene not found");
            _screen = scene.Instantiate<NModdingScreen>();
            _harmony = new Harmony("bmm.isolated.scene.smoke");
            var prefix = new HarmonyMethod(typeof(SceneSmoke).GetMethod(nameof(SkipGameCallbacks))!);
            var patched = new HashSet<MethodBase>();
            foreach (var concreteType in Descendants(_screen).Prepend(_screen).Select(node => node.GetType()).Distinct())
            {
                for (var type = concreteType; type != null && type.Assembly == typeof(NModdingScreen).Assembly; type = type.BaseType)
                {
                    foreach (string name in new[] { "_Ready", "_ExitTree", "_Process" })
                    {
                        if (type == typeof(NModdingScreen) && name == "_Ready") continue;
                        var method = type.GetMethod(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
                        if (method != null && patched.Add(method)) _harmony.Patch(method, prefix: prefix);
                    }
                }
            }
            var buttonType = typeof(NModdingScreen).Assembly.GetType("MegaCrit.Sts2.Core.Nodes.GodotExtensions.NButton", true)!;
            foreach (string name in new[] { "RegisterHotkeys", "UnregisterHotkeys" })
                _harmony.Patch(buttonType.GetMethod(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)!, prefix: prefix);
            var localization = typeof(NModdingScreen).Assembly.GetType("MegaCrit.Sts2.Core.Localization.LocString", true)!;
            foreach (var method in localization.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).Where(method => method.Name == "GetFormattedText" && method.ReturnType == typeof(string)))
                _harmony.Patch(method, prefix: new HarmonyMethod(typeof(SceneSmoke).GetMethod(nameof(StubLocale))!));
            _harmony.Patch(typeof(NModdingScreen).GetMethod("_Ready")!, postfix: new HarmonyMethod(typeof(SceneSmoke).GetMethod(nameof(MarkScreenReady))!));
            _harmony.Patch(typeof(NModdingScreen).GetMethod("_Ready")!, postfix: new HarmonyMethod(typeof(NModdingScreenPatch).GetMethod(nameof(NModdingScreenPatch.Postfix_Ready))!));
            var backup = typeof(ProfileManager).GetMethod("TryBackupResumeOnce", BindingFlags.Static | BindingFlags.NonPublic)!;
            _harmony.Patch(backup, prefix: new HarmonyMethod(typeof(SceneSmoke).GetMethod(nameof(SkipBackup))!));
            ProfileManager.ResetState();
            ProfileManager.ModNameStyles.Enabled = false;
            CallDeferred(Node.MethodName.AddChild, _screen);
        }
        catch (Exception ex) { GD.PushError(ex.ToString()); GetTree().Quit(1); }
    }
    public override void _Process(double delta)
    {
        try
        {
            if (_screen == null || !_screen.IsInsideTree()) return;
            if (_stage++ == 0)
            {
                if (!ScreenReadyCompleted) throw new Exception("Game screen _Ready did not complete");
                var assembly = typeof(ProfileManager).Assembly;
                var context = assembly.GetType("BetterModMenu.Patches.ModdingScreenContext", true)!;
                var session = context.GetMethod("GetSession", BindingFlags.Static | BindingFlags.Public)!.Invoke(null, [_screen])!;
                var chrome = session.GetType().GetProperty("ChromeRoot")!.GetValue(session) as Control;
                if (chrome == null || !GodotObject.IsInstanceValid(chrome)) throw new Exception("Chrome root missing");
                foreach (string name in new[] { "BetterModMenuTopBar", "BetterModMenuGroupBar", "BetterModMenuSearchBar" })
                    if (chrome.GetNodeOrNull(name) == null) throw new Exception(name + " missing");
                if (_screen.GetNodeOrNull("BetterModMenuWorkshopTagRefresh") == null) throw new Exception("Workshop refresh node missing");
                NModdingScreenPatch.Postfix_Ready(_screen);
                if (chrome.GetChildren().Count(node => node.Name == "BetterModMenuTopBar") != 1) throw new Exception("Repeated setup duplicated the top bar");
                if (_screen.GetChildren().Count(node => node.Name == "BetterModMenuWorkshopTagRefresh") != 1) throw new Exception("Repeated setup duplicated the refresh node");
                GD.Print("GAME_SCENE_MOD_SETUP_OK");
                GetTree().Quit();
            }
        }
        catch (Exception ex) { GD.PushError(ex.ToString()); GetTree().Quit(1); }
    }
    private static IEnumerable<Node> Descendants(Node node)
    {
        foreach (Node child in node.GetChildren())
        {
            yield return child;
            foreach (Node descendant in Descendants(child)) yield return descendant;
        }
    }
}
