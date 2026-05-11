# Task

- Task description: Create a new standalone Windows repository for DockCat and implement a minimal WPF runnable version.
- Mode: Feature
- Target files:
  - `DockCatWin/DockCatWin.csproj`
  - `DockCatWin/App.xaml`
  - `DockCatWin/App.xaml.cs`
  - `DockCatWin/MainWindow.xaml`
  - `DockCatWin/MainWindow.xaml.cs`
  - `DockCatWin/Platform/TaskbarGeometry.cs`
  - `README.md`
  - `.gitignore`
- Constraints:
  - Keep the existing macOS repository untouched.
  - Use .NET 8 WPF for the first Windows implementation.
  - Copy and reuse the existing default cat image resources.
  - Implement only the minimal desktop pet behavior first: transparent topmost window, taskbar positioning, walking/resting animation, drag, and right-click exit.
  - Local machine currently lacks the .NET SDK and GitHub CLI, so build/publish may be blocked until those are installed.
