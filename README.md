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
- outing/focus mode with recall, return events, and collectable rewards
- basic settings persistence
- custom asset pack folder with fallback to the default cat
- custom walking animation can use either PNG frames or a green-screen video source
- display selection and display-change clamping
- usage statistics and local data backup

Custom asset packs live under:

```text
%APPDATA%\DockCatWin\AssetPacks
```

DockCatWin ships with a bundled `my-cat` asset pack under `DockCatWin\Resources\MyCat`. On startup, it is copied to `%APPDATA%\DockCatWin\AssetPacks\my-cat` when that folder does not already exist, so users can edit their local copy without future runs overwriting it.

Settings, usage statistics, and backups live under:

```text
%APPDATA%\DockCatWin
%APPDATA%\DockCatWin\DataBackup\user-data-backup.json
```

Outing collectable inventory is stored at:

```text
%APPDATA%\DockCatWin\collectable-inventory.json
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

## Custom Walk Video

Custom asset packs can provide walking PNG frames under:

```text
animations\walk\walk_01.png
animations\walk\walk_02.png
...
```

Or they can provide a green-screen walking video and let DockCatWin extract transparent PNG frames into a local cache:

```json
"animations": {
  "walk": {
    "fps": 3,
    "video": "animations/walk/walk.mp4",
    "video_frame_count": 4,
    "frames": []
  }
}
```

PNG frames take priority. If no walking PNG frames are found, DockCatWin reads `video`, extracts `video_frame_count` evenly spaced frames, removes a pure green-screen background, normalizes the cat into centered bottom-aligned `1100 x 650` transparent frames, and caches the generated PNG files under `%APPDATA%\DockCatWin\VideoCache`.

The green-screen keyer is a first-pass chroma key: solid, evenly lit chroma green works best. It makes strong green fully transparent, softens near-green edges, and reduces green spill on semi-transparent edges. Busy backgrounds, heavy shadows, green reflection on fur, or green/yellow details close to the background color may still need manual cleanup.

## Custom Held Size

The dragged/held pose can use its own display canvas size. This is useful when `poses\held\held.png` is wider than the default narrow held asset and would otherwise appear too small.

```json
"display_sizes": {
  "held": { "width": 650, "height": 1236 }
}
```

When omitted, DockCatWin uses the pack's normal `canvas_width` and `canvas_height` for the held state.

## Publish

```powershell
.\scripts\publish-win.ps1
```

The publish output is written to:

```text
artifacts\DockCatWin
```
