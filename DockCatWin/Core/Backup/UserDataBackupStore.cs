using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using DockCatWin.Core.Outing;
using DockCatWin.Core.Settings;
using DockCatWin.Core.Statistics;

namespace DockCatWin.Core.Backup;

public sealed class UserDataBackupStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    private readonly string backupFilePath;

    public UserDataBackupStore()
    {
        BackupDirectoryPath = Path.Combine(
            AppContext.BaseDirectory,
            "UserData",
            "DataBackup");
        backupFilePath = Path.Combine(BackupDirectoryPath, "user-data-backup.json");
    }

    public string BackupDirectoryPath { get; }

    public void Save(
        AppSettings settings,
        UsageStatistics statistics,
        CollectableInventory inventory,
        OutingCatalog? outingCatalog = null)
    {
        Directory.CreateDirectory(BackupDirectoryPath);
        var snapshot = new UserDataBackupSnapshot
        {
            SchemaVersion = 2,
            GeneratedAt = DateTime.UtcNow,
            App = "DockCatWin",
            Settings = settings.Clone(),
            UsageStatistics = statistics.Clone(),
            CollectableInventory = UserDataBackupCollectableInventory.FromInventory(inventory, outingCatalog)
        };
        File.WriteAllText(backupFilePath, JsonSerializer.Serialize(snapshot, JsonOptions));
    }

    public UserDataRestoreResult RestoreData(string path, OutingCatalog outingCatalog)
    {
        var snapshot = JsonSerializer.Deserialize<UserDataBackupSnapshot>(File.ReadAllText(path), JsonOptions)
            ?? throw new InvalidDataException("备份文件为空或格式无效。");
        if (snapshot.SchemaVersion is < 1 or > 2)
        {
            throw new InvalidDataException($"不支持的备份版本：{snapshot.SchemaVersion}");
        }
        if (!IsValid(snapshot.UsageStatistics))
        {
            throw new InvalidDataException("备份中的使用统计无效。");
        }

        var collectablesByID = outingCatalog.Collectables.ToDictionary(item => item.Id, item => item);
        var restoredEntries = new Dictionary<string, CollectableInventoryEntry>();
        var skippedNames = new List<string>();
        foreach (var (key, backupEntry) in snapshot.CollectableInventory.Entries)
        {
            var collectableID = string.IsNullOrWhiteSpace(backupEntry.CollectableID)
                ? key
                : backupEntry.CollectableID;
            if (string.IsNullOrWhiteSpace(collectableID))
            {
                throw new InvalidDataException($"备份中的收藏品条目无效：{key}");
            }
            if (!collectablesByID.ContainsKey(collectableID))
            {
                skippedNames.Add(backupEntry.DisplayName(collectableID));
                continue;
            }

            var count = backupEntry.Count ?? 1;
            if (count <= 0)
            {
                throw new InvalidDataException($"备份中的收藏品数量无效：{collectableID}");
            }
            var lastAcquiredAt = backupEntry.LastAcquiredAt == default
                ? DateTime.UtcNow
                : backupEntry.LastAcquiredAt;
            restoredEntries[collectableID] = new CollectableInventoryEntry
            {
                CollectableID = collectableID,
                Count = count,
                FirstAcquiredAt = backupEntry.FirstAcquiredAt ?? lastAcquiredAt,
                LastAcquiredAt = lastAcquiredAt
            };
        }

        var recentNewCollectableID = snapshot.CollectableInventory.RecentNewCollectableID;
        if (recentNewCollectableID is not null && !restoredEntries.ContainsKey(recentNewCollectableID))
        {
            recentNewCollectableID = null;
        }

        var settings = snapshot.Settings?.Clone() ?? AppSettings.Defaults;
        settings.ActiveOutingEndDate = null;
        settings.ActiveOutingDurationSeconds = null;
        settings.Normalize();

        return new UserDataRestoreResult(
            settings,
            snapshot.UsageStatistics.Clone(),
            new CollectableInventory
            {
                Entries = restoredEntries,
                RecentNewCollectableID = recentNewCollectableID
            },
            skippedNames.Order(StringComparer.OrdinalIgnoreCase).ToList());
    }

    private static bool IsValid(UsageStatistics statistics)
    {
        return statistics.TotalCompanionSeconds >= 0
            && statistics.CompletedWaterReminders >= 0
            && statistics.CompletedMovementReminders >= 0
            && statistics.OutingEvents >= 0
            && statistics.OutingCollectables >= 0;
    }
}

public sealed class UserDataBackupSnapshot
{
    [JsonPropertyName("schemaVersion")]
    public int SchemaVersion { get; set; }

    [JsonPropertyName("generatedAt")]
    public DateTime GeneratedAt { get; set; }

    [JsonPropertyName("app")]
    public string App { get; set; } = "DockCatWin";

    [JsonPropertyName("settings")]
    public AppSettings? Settings { get; set; }

    [JsonPropertyName("usageStatistics")]
    public UsageStatistics UsageStatistics { get; set; } = new();

    [JsonPropertyName("collectableInventory")]
    public UserDataBackupCollectableInventory CollectableInventory { get; set; } = new();
}

public sealed class UserDataBackupCollectableInventory
{
    public Dictionary<string, UserDataBackupCollectableInventoryEntry> Entries { get; set; } = [];
    public string? RecentNewCollectableID { get; set; }

    public static UserDataBackupCollectableInventory FromInventory(CollectableInventory inventory, OutingCatalog? outingCatalog = null)
    {
        var collectablesByID = outingCatalog?.Collectables.ToDictionary(item => item.Id, item => item)
            ?? [];
        return new UserDataBackupCollectableInventory
        {
            Entries = inventory.Entries.ToDictionary(
                pair => pair.Key,
                pair =>
                {
                    collectablesByID.TryGetValue(pair.Value.CollectableID, out var collectable);
                    return UserDataBackupCollectableInventoryEntry.FromEntry(pair.Value, collectable);
                }),
            RecentNewCollectableID = inventory.RecentNewCollectableID
        };
    }
}

public sealed class UserDataBackupCollectableInventoryEntry
{
    public string CollectableID { get; set; } = "";
    public int? Count { get; set; }
    public DateTime? FirstAcquiredAt { get; set; }
    public DateTime LastAcquiredAt { get; set; }
    public string? ChineseName { get; set; }
    public string? EnglishName { get; set; }

    public static UserDataBackupCollectableInventoryEntry FromEntry(
        CollectableInventoryEntry entry,
        OutingCollectable? collectable = null)
    {
        return new UserDataBackupCollectableInventoryEntry
        {
            CollectableID = entry.CollectableID,
            Count = entry.Count,
            FirstAcquiredAt = entry.FirstAcquiredAt,
            LastAcquiredAt = entry.LastAcquiredAt,
            ChineseName = collectable?.ChineseName,
            EnglishName = collectable?.EnglishName
        };
    }

    public string DisplayName(string fallbackID)
    {
        if (!string.IsNullOrWhiteSpace(ChineseName))
        {
            return ChineseName.Trim();
        }
        if (!string.IsNullOrWhiteSpace(EnglishName))
        {
            return EnglishName.Trim();
        }
        return fallbackID;
    }
}

public sealed record UserDataRestoreResult(
    AppSettings Settings,
    UsageStatistics UsageStatistics,
    CollectableInventory CollectableInventory,
    IReadOnlyList<string> SkippedCollectableNames);
