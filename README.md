# Better Mod Menu

Better Mod Menu extends the [Slay the Spire 2](https://store.steampowered.com/app/2868840/Slay_the_Spire_2/) mod screen with profiles, custom groups, saved ordering, local backups, CSV exports, log viewing, and game-version command previews.

## Features

- Fixes the base menu's mod list overflow issue.
- Create, rename, delete, and switch between mod profiles.
- Organize mods into custom groups and toggle them together.
- Save a preferred mod order for the next launch.
- Back up Better Mod Menu profile data and the current enabled-mod settings.
- Export the installed mod list as an Excel-friendly CSV with versions and links when manifests provide them.
- View recent BetterModMenu/TTSMM log output from the mod screen.
- Reopen the first-launch tutorial from the in-game `Help` button.
- Preview a SteamCMD command for configured Slay the Spire 2 game-version downloads.

## Requirements

- Slay the Spire 2 `0.99.1` or newer

## Installation

1. Download the latest `BetterModMenu_v*.zip` from [releases](../../releases).
2. Extract `BetterModMenu.dll` and `BetterModMenu.json` into your Slay the Spire 2 `mods` folder.
3. Launch the game and open the `Modding` screen.

## In-Game Controls

- `Profile` switches between saved enabled-mod sets. `New`, `Edit`, and `Del` manage profiles.
- `Portable Mode` stores profile data beside the mod files instead of under game mod data.
- `Backup` snapshots Better Mod Menu profile data and current game mod enabled settings.
- `CSV` exports the installed mod list.
- `Logs` opens recent BetterModMenu/TTSMM log output.
- `Help` reopens the tutorial popup.
- `Game` previews the SteamCMD command for the selected configured game version.
- `Group` plus `Add` creates a custom group. Group headers can rename, move, delete, collapse, and enable or disable all mods in that group.

## Notes

- Load order changes are saved for the next launch.
- STS2 may still reorder dependency chains during startup.
- The `Game` action only previews the SteamCMD command; it does not launch SteamCMD or download game files.

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
