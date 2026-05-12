# Task

- Task description: Validate the processed custom cat assets, sync valid resources into the bundled and local `my-cat` asset pack, then build, commit, and push to GitHub.
- Mode: Feature
- Target files:
  - `DockCatWin/Resources/MyCat/**`
  - `%APPDATA%/DockCatWin/AssetPacks/my-cat/**`
  - `review/task.md`
  - `review/codex_summary.md`
- Constraints:
  - Verify PNG transparency and dimensions before copying.
  - Preserve required asset pack structure and manifest.
  - Do not commit user desktop source files, only project resources.
  - Build verification should pass before commit.
