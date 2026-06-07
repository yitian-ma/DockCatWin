using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace DockCatWin.Core.Outing;

public sealed class OutingEvent
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = "";

    [JsonPropertyName("event_type")]
    public string EventType { get; set; } = "";

    [JsonPropertyName("chinese_description")]
    public string ChineseDescription { get; set; } = "";

    [JsonPropertyName("english_description")]
    public string EnglishDescription { get; set; } = "";

    [JsonPropertyName("author")]
    public string Author { get; set; } = "";
}

[JsonConverter(typeof(OutingCollectableJsonConverter))]
public sealed class OutingCollectable
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = "";

    [JsonPropertyName("chinese_name")]
    public string ChineseName { get; set; } = "";

    [JsonPropertyName("english_name")]
    public string EnglishName { get; set; } = "";

    [JsonPropertyName("rarity")]
    public int Rarity { get; set; }

    public bool IsSpecialDisplayRarity { get; set; }

    public string RarityLabel => IsSpecialDisplayRarity ? "X" : Rarity.ToString();

    public int RaritySortRank => IsSpecialDisplayRarity ? 6 : Rarity;

    public bool IsStandardRarity => Rarity is >= 1 and <= 5;

    public bool IsRewardEligible => IsStandardRarity && !IsRetired;

    [JsonPropertyName("author")]
    public string Author { get; set; } = "";

    [JsonPropertyName("image_path")]
    public string ImagePath { get; set; } = "";

    [JsonPropertyName("is_retired")]
    public bool IsRetired { get; set; }
}

public sealed class OutingCollectableJsonConverter : JsonConverter<OutingCollectable>
{
    public override OutingCollectable Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartObject)
        {
            throw new JsonException("Collectable must be a JSON object.");
        }

        var collectable = new OutingCollectable();
        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
            {
                return collectable;
            }

            if (reader.TokenType != JsonTokenType.PropertyName)
            {
                throw new JsonException("Expected collectable property name.");
            }

            var propertyName = reader.GetString();
            reader.Read();
            switch (propertyName)
            {
                case "id":
                    collectable.Id = reader.GetString() ?? "";
                    break;
                case "chinese_name":
                    collectable.ChineseName = reader.GetString() ?? "";
                    break;
                case "english_name":
                    collectable.EnglishName = reader.GetString() ?? "";
                    break;
                case "rarity":
                    ReadRarity(ref reader, collectable);
                    break;
                case "author":
                    collectable.Author = reader.GetString() ?? "";
                    break;
                case "image_path":
                    collectable.ImagePath = reader.GetString() ?? "";
                    break;
                case "isRetired":
                case "is_retired":
                    collectable.IsRetired = reader.TokenType == JsonTokenType.True
                        || (reader.TokenType == JsonTokenType.String
                            && bool.TryParse(reader.GetString(), out var parsed)
                            && parsed);
                    break;
                default:
                    reader.Skip();
                    break;
            }
        }

        throw new JsonException("Collectable object was not closed.");
    }

    public override void Write(
        Utf8JsonWriter writer,
        OutingCollectable value,
        JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WriteString("id", value.Id);
        writer.WriteString("chinese_name", value.ChineseName);
        writer.WriteString("english_name", value.EnglishName);
        if (value.IsSpecialDisplayRarity)
        {
            writer.WriteString("rarity", "X");
        }
        else
        {
            writer.WriteNumber("rarity", value.Rarity);
        }
        writer.WriteString("author", value.Author);
        writer.WriteString("image_path", value.ImagePath);
        if (value.IsRetired)
        {
            writer.WriteBoolean("isRetired", value.IsRetired);
        }
        writer.WriteEndObject();
    }

    private static void ReadRarity(ref Utf8JsonReader reader, OutingCollectable collectable)
    {
        if (reader.TokenType == JsonTokenType.Number && reader.TryGetInt32(out var rarity))
        {
            collectable.Rarity = rarity;
            collectable.IsSpecialDisplayRarity = false;
            return;
        }

        if (reader.TokenType == JsonTokenType.String
            && string.Equals(reader.GetString(), "X", StringComparison.OrdinalIgnoreCase))
        {
            collectable.Rarity = 0;
            collectable.IsSpecialDisplayRarity = true;
            return;
        }

        throw new JsonException("Collectable rarity must be 1...5 or X.");
    }
}

public sealed class OutingCatalog
{
    public OutingCatalog(
        IReadOnlyList<OutingCollectable> collectables,
        IReadOnlyList<OutingEvent> events,
        Func<OutingCollectable, Stream?> openImageStream)
    {
        Collectables = collectables;
        Events = events;
        this.openImageStream = openImageStream;
    }

    private readonly Func<OutingCollectable, Stream?> openImageStream;

    public IReadOnlyList<OutingCollectable> Collectables { get; }
    public IReadOnlyList<OutingEvent> Events { get; }

    public Stream? OpenImageStreamFor(OutingCollectable collectable)
    {
        return openImageStream(collectable);
    }
}

public abstract record OutingReward
{
    public sealed record Event(OutingEvent Value) : OutingReward;
    public sealed record Collectable(OutingCollectable Value) : OutingReward;
}

public sealed class CollectableInventoryEntry
{
    public string CollectableID { get; set; } = "";
    public int Count { get; set; }
    public DateTime FirstAcquiredAt { get; set; }
    public DateTime LastAcquiredAt { get; set; }
}

public sealed class CollectableInventory
{
    public Dictionary<string, CollectableInventoryEntry> Entries { get; set; } = [];
    public string? RecentNewCollectableID { get; set; }

    public bool RecordCollectable(string collectableID, DateTime? at = null)
    {
        var now = at ?? DateTime.UtcNow;
        if (Entries.TryGetValue(collectableID, out var entry))
        {
            entry.Count++;
            entry.LastAcquiredAt = now;
            RecentNewCollectableID = null;
            return false;
        }

        Entries[collectableID] = new CollectableInventoryEntry
        {
            CollectableID = collectableID,
            Count = 1,
            FirstAcquiredAt = now,
            LastAcquiredAt = now
        };
        RecentNewCollectableID = collectableID;
        return true;
    }

    public void ClearRecentNewMarker()
    {
        RecentNewCollectableID = null;
    }
}
