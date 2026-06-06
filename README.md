# Better Mod Menu

Better Mod Menu extends the [Slay the Spire 2](https://store.steampowered.com/app/2868840/Slay_the_Spire_2/) mod screen with profiles, custom groups, and saved ordering.

## Features

- Fixes the base menu's mod list overflow issue.
- Create, rename, delete, and switch between mod profiles.
- Organize mods into custom groups and toggle them together.
- Save a preferred mod order for the next launch.

## Requirements

- Slay the Spire 2 `0.99.1` or newer

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
- Cloud-capable builds can mirror backups and CSV exports to a synced directory when `CloudBackups` is enabled in the profile save:

```json
"CloudBackups": {
  "Enabled": true,
  "Directory": "C:\\Users\\you\\OneDrive\\BetterModMenu",
  "MirrorProfileBackups": true,
  "MirrorModSettingsBackups": true,
  "MirrorModListExports": true
}
```
- Game-version command previews use SteamDB-derived app, depot, and manifest ids from `GameVersionDownloads` in the profile save:

```json
"GameVersionDownloads": {
  "Enabled": true,
  "SteamCmdPath": "steamcmd",
  "InstallRootDirectory": "C:\\Games\\STS2 Versions",
  "SelectedVersion": "0.99.1",
  "Versions": [
    {
      "DisplayName": "0.99.1",
      "AppId": 2868840,
      "DepotId": 2868841,
      "ManifestId": 1234567890123456789
    }
  ]
}
```

The in-game `Game` action previews the SteamCMD command; it does not launch downloads itself.
In cloud-capable builds, the in-game `Cloud` action sets or clears the synced mirror folder.

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
Cloud-capable builds are opt-in and produce a separate `_cloud` archive:

```powershell
dotnet build BetterModMenu.csproj -p:Sts2DllPath="C:\path\to\sts2.dll" -p:PackageModOnBuild=true -p:IncludeCloudFeatures=true
```

Published GitHub releases can be mirrored to Nexus Mods through `publish-nexus-release.yml`.

## License

MIT. See [LICENSE](LICENSE).
