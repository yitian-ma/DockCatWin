using System.IO;
using System.Text.Json;
using DockCatWin.Core.Settings;
using DockCatWin.Core.Statistics;

namespace DockCatWin.Core.Backup;

public sealed class UserDataBackupStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    private readonly string backupFilePath;

    public UserDataBackupStore()
    {
        var root = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "DockCatWin",
            "DataBackup");
        backupFilePath = Path.Combine(root, "user-data-backup.json");
    }

    public void Save(AppSettings settings, UsageStatistics statistics)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(backupFilePath)!);
        var snapshot = new
        {
            schemaVersion = 1,
            generatedAt = DateTime.UtcNow,
            app = "DockCatWin",
            settings,
            usageStatistics = statistics
        };
        File.WriteAllText(backupFilePath, JsonSerializer.Serialize(snapshot, JsonOptions));
    }
}
