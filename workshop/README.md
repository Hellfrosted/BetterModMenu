# Steam Workshop Release Prep

BetterModMenu uses MegaCrit's `sts2-mod-uploader` for Steam Workshop uploads.
This repository only prepares the uploader workspace; publishing remains a
manual release step because it requires an authenticated Steam session.

## One-time setup

1. Download `ModUploader` from the latest
   `megacrit/sts2-mod-uploader` release.
2. Add a 1:1 preview image at `workshop/image.png`.
3. Keep the Workshop item visibility set to `private` until it has been
   smoke-tested on the current Slay the Spire 2 main branch.

## Prepare the workspace

```powershell
dotnet build BetterModMenu.csproj -p:Sts2DllPath="C:\path\to\sts2.dll" -p:PrepareWorkshopWorkspaceOnBuild=true
```

The prepared workspace is written to:

```text
artifacts/workshop/BetterModMenu/
```

Its `content/` directory contains the runtime files uploaded to Workshop:

```text
BetterModMenu.dll
BetterModMenu.json
```

## Upload manually

Run the uploader from the directory containing `ModUploader.exe`:

```powershell
.\ModUploader.exe upload -w "C:\path\to\BetterModMenu\artifacts\workshop\BetterModMenu"
```

The uploader writes `mod_id.txt` after the first successful upload. Preserve
that ID for later updates, but do not publish a public Workshop item until the
private item has passed the smoke checklist below.

## Smoke checklist

- Subscribe to the private Workshop item.
- Start Slay the Spire 2 v0.107.1 or newer.
- Open the base game's `Modding` screen and confirm Better Mod Menu controls
  appear there without a separate title-screen entry point.
- Toggle a mod on and off and restart to confirm enabled state persists.
- Reorder mods and restart to confirm order persists, allowing dependency
  re-sorts by the base game.
- Create, switch, rename, and delete a profile.
- Create a custom group, assign mods to it, collapse it, and toggle the group.
- Run `Backup`, then confirm the mod-settings backup includes Workshop links
  for Workshop-installed mods before using `Load` on the newest profile backup.
- Run `CSV` and confirm the export appears in the expected user-data folder
  with Workshop links for Workshop-installed mods.
- Open `Logs`, confirm warning/error lines are highlighted, then use
  `Open Folder` and confirm the OS file manager opens the log directory.
- Toggle `Portable Mode` only after confirming the Workshop directory is
  writable; if it is not writable, confirm the failure is visible and does not
  corrupt the normal profile save.
