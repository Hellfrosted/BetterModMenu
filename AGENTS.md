# BetterModMenu Agent Guide

BetterModMenu is a C# Godot/.NET mod that extends the Slay the Spire 2 mod screen with profiles, custom groups, and saved ordering.

## Essentials

- Default validation: `dotnet test BetterModMenu.Tests/BetterModMenu.Tests.csproj`.
- Build when explicitly requested:
  `dotnet build BetterModMenu.csproj -p:Sts2DllPath="<path-to-sts2.dll>"`.
- Packaging and Steam Workshop upload are separate, explicit operations; follow
  [README.md](README.md) and do not infer permission to publish from a build request.

## Local release paths

- STS2 assembly: `D:\Program Files\Steam\steamapps\common\Slay the Spire 2\data_sts2_windows_x86_64\sts2.dll`
- ModUploader executable: `E:\dev\STS2 mod\ModUploader-win-x64\ModUploader.exe`
- Better Mod Menu Workshop workspace: `E:\dev\STS2 mod\ModUploader-win-x64\workshop\BetterModMenu`
- Workshop item ID: `3748029698` (stored in `mod_id.txt`)

Build and upload from PowerShell with:

```powershell
dotnet build BetterModMenu.csproj -t:UploadSteamWorkshop `
  -p:Sts2DllPath="D:\Program Files\Steam\steamapps\common\Slay the Spire 2\data_sts2_windows_x86_64\sts2.dll" `
  -p:ModUploaderRoot="E:\dev\STS2 mod\ModUploader-win-x64" `
  -p:ModUploaderWorkshopDir="E:\dev\STS2 mod\ModUploader-win-x64\workshop\BetterModMenu"
```

The upload target stages only `BetterModMenu.dll` and `BetterModMenu.json` in
the workspace `content` folder, then runs `ModUploader.exe upload -w` against
the workspace. Steam authentication is required.
