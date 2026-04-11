# Better Mod Menu

A UI enhancement mod for [Slay the Spire 2](https://store.steampowered.com/app/2868840/Slay_the_Spire_2/) that adds management features to the built-in mod menu.

![License](https://img.shields.io/badge/license-MIT-blue.svg)
![Version](https://img.shields.io/badge/version-0.0.4-green.svg)

## Features

- **UI Bug Fixes**: Fixes a vanilla game bug where the mod list visually spills outside of the menu window when you have many mods installed.
- **Mod Profiles**: Create, rename, delete, and switch between mod loadouts.
- **Custom Groups**: Organize mods into collapsible, named groups.
- **Group Toggles**: Enable or disable all mods within a group.
- **Load Order Manipulation**: Move mods up (`^`) and down (`v`) to adjust load order in-game.
- **Auto-Save**: Active profile, custom groups, and load order are saved automatically.

## Requirements

- **Slay the Spire 2** (Game Version 0.99.1+)

## Installation

1. Download the dll and json files from the latest release from the [Releases](../../releases) page.
2. Place `BetterModMenu.json` and `BetterModMenu.dll` into your Slay the Spire 2 `mods` folder.
3. Launch the game and open the Modding screen.

## Usage

- **Managing Profiles**: Use the top bar on the Modding screen to create `+ New` profiles, `Rename` the current profile, or `Del`ete it. Selecting a profile applies its saved mod states.
- **Managing Groups**: Use the `Group:` input at the top right to `+ Add` a new group. It will appear in the mod list.
- **Assigning Mods**: Next to each mod row, use the dropdown menu to assign it to a custom group or leave it "Unassigned".
- **Reordering Mods**: Click the `^` or `v` buttons on a mod's row to shift its position in the game's load order. *Note: When using Custom Groups, movement is based on the underlying global list. If a grouped mod doesn't visually move right away, keep clicking until it bypasses the other mods.*

## Building from Source

This mod is built using **Godot 4** and **.NET 9/C#**.

1. Clone this repository.
2. This project references `sts2.dll` directly from your game installation directory. If you are not using the default path, set `STS2_DLL_PATH` or pass `-p:Sts2DllPath="C:\path\to\sts2.dll"`.
3. Run `dotnet build` from the command line.
4. To create the release zip during build, add `-p:PackageModOnBuild=true`.

## Automated Checks

- Run the lightweight logic tests with `dotnet run --project BetterModMenu.Tests/BetterModMenu.Tests.csproj`.
- Build the mod with `dotnet build`.

## License

This project is licensed under the [MIT License](LICENSE).
