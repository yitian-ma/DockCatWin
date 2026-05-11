using System.IO;
using System.Text.Json;
using System.Windows.Media.Imaging;

namespace DockCatWin.Core.Assets;

public sealed class AssetPackLoader
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public CatAssetPack LoadDefaultPack()
    {
        var root = Path.Combine(AppContext.BaseDirectory, "Resources", "DefaultCat");
        return LoadPack(root);
    }

    public CatAssetPack LoadSelectedPack(string selectedID)
    {
        if (!string.IsNullOrWhiteSpace(selectedID) && selectedID != "default-lizz")
        {
            var customRoot = Path.Combine(CustomPacksRoot(), selectedID);
            if (Directory.Exists(customRoot))
            {
                return LoadPack(customRoot);
            }
        }

        return LoadDefaultPack();
    }

    public IReadOnlyList<string> CustomPackIDs()
    {
        var root = CustomPacksRoot();
        if (!Directory.Exists(root))
        {
            return [];
        }

        return Directory.EnumerateDirectories(root)
            .Select(Path.GetFileName)
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Order(StringComparer.OrdinalIgnoreCase)
            .Cast<string>()
            .ToList();
    }

    public string CustomPacksRoot()
    {
        return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "DockCatWin",
            "AssetPacks");
    }

    private static CatAssetPack LoadPack(string root)
    {
        var manifestPath = Path.Combine(root, "manifest.json");
        var manifest = File.Exists(manifestPath)
            ? JsonSerializer.Deserialize<AssetManifest>(File.ReadAllText(manifestPath), JsonOptions) ?? new AssetManifest()
            : new AssetManifest();

        var walkFrames = LoadWalkFrames(root, manifest);
        var resting = LoadImages(Path.Combine(root, NormalizePath(manifest.Poses.Resting)));
        var transition = LoadImages(Path.Combine(root, NormalizePath(manifest.Poses.Transition)));
        var held = LoadImages(Path.Combine(root, NormalizePath(manifest.Poses.Held)));
        var dialogue = LoadImages(Path.Combine(root, NormalizePath(manifest.Poses.Dialogue)));

        return new CatAssetPack(manifest, walkFrames, resting, transition, held, dialogue);
    }

    private static IReadOnlyList<BitmapImage> LoadWalkFrames(string root, AssetManifest manifest)
    {
        if (manifest.Animations.Walk.Frames.Length > 0)
        {
            return manifest.Animations.Walk.Frames
                .Select(frame => Path.Combine(root, NormalizePath(frame)))
                .Where(File.Exists)
                .Select(LoadImage)
                .ToList();
        }

        return LoadImages(Path.Combine(root, "animations", "walk"));
    }

    private static IReadOnlyList<BitmapImage> LoadImages(string directory)
    {
        if (!Directory.Exists(directory))
        {
            return [];
        }

        return Directory.EnumerateFiles(directory, "*.png")
            .OrderBy(Path.GetFileName)
            .Select(LoadImage)
            .ToList();
    }

    private static BitmapImage LoadImage(string file)
    {
        var image = new BitmapImage();
        image.BeginInit();
        image.CacheOption = BitmapCacheOption.OnLoad;
        image.UriSource = new Uri(file, UriKind.Absolute);
        image.EndInit();
        image.Freeze();
        return image;
    }

    private static string NormalizePath(string path)
    {
        return path.Replace('/', Path.DirectorySeparatorChar);
    }
}
