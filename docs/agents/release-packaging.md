# Release And Packaging

Last updated: 2026-05-01

## Mod Manifest

- Keep `BetterModMenu.json` aligned with release changes.
- Preserve the mod id `BetterModMenu` unless explicitly asked to change it.
- `affects_gameplay` should remain `false` unless the mod starts changing gameplay behavior.

## Packaging

- Packaging is controlled by the `PackageModOnBuild` MSBuild property.
- Packaging copies `BetterModMenu.dll` and `BetterModMenu.json` into a versioned zip under `artifacts/`.
- Do not update generated artifacts unless the user asks for a release or packaging task.

## Distribution

- GitHub releases can be mirrored to Nexus Mods through `.github/workflows/publish-nexus-release.yml`.
- Do not handle credentials, tokens, or session exports in repo files or markdown.
