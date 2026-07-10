# Commands

Last updated: 2026-05-01

## Validation

Run the smallest command that proves the change:

```bash
dotnet test BetterModMenu.Tests/BetterModMenu.Tests.csproj
```

Use the solution-level test command when changes cross project boundaries:

```bash
dotnet test BetterModMenu.sln
```

There is no standalone typecheck command for this repository.

## Build

Do not run build commands unless specifically asked. Building the mod requires `sts2.dll`:

```bash
dotnet build BetterModMenu.csproj -p:Sts2DllPath="C:\path\to\sts2.dll"
```

If `STS2_DLL_PATH` is already set, the project file uses it automatically.

## Packaging

Do not package unless specifically asked:

```bash
dotnet build BetterModMenu.csproj -p:Sts2DllPath="C:\path\to\sts2.dll" -p:PackageModOnBuild=true
```

Packaged archives are written to `artifacts/`.

## Command Preferences

- Do not run dev server commands.
- Do not run build commands unless specifically asked.
- Avoid `npm` and `yarn`; they are not used in this repository.
