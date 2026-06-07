# Better Mod Menu

Better Mod Menu extends the [Slay the Spire 2](https://store.steampowered.com/app/2868840/Slay_the_Spire_2/) mod screen with profiles, custom groups, saved ordering, local backups, CSV exports, and log viewing.

## Features

- Fixes the base menu's mod list overflow issue.
- Create, rename, delete, and switch between mod profiles.
- Organize mods into custom groups and toggle them together.
- Save a preferred mod order for the next launch.
- Back up Better Mod Menu profile data and the current enabled-mod settings.
- Export the installed mod list as an Excel-friendly CSV with versions, enabled state, and group names.
- View recent BetterModMenu/TTSMM log output from the mod screen.
- Reopen the first-launch tutorial from the in-game `Help` button.

## Requirements

- Slay the Spire 2 `0.99.1` or newer

## Installation

1. Download the latest `BetterModMenu_v*.zip` from [releases](../../releases).
2. Extract `BetterModMenu.dll` and `BetterModMenu.json` into your Slay the Spire 2 `mods` folder.
3. Launch the game and open the `Modding` screen.

## In-Game Controls

- `Profile` chooses a saved mod setup. Switching profiles turns mods on or off to match that setup.
- `New` copies the mods that are enabled right now into a new profile. `Rename`/`Edit` changes the selected profile name. `Del` deletes the selected profile, but it does not uninstall mods.
- `Portable Mode` saves Better Mod Menu data beside the mod files. Leave it off for the normal game save folder; turn it on when you want the same setup to travel with a copied game or mod folder.
- `Backup` saves copies of profiles, groups, and the game's current enabled-mod settings.
- `Load` shows available Better Mod Menu profile and group backups, newest first, and restores the one you choose. Installed mod files are not changed.
- `CSV` creates a spreadsheet-friendly installed-mod list with names, versions, enabled state, and group names.
- `Logs` opens recent BetterModMenu/TTSMM log output.
- `Help` reopens the tutorial popup.
- `Group` plus `Add` creates a custom group label. Use each mod row's group picker to put mods in that group.
- Group headers can collapse the section, move or rename the group, delete only the group label, or enable/disable every mod in the group.

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
Workshop readiness notes are tracked in [docs/workshop-readiness.md](docs/workshop-readiness.md).

## License

MIT. See [LICENSE](LICENSE).
