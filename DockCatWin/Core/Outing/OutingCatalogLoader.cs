using System.IO;
using System.Text.Json;

namespace DockCatWin.Core.Outing;

public sealed class OutingCatalogLoader
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public OutingCatalog LoadCatalog()
    {
        var root = Path.Combine(AppContext.BaseDirectory, "Resources", "Outing");
        var collectables = LoadJson<List<OutingCollectable>>(Path.Combine(root, "collectables.json")) ?? [];
        var events = LoadJson<List<OutingEvent>>(Path.Combine(root, "events.json")) ?? [];

        collectables = collectables
            .Where(item => !string.IsNullOrWhiteSpace(item.Id)
                && !string.IsNullOrWhiteSpace(item.ChineseName)
                && !string.IsNullOrWhiteSpace(item.ImagePath)
                && item.Rarity is >= 1 and <= 5
                && File.Exists(Path.Combine(root, item.ImagePath.Replace('/', Path.DirectorySeparatorChar))))
            .ToList();

        events = events
            .Where(item => !string.IsNullOrWhiteSpace(item.Id)
                && !string.IsNullOrWhiteSpace(item.ChineseDescription))
            .ToList();

        return new OutingCatalog(collectables, events, root);
    }

    private static T? LoadJson<T>(string path)
    {
        try
        {
            return File.Exists(path)
                ? JsonSerializer.Deserialize<T>(File.ReadAllText(path), JsonOptions)
                : default;
        }
        catch
        {
            return default;
        }
    }
}
