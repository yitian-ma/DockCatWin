# Task

- Task description: Commit and publish the current custom-cat work, including bundling the user's `my-cat` asset pack inside the project so other users can run the app with the same cat resources.
- Mode: Feature
- Target files:
  - `DockCatWin/Core/Assets/AssetPackLoader.cs`
  - `DockCatWin/Resources/MyCat/**`
  - `README.md`
  - `review/codex_summary.md`
- Constraints:
  - Preserve existing AppData asset pack loading behavior.
  - Do not remove or overwrite user-local AppData resources during normal startup.
  - Bundled resource pack should be copied to `%APPDATA%\DockCatWin\AssetPacks\my-cat` only when absent, so users can customize it later.
  - Build verification should pass before commit.
  - Stage, commit, and push to GitHub after verification.
