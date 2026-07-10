# Testing

Last updated: 2026-05-01

## Framework

- Tests use MSTest in `BetterModMenu.Tests/`.
- Prefer focused tests around pure rules, manifest scanning, persistence normalization, and path behavior.
- Keep tests independent of Slay the Spire 2 and Godot runtime when possible.

## When To Add Tests

- Add or update tests for changes to group/profile rules, manifest parsing, save-data normalization, and path safety.
- For UI patch changes that are hard to automate, extract pure logic into testable helpers when it is a small, natural fit.

## Verification

Use:

```bash
dotnet test BetterModMenu.Tests/BetterModMenu.Tests.csproj
```

If tests cannot run, report the command attempted and the blocking error.
