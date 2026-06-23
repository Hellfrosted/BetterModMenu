# Better Mod Menu

Better Mod Menu extends the [Slay the Spire 2](https://store.steampowered.com/app/2868840/Slay_the_Spire_2/) mod screen with profiles, custom groups, saved ordering, local backups, CSV exports, and log viewing.

## Features

- Fixes the base menu's mod list overflow issue.
- Create, rename, delete, and switch between mod profiles.
- Organize mods into custom groups and toggle them together.
- Save a preferred mod order for the next launch.
- Back up Better Mod Menu profile data and the current enabled-mod settings, with Steam Workshop links when available.
- Export the installed mod list as an Excel-friendly CSV with versions, enabled state, group names, and Steam Workshop links when available.
- View full Better Mod Menu log output from the mod screen with warnings and errors highlighted, level filters, plus a shortcut to open the log folder.
- Color mod names from the supported Steam Workshop tags, and adjust tag or per-mod colors from the in-game `Style` editor.
- Reopen the first-launch tutorial from the in-game `Help` button.

## Requirements

- Slay the Spire 2 `0.107.1` or newer

## Installation

1. Download the latest `BetterModMenu_v*.zip` from [releases](../../releases).
2. Extract `BetterModMenu.dll` and `BetterModMenu.json` into your Slay the Spire 2 `mods` folder.
3. Launch the game and open Slay the Spire 2's built-in `Modding` screen. Better Mod Menu's `Profile`, `Group`, `Backup`, `CSV`, `Logs`, `Style`, and `Help` controls appear there.

## Where To Find It

Better Mod Menu does not add a separate title-screen button. Open the base game's `Modding` screen, then use the Better Mod Menu controls added to that screen.

## In-Game Controls

- `Profile` chooses a saved mod setup. Switching profiles turns mods on or off to match that setup.
- `New` copies the mods that are enabled right now into a new profile. `Rename`/`Edit` changes the selected profile name. `Del` deletes the selected profile, but it does not uninstall mods.
- `Portable Mode` saves Better Mod Menu data beside the mod files. Leave it off for the normal game save folder; turn it on when you want the same setup to travel with a copied game or mod folder.
- `Backup` saves copies of profiles, groups, and the game's current enabled-mod settings. Mods installed from Steam Workshop include their Workshop links in the settings backup.
- `Load` shows available Better Mod Menu profile and group backups, newest first, and restores the one you choose. Installed mod files are not changed.
- `CSV` creates a spreadsheet-friendly installed-mod list with names, versions, enabled state, group names, and Steam Workshop links when available.
- `Logs` opens the full discovered Better Mod Menu log with warnings and errors highlighted. The log viewer can show or hide debug, info, warning, error, and unclassified lines. Its `Open Folder` button opens the folder containing that log through the operating system file manager.
- `Style` opens the compact mod-name color editor. Use it to turn name styling on or off, keep or disable default tag colors, set a supported Steam Workshop tag color, or set a one-mod color override by mod id or displayed name.
- `Help` reopens the tutorial popup.
- `Group` plus `Add` creates a custom group label. Use each mod row's group picker to put mods in that group.
- Group headers can collapse the section, move or rename the group, delete only the group label, or enable/disable every mod in the group.
- Steam Workshop mods with recognized tags get colored names automatically. Local mods and untagged Workshop mods keep the normal name unless configured with `Style` or in save data.

## Notes

- Load order changes are saved for the next launch.
- STS2 may still reorder dependency chains during startup.
- On SteamOS, Bazzite, CachyOS, and similar Linux setups, `Open Folder` uses common desktop file openers such as `xdg-open`, `gio`, and KDE openers. It is most reliable in Desktop Mode; Gaming Mode or minimal window-manager sessions may not show a file manager.

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

Mod name styles are saved in the same profile file. The in-game `Style` editor covers common color editing for supported Steam Workshop tags and per-mod overrides. The canonical Steam Workshop tags are exactly: `<none selected>`, `Acts`, `Ancients`, `Audio`, `Cards`, `Characters`, `Cosmetics`, `Events`, `Expansion`, `Extensions`, `Humor`, `Modifiers`, `Monsters`, `Potions`, `QoL`, `Relics`, `Rooms`, `Tools & APIs`, `Utility`, and `Misc`. `ModFormats` targets a specific mod id or displayed mod name and wins before tag formatting, which is useful for favorite-mod easter eggs. Then `DisabledTags` removes supported tags, `TagFormats` overrides the remaining supported tag formats, and the effective `TagPriority` picks which matching tag wins. Custom normalized priority entries are tried first, then the default priority order is used as fallback, including when `UseDefaultTagFormats` is `false`. Common mechanical aliases normalize to canonical tags, such as `Quality of Life` to `QoL`, `Tools and APIs` to `Tools & APIs`, singular forms like `Card` to `Cards`, and `Miscellaneous` to `Misc`. Unsupported tag names are ignored. Values can be any Godot RichTextLabel BBCode template that includes `{name}`, or a bare hex color.
Set `Enabled` to `false` to keep the vanilla text and skip Workshop tag lookups.

```json
"ModNameStyles": {
  "Enabled": true,
  "UseDefaultTagFormats": true,
  "TagFormats": {
    "QoL": "[color=#80f0b0]{name}[/color]",
    "Characters": "[color=#ff7ad9][b]{name}[/b][/color]"
  },
  "TagPriority": [
    "QoL",
    "Tools & APIs",
    "Utility"
  ],
  "DisabledTags": [
    "Misc",
    "<none selected>"
  ],
  "ModFormats": {
    "Favorite.Mod": "[rainbow freq=0.8 sat=0.9 val=1.0]{name}[/rainbow]"
  }
}
```

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

To refresh a Steam Workshop uploader workspace during build:

```powershell
dotnet build BetterModMenu.csproj -p:Sts2DllPath="C:\path\to\sts2.dll" -p:ModUploaderWorkshopDir="E:\dev\STS2 mod\ModUploader-win-x64\workshop\BetterModMenu"
```

This copies the built `BetterModMenu.dll` and `BetterModMenu.json` into the
workspace `content` folder, leaving `workshop.json`, preview images, screenshots,
and `mod_id.txt` in place. `ModUploaderWorkshopDir` must point to the workshop
root that contains `workshop.json`, not the `content` folder itself. You can
also set `MOD_UPLOADER_WORKSHOP_DIR` instead of passing the build property every
time.

To build, refresh the ModUploader workspace content, and upload to Steam Workshop
from this repo without changing into the ModUploader directory:

```powershell
dotnet build BetterModMenu.csproj -t:UploadSteamWorkshop -p:Sts2DllPath="C:\path\to\sts2.dll"
```

By default, `UploadSteamWorkshop` looks for the uploader beside this repo at
`..\ModUploader-win-x64\ModUploader.exe` and uses
`..\ModUploader-win-x64\workshop\BetterModMenu`. Override those paths with:

```powershell
dotnet build BetterModMenu.csproj -t:UploadSteamWorkshop -p:Sts2DllPath="C:\path\to\sts2.dll" -p:ModUploaderRoot="E:\dev\STS2 mod\ModUploader-win-x64" -p:ModUploaderWorkshopDir="E:\dev\STS2 mod\ModUploader-win-x64\workshop\BetterModMenu"
```

`UploadSteamWorkshop` is explicit-only; ordinary `dotnet build` commands do not
upload to Steam. Use the Windows `dotnet` command from PowerShell or Windows
Terminal for real uploads; WSL path handoff to `ModUploader.exe` is not verified.
The upload target fails if the workspace `content` folder contains files other
than `BetterModMenu.dll` and `BetterModMenu.json`, so stale files are not
published by accident.

Cloud-capable builds are opt-in and produce a separate `_cloud` archive:

```powershell
dotnet build BetterModMenu.csproj -p:Sts2DllPath="C:\path\to\sts2.dll" -p:PackageModOnBuild=true -p:IncludeCloudFeatures=true
```

## License

MIT. See [LICENSE](LICENSE).
