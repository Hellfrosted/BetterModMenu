using System;
using System.Runtime.CompilerServices;
using Godot;
using MegaCrit.Sts2.Core.Nodes.Screens.ModdingScreen;

namespace BetterModMenu.Patches;

internal static class ModdingScreenContext
{
    private static WeakReference<NModdingScreen>? _currentScreenRef;
    private static readonly ConditionalWeakTable<NModdingScreen, ModdingScreenSession> Sessions = new();

    public static void TrackCurrentScreen(NModdingScreen screen)
    {
        _currentScreenRef = new(screen);
    }

    public static void ReleaseScreen(NModdingScreen screen)
    {
        if (IsCurrentScreen(screen))
            _currentScreenRef = null;

        GetSession(screen).Reset();
    }

    public static bool IsCurrentScreen(NModdingScreen? screen)
    {
        return screen != null &&
            _currentScreenRef?.TryGetTarget(out var current) == true &&
            current == screen &&
            GodotObject.IsInstanceValid(screen);
    }

    public static bool TryGetCurrentScreen(out NModdingScreen? screen)
    {
        if (_currentScreenRef?.TryGetTarget(out screen) == true && GodotObject.IsInstanceValid(screen))
            return true;

        screen = null;
        return false;
    }

    public static ModdingScreenSession GetSession(NModdingScreen screen)
    {
        return Sessions.GetOrCreateValue(screen);
    }

    public static bool IsAutoSaveSuppressed(NModdingScreen screen)
    {
        return GetSession(screen).AutoSaveSuppressionDepth > 0;
    }

    public static bool IsTickboxHandlerSuppressed(NModdingScreen screen)
    {
        return GetSession(screen).TickboxSuppressionDepth > 0;
    }
}
