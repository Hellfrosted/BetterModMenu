# Godot editor smoke test

This optional headless test uses Godot 4.5.1 .NET and the game's assembly. It
opens the real style editor on a synthetic mod screen and checks tag refresh,
user selection, and dialog cleanup. It does not open the game or write its saves.

Build the mod first, then set `STS2_DLL_PATH` to the game's `sts2.dll` and run:

```powershell
dotnet build BetterModMenu.Tests/GodotSmoke/GodotSmoke.csproj
& $godot --headless --path BetterModMenu.Tests/GodotSmoke --editor --quit
& $godot --headless --path BetterModMenu.Tests/GodotSmoke --quit-after 120
```

Set `$godot` to the Godot 4.5.1 .NET console executable. The final command must
exit successfully and print `REAL_EDITOR_SMOKE_OK`.

To exercise the mod's setup against the installed game's actual packed scene,
set `STS2_PCK_PATH` to `SlayTheSpire2.pck` and run the scene smoke test using a
.NET 9 runtime (the runtime bundled with the game). For a portable runtime, set
`DOTNET_ROOT` and `DOTNET_ROOT_X64` to its directory before starting Godot:

```powershell
& $godot --headless --path BetterModMenu.Tests/GodotSmoke res://SceneSmoke.tscn --quit-after 120
```

The test must print `GAME_SCENE_MOD_SETUP_OK`. It runs the screen's own `_Ready`
with localization and hotkeys stubbed, while suppressing child lifecycle
callbacks and automatic backup because the isolated project does not initialize
all game services. It runs the mod's Harmony postfix at the actual `_Ready`
boundary and checks repeat setup on the real scene; it does not replace testing
in the game.
