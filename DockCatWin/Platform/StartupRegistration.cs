using System.Diagnostics;
using System.IO;
using Microsoft.Win32;

namespace DockCatWin.Platform;

public sealed class StartupRegistration
{
    private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string ValueName = "DockCatWin";

    public bool IsEnabled()
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: false);
        return string.Equals(key?.GetValue(ValueName) as string, StartupCommand(), StringComparison.OrdinalIgnoreCase);
    }

    public void SetEnabled(bool enabled)
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: true)
            ?? Registry.CurrentUser.CreateSubKey(RunKeyPath, writable: true);
        if (enabled)
        {
            key.SetValue(ValueName, StartupCommand());
            return;
        }

        key.DeleteValue(ValueName, throwOnMissingValue: false);
    }

    private static string StartupCommand()
    {
        var processPath = Environment.ProcessPath
            ?? Process.GetCurrentProcess().MainModule?.FileName;
        if (!string.IsNullOrWhiteSpace(processPath)
            && string.Equals(Path.GetExtension(processPath), ".exe", StringComparison.OrdinalIgnoreCase)
            && !string.Equals(Path.GetFileNameWithoutExtension(processPath), "dotnet", StringComparison.OrdinalIgnoreCase))
        {
            return Quote(processPath);
        }

        var assemblyPath = Path.Combine(AppContext.BaseDirectory, "DockCatWin.dll");
        if (File.Exists(assemblyPath))
        {
            return $"dotnet {Quote(assemblyPath)}";
        }

        throw new InvalidOperationException("无法确定 DockCatWin 的启动路径。");
    }

    private static string Quote(string path)
    {
        return $"\"{path.Replace("\"", "\\\"")}\"";
    }
}
