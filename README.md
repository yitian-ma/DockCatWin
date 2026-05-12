# DockCatWin

## Acknowledgements

This project is a native Windows port of the amazing macOS desktop pet app [DockCat](https://github.com/Auwuua/DockCat). All original concepts, logic design, and default cat UI assets belong to the original author. Huge thanks for their fantastic work!

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
UserData\AssetPacks
```

DockCatWin ships with a bundled `my-cat` asset pack under `DockCatWin\Resources\MyCat`. On startup, it is copied to `UserData\AssetPacks\my-cat` when that folder does not already exist, so users can edit their local copy without future runs overwriting it. *(Note: The resources currently in `my-cat` were created based on my own cat, Huihui (灰灰) — a Tabby and White American Shorthair boy! 🐾)*

Settings, usage statistics, and backups live under:

```text
UserData
UserData\DataBackup\user-data-backup.json
```

Outing collectable inventory is stored at:

```text
UserData\collectable-inventory.json
```

<details>
<summary>🎨 <strong>Advanced: Creating Custom Asset Packs</strong></summary>

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

PNG frames take priority. If no walking PNG frames are found, DockCatWin reads `video`, extracts `video_frame_count` evenly spaced frames, removes a pure green-screen background, normalizes the cat into centered bottom-aligned `1100 x 650` transparent frames, and caches the generated PNG files under `UserData\VideoCache`.

The green-screen keyer is a first-pass chroma key: solid, evenly lit chroma green works best. It makes strong green fully transparent, softens near-green edges, and reduces green spill on semi-transparent edges. Busy backgrounds, heavy shadows, green reflection on fur, or green/yellow details close to the background color may still need manual cleanup.

## Custom Held Size

The dragged/held pose can use its own display canvas size. This is useful when `poses\held\held.png` is wider than the default narrow held asset and would otherwise appear too small.

```json
"display_sizes": {
  "held": { "width": 650, "height": 1236 }
}
```

When omitted, DockCatWin uses the pack's normal `canvas_width` and `canvas_height` for the held state.
</details>

<details>
<summary>💻 <strong>Developer Guide (Building from Source)</strong></summary>

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

The output is fully portable and self-contained. You can zip the `artifacts\DockCatWin` folder and share it directly; users do not need to install the .NET runtime.
</details>
