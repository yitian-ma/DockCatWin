# DockCatWin

DockCatWin is a first Windows port of DockCat. This repository starts with a minimal WPF desktop pet:

- transparent topmost window
- taskbar-edge positioning
- default image resources copied from DockCat
- simple walking/resting animation
- drag support
- right-click menu with exit

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
