# Untangling decision log

- Share save-format parsing between normal load and backup restore.
  - ProfileSaveStorage has two independent parsers for current and legacy formats with different exception handling.
- Keep config-path state in the path owner and change it only after a successful write.
  - The storage layer used to call a resolver callback before committing the file, and its cleanup could throw from the error handler.
- Put workshop-tag lookup and cache initialization behind one query.
  - Row and info patches duplicate lookup and initialization; the style editor reads the cache directly.
- Fetch Workshop tags asynchronously from a snapshot and publish on the screen thread.
  - Rendering and startup previously performed blocking network requests; a failed request was treated as a completed attempt.
  - A single Workshop item may contain several mods, but the old map retained only one mod per item.
  - Cache invalidation must refresh existing names and discard completions for earlier installations or resets.
- Make profile deletion and selection one transaction.
  - Deletion precedes the selection rollback snapshot, so a failed save can lose the deleted profile in memory.
- Put group assignment beside the other persisted group mutations.
  - The row patch separately implements assignment and save rollback.
- Share automatic-backup naming and retention policy where behavior matches.
  - Profile and settings backups separately implement timestamp/reason naming and retention.
- Correct tests to exercise platform-independent behavior.
  - Manifest lookup test assumes case-sensitive filenames, log test expects a value not in its fixture, and Linux opener test executes on Windows.
- Preserve distinct log-path casing until the filesystem resolves it.
  - Candidate deduplication previously removed valid case variants on case-sensitive filesystems.
- Test actual behavior instead of incidental formatting or ordering.
  - Build and resource tests now inspect evaluated MSBuild properties and items, uploader tests use a local executable on Windows, and localization tests check placeholder use rather than JSON key order.
  - Profile operations are tested through game-service fakes for both successful changes and failed persistence rollback.
- Remove pass-through and restating comments where they carry no independent contract.
  - Group search naming wrapper and several ProfileManager comments repeat their implementations.

The profile manager now owns one ProfileSaveData instance, eliminating separate field lists for capture and application. Existing public accessors remain available. Manifest and config extension lists have distinct ordering requirements. Compatibility retries, exception boundaries, and platform-specific branches represent external behavior and require evidence before removal. The Workshop-tag coordinator and profile transactions have isolated behavioral coverage. A headless Godot 4.5.1 smoke project exercised the real refresh node, active/inactive screen transitions, and window visibility signals with simulated game services. A separate headless project loaded the built mod and game assembly and verified real profile writes, failed-write cleanup and extension state, and reset behavior using a temporary directory. The reproducible GodotSmoke project tests the real style editor on a synthetic screen and runs the game's packed screen `_Ready` with the mod's Harmony postfix. The latter stubs localization and hotkeys and suppresses child lifecycle callbacks and backup, so it does not validate all game integration.
