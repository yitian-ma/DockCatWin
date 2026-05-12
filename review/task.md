# Task

- Task description: Resize `walk_01.png` and `walk_02.png` so their cat subject scale matches `walk_03.png` and `walk_04.png`, then sync the corrected assets into the bundled and local `my-cat` asset packs.
- Mode: Feature
- Target files:
  - `C:\Users\17569\Desktop\去背景\processed\walk_01.png`
  - `C:\Users\17569\Desktop\去背景\processed\walk_02.png`
  - `DockCatWin/Resources/MyCat/animations/walk/walk_01.png`
  - `DockCatWin/Resources/MyCat/animations/walk/walk_02.png`
  - `%APPDATA%/DockCatWin/AssetPacks/my-cat/animations/walk/walk_01.png`
  - `%APPDATA%/DockCatWin/AssetPacks/my-cat/animations/walk/walk_02.png`
  - `review/task.md`
  - `review/codex_summary.md`
- Constraints:
  - Preserve transparent PNG output and `1100 x 650` canvas size.
  - Preserve aspect ratio; do not stretch the cat.
  - Keep feet bottom-aligned with existing walk frames.
  - Build verification should pass after syncing project resources.
