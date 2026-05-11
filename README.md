# DockCatWin

DockCatWin is a first Windows port of DockCat. This repository starts with a minimal WPF desktop pet:

- transparent topmost window
- taskbar-edge positioning
- default image resources copied from DockCat
- walking/resting/transition animation
- drag support
- right-click menu and tray menu
- water and movement reminders
- speech bubble actions
- basic settings persistence
- custom asset pack folder with fallback to the default cat
- display selection and display-change clamping
- usage statistics and local data backup

Custom asset packs live under:

```text
%APPDATA%\DockCatWin\AssetPacks
```

Settings, usage statistics, and backups live under:

```text
%APPDATA%\DockCatWin
%APPDATA%\DockCatWin\DataBackup\user-data-backup.json
```

## Requirements

- Windows 10 or Windows 11
- .NET 8 SDK

## Run

```powershell
dotnet run --project .\DockCatWin\DockCatWin.csproj
```

## Build

```powershell
dotnet build .\DockCatWin\DockCatWin.csproj
```

## Publish

```powershell
.\scripts\publish-win.ps1
```

The publish output is written to:

```text
artifacts\DockCatWin
```
