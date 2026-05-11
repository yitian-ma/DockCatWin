# Codex Summary

## Changed Files

- `README.md`
- `DockCatWin/MainWindow.xaml`
- `DockCatWin/MainWindow.xaml.cs`
- `DockCatWin/SettingsWindow.xaml`
- `DockCatWin/SettingsWindow.xaml.cs`
- `DockCatWin/Core/Assets/AssetPackLoader.cs`
- `DockCatWin/Core/Assets/CatAssetPack.cs`
- `DockCatWin/Core/Reminder/ReminderType.cs`
- `DockCatWin/Core/Reminder/ReminderScheduler.cs`
- `DockCatWin/UI/CatWindow/CatWindowController.cs`
- `DockCatWin/UI/Tray/TrayIconController.cs`
- `review/task.md`
- `review/codex_summary.md`

## Summary of Edits

- Added outing/focus mode with duration prompt, departure confirmation, walk-out, away state, recall, walk-in, and return bubble.
- Copied original outing event and collectable resources into the Windows project.
- Added outing catalog loading, collectable inventory persistence, and reward generation matching the original rarity probability curve.
- Restored default outing duration in settings.
- Added outing event and collectable counts to usage statistics and backup data.
- Added pending/deferred reminder behavior so reminders wait for long-duration cat states.
- Added status and remaining-time rows to tray and cat context menus.
- Added Windows suspend/resume handling to resolve active outings after wake.
- Generated the tray icon from the active cat image instead of using the generic application icon.
- Added a simple collected-items list in settings.
- Added a Windows tray icon with menu actions for petting, walking/resting toggle, settings, show/hide, and exit.
- Added a speech bubble area above the cat with action buttons.
- Added water and movement reminder scheduling with complete and 5-minute snooze actions.
- Expanded settings with reminder enablement, asset pack refresh, asset folder open, and asset status text.
- Added asset pack directory preparation under `%APPDATA%\DockCatWin\AssetPacks`.
- Added default pack seeding, `my-cat` template generation, manifest error tolerance, and fallback to default cat resources when custom packs are incomplete.
- Hid outing-specific settings from the UI for now.
- Clamped dragged cat anchors so the cat cannot end below the bottom taskbar/work-area boundary.
- Added display selection in settings and reclamping when Windows display settings change.
- Added usage statistics for companion time and completed reminders.
- Added local user data backup under `%APPDATA%\DockCatWin\DataBackup`.
- Added a PowerShell publish script for Windows release builds.
- Verified the publish script creates `artifacts\DockCatWin`.
- Updated README with the new Windows behavior and asset pack location.

## Risk Analysis

- Build verification passes with .NET SDK 8.0.420.
- The reminder feature is timer-based and not yet backed by usage statistics.
- The tray icon uses the default system application icon until a proper `.ico` asset is added.
- Asset validation is intentionally lightweight; it reports broad availability rather than per-file diagnostics.
- Bottom taskbar clamping now keeps the cat's lower edge at the work-area bottom; non-bottom taskbar behavior still needs real desktop layout testing.
- Release prep creates a framework-dependent win-x64 publish folder, not a full installer yet.
- Outing left/right taskbar behavior exits to the right side like the original implementation; further polish can make edge-specific exit paths later.
- Windows implementation now covers the practical original feature set, but pixel-perfect menu/status presentation and platform-specific Dock icon behavior differ by design.

## Suggested Verification Steps

1. Run `dotnet build .\DockCatWin\DockCatWin.csproj`.
2. Run `dotnet run --project .\DockCatWin\DockCatWin.csproj`.
3. Confirm the tray menu can show/hide, open settings, toggle walking/resting, and exit.
4. Temporarily lower reminder intervals in settings and confirm reminder bubbles appear with complete/snooze actions.
5. Open the asset folder from settings and confirm `default-lizz` and `my-cat` are created.
6. Change the selected display in settings, then confirm the cat repositions and stays clamped to the target work area.
7. Run `.\scripts\publish-win.ps1` and confirm `artifacts\DockCatWin` is created.
8. Start an outing with a short duration, confirm the cat walks out, returns, and shows either an event or collectable reward.
9. Recall during outing and confirm the cat returns with an event-style reward.
