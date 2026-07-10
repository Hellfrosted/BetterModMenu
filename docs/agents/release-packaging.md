# Release And Packaging

Last updated: 2026-05-01

## Mod Manifest

- Keep `BetterModMenu.json` aligned with release changes.
- Preserve the mod id `BetterModMenu` unless explicitly asked to change it.
- `affects_gameplay` should remain `false` unless the mod starts changing gameplay behavior.

## Packaging

- Packaging is controlled by the `PackageModOnBuild` MSBuild property.
- Packaging copies `BetterModMenu.dll` and `BetterModMenu.json` into a versioned zip under `artifacts/`.
- Cloud-capable packaging is controlled separately by `IncludeCloudFeatures=true`; default release artifacts must remain local-only.
- Cloud-capable archives use the `_cloud` suffix so they remain optional alongside the default artifact.
- The Nexus upload workflow intentionally selects the default `BetterModMenu_v*.zip` archive and excludes `*_cloud.zip` sidecars.
- Nexus file descriptions should stay short: an automated upload line, the GitHub release URL, and a `Tested on <game version>` placeholder. Keep the player-facing changelog on the Nexus changelog tab instead.
- Do not update generated artifacts unless the user asks for a release or packaging task.

## Distribution

- GitHub releases can be mirrored to Nexus Mods through `.github/workflows/publish-nexus-release.yml`.
- Do not handle credentials, tokens, or session exports in repo files or markdown.
