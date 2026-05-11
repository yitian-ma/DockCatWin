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

Custom asset packs live under:

```text
%APPDATA%\DockCatWin\AssetPacks
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
