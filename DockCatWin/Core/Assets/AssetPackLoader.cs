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
        PrepareCustomPacksDirectory();
        var root = Path.Combine(AppContext.BaseDirectory, "Resources", "DefaultCat");
        return TryLoadPack(root, null);
    }

    public CatAssetPack LoadSelectedPack(string selectedID)
    {
        PrepareCustomPacksDirectory();
        var fallback = LoadDefaultPackWithoutPreparing();
        if (!string.IsNullOrWhiteSpace(selectedID) && selectedID != "default-lizz")
        {
            var customRoot = Path.Combine(CustomPacksRoot(), selectedID);
            if (Directory.Exists(customRoot))
            {
                return MergeWithFallback(TryLoadPack(customRoot, fallback), fallback);
            }
        }

        return fallback;
    }

    public IReadOnlyList<string> CustomPackIDs()
    {
        PrepareCustomPacksDirectory();
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

    public void PrepareCustomPacksDirectory()
    {
        var root = CustomPacksRoot();
        Directory.CreateDirectory(root);
        CopyDefaultPackIfNeeded(root);
        CreateTemplatePackIfNeeded(root);
    }

    public string ValidationSummary(string selectedID)
    {
        try
        {
            var pack = LoadSelectedPack(selectedID);
            var issues = new List<string>();
            if (pack.LoadError is not null)
            {
                issues.Add(pack.LoadError);
            }

            if (pack.WalkFrames.Count == 0) issues.Add("缺少散步动画帧");
            if (pack.RestingPoses.Count == 0) issues.Add("缺少休息姿态");
            if (pack.HeldPoses.Count == 0) issues.Add("缺少抱起姿态");
            if (pack.DialoguePoses.Count == 0) issues.Add("缺少对话姿态");
            if (pack.TransitionPoses.Count == 0) issues.Add("缺少过渡姿态");

            return issues.Count == 0
                ? $"资源包可用：{pack.Manifest.Name} ({pack.Manifest.Id})"
                : $"资源包可加载，缺失项会使用默认猫：{string.Join("、", issues)}";
        }
        catch (Exception ex)
        {
            return $"资源包不可用：{ex.Message}";
        }
    }

    private CatAssetPack LoadDefaultPackWithoutPreparing()
    {
        var root = Path.Combine(AppContext.BaseDirectory, "Resources", "DefaultCat");
        return TryLoadPack(root, null);
    }

    private static CatAssetPack TryLoadPack(string root, CatAssetPack? fallback)
    {
        string? loadError = null;
        AssetManifest manifest;
        var manifestPath = Path.Combine(root, "manifest.json");
        try
        {
            manifest = File.Exists(manifestPath)
                ? JsonSerializer.Deserialize<AssetManifest>(File.ReadAllText(manifestPath), JsonOptions) ?? new AssetManifest()
                : new AssetManifest();
        }
        catch (Exception ex)
        {
            manifest = new AssetManifest();
            loadError = $"manifest.json 无法解析：{ex.Message}";
        }

        IReadOnlyList<BitmapImage> SafeLoad(Func<IReadOnlyList<BitmapImage>> load)
        {
            try
            {
                return load();
            }
            catch
            {
                return [];
            }
        }

        var pack = new CatAssetPack(
            manifest,
            SafeLoad(() => LoadWalkFrames(root, manifest)),
            SafeLoad(() => LoadImages(Path.Combine(root, NormalizePath(manifest.Poses.Resting)))),
            SafeLoad(() => LoadImages(Path.Combine(root, NormalizePath(manifest.Poses.Transition)))),
            SafeLoad(() => LoadImages(Path.Combine(root, NormalizePath(manifest.Poses.Held)))),
            SafeLoad(() => LoadImages(Path.Combine(root, NormalizePath(manifest.Poses.Dialogue)))),
            root,
            loadError);

        return fallback is null ? pack : MergeWithFallback(pack, fallback);
    }

    private static CatAssetPack MergeWithFallback(CatAssetPack pack, CatAssetPack fallback)
    {
        return new CatAssetPack(
            pack.Manifest,
            pack.WalkFrames.Count > 0 ? pack.WalkFrames : fallback.WalkFrames,
            pack.RestingPoses.Count > 0 ? pack.RestingPoses : fallback.RestingPoses,
            pack.TransitionPoses.Count > 0 ? pack.TransitionPoses : fallback.TransitionPoses,
            pack.HeldPoses.Count > 0 ? pack.HeldPoses : fallback.HeldPoses,
            pack.DialoguePoses.Count > 0 ? pack.DialoguePoses : fallback.DialoguePoses,
            pack.RootPath,
            pack.LoadError);
    }

    private static void CopyDefaultPackIfNeeded(string customRoot)
    {
        var source = Path.Combine(AppContext.BaseDirectory, "Resources", "DefaultCat");
        var destination = Path.Combine(customRoot, "default-lizz");
        if (!Directory.Exists(source) || Directory.Exists(destination))
        {
            return;
        }

        CopyDirectory(source, destination);
    }

    private static void CreateTemplatePackIfNeeded(string customRoot)
    {
        var root = Path.Combine(customRoot, "my-cat");
        if (Directory.Exists(root))
        {
            return;
        }

        Directory.CreateDirectory(Path.Combine(root, "poses", "resting"));
        Directory.CreateDirectory(Path.Combine(root, "poses", "held"));
        Directory.CreateDirectory(Path.Combine(root, "poses", "dialogue"));
        Directory.CreateDirectory(Path.Combine(root, "poses", "transition"));
        Directory.CreateDirectory(Path.Combine(root, "animations", "walk"));
        File.WriteAllText(Path.Combine(root, "manifest.json"), """
        {
          "id": "my-cat",
          "name": "My Cat",
          "author": "Your Name",
          "canvas_width": 512,
          "canvas_height": 512,
          "default_anchor": { "x": 0.5, "y": 0.88 },
          "poses": {
            "resting": "poses/resting",
            "held": "poses/held",
            "dialogue": "poses/dialogue",
            "transition": "poses/transition"
          },
          "animations": {
            "walk": { "fps": 3, "frames": [] }
          }
        }
        """);
    }

    private static void CopyDirectory(string source, string destination)
    {
        Directory.CreateDirectory(destination);
        foreach (var directory in Directory.EnumerateDirectories(source, "*", SearchOption.AllDirectories))
        {
            Directory.CreateDirectory(directory.Replace(source, destination));
        }

        foreach (var file in Directory.EnumerateFiles(source, "*", SearchOption.AllDirectories))
        {
            File.Copy(file, file.Replace(source, destination), overwrite: false);
        }
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

        return new CatAssetPack(manifest, walkFrames, resting, transition, held, dialogue, root);
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
