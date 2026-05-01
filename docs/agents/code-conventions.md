# Code Conventions

Last updated: 2026-05-01

## C#

- Keep nullable reference types enabled and respect existing nullability annotations.
- Prefer concise, simple C# that follows the surrounding file's style.
- Keep game-facing behavior in patch/session classes and reusable pure logic in `Data/` or rule/helper classes when that pattern already exists.
- Prefer named constants for shared UI text, group names, and behavioral constants.
- Avoid broad refactors unless they are required for the task.

## Project Structure

- `Data/`: profile state, manifest scanning, persistence, and portable-mode path logic.
- `Patches/`: Harmony/Godot mod screen integration and UI operations.
- `BetterModMenu.Tests/`: MSTest coverage for pure rules and file/path behavior.

## Dependencies

- Do not add dependencies without asking first.
- Prefer .NET and Godot APIs already available in the project.
