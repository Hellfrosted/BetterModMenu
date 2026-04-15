# Better Mod Menu

Better Mod Menu extends the [Slay the Spire 2](https://store.steampowered.com/app/2868840/Slay_the_Spire_2/) mod screen with profiles, custom groups, and saved ordering.

## Features

- Fixes the base menu's mod list overflow issue.
- Create, rename, delete, and switch between mod profiles.
- Organize mods into custom groups and toggle them together.
- Save a preferred mod order for the next launch.

## Requirements

- Slay the Spire 2 public/stable and beta branches that expose either `ModManager.LoadedMods` or `ModManager.Mods`

## Installation

1. Download `BetterModMenu.dll` and `BetterModMenu.json` from the latest [release](../../releases).
2. Put both files in your Slay the Spire 2 `mods` folder.
3. Launch the game and open the `Modding` screen.

## Notes

- Load order changes are saved for the next launch.
- STS2 may still reorder dependency chains during startup.

## Save Data

- Portable Mode stores `mod_profiles.json`, `mod_profiles.jsonc`, or `mod_profiles.json5` beside the mod files.
- Otherwise, the file is stored under `mod_data/BetterModMenu/`.

## Build

Requires Godot 4, .NET 9, and access to `sts2.dll`.

```powershell
dotnet build BetterModMenu.csproj -p:Sts2DllPath="C:\path\to\sts2.dll"
```

To package the mod during build:

```powershell
dotnet build BetterModMenu.csproj -p:Sts2DllPath="C:\path\to\sts2.dll" -p:PackageModOnBuild=true
```

Packaged archives are written to `artifacts/`.

## License

MIT. See [LICENSE](LICENSE).
