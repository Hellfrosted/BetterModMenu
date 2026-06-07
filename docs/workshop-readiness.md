# Steam Workshop Readiness

Last updated: 2026-06-07

STS2 does not currently expose a Workshop upload format in the public modding
notes. Keep Better Mod Menu ready for it by treating Workshop support as a
distribution change, not a feature rewrite.

## Current Assumptions

- The default package contains only `BetterModMenu.dll` and `BetterModMenu.json`.
- Optional cloud-capable builds stay separate with the `_cloud` suffix.
- Profile data defaults to the account-scoped `mod_data/BetterModMenu/` path,
  not the installed mod directory.
- Portable Mode is opt-in for manual installs or copied mod folders. Do not make
  it the default for Workshop installs because Workshop directories may be
  replaced or read-only.
- CSV and gameplay-flag metadata resolve current `<mod_id>.json` manifests,
  legacy `mod_manifest.json` manifests, and top-level manifests whose internal
  `id` matches the loaded mod.

## Future Workshop Work

When STS2 adds Workshop support, the expected work should be limited to:

- Add the Workshop upload metadata required by STS2 or Steam.
- Add or adjust a packaging target if Workshop requires a folder layout instead
  of the current release zip.
- Keep generated upload metadata out of the default local-only package unless
  STS2 requires it at runtime.
- Verify that profiles, groups, backups, CSV exports, and logs still use
  user-data paths by default after a Workshop install.
- Verify that Portable Mode remains optional and fails visibly if the Workshop
  folder cannot be written.
