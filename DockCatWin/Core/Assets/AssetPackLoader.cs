using System.IO;
using System.Security.Cryptography;
using System.Text.Json;
using System.Windows.Media.Imaging;
using OpenCvSharp;

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
        CopyBundledPackIfNeeded("MyCat", "my-cat", root);
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

    private static void CopyBundledPackIfNeeded(string resourceFolderName, string destinationFolderName, string customRoot)
    {
        var source = Path.Combine(AppContext.BaseDirectory, "Resources", resourceFolderName);
        var destination = Path.Combine(customRoot, destinationFolderName);
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
          "display_sizes": {
            "held": { "width": 650, "height": 1236 }
          },
          "animations": {
            "walk": {
              "fps": 3,
              "video": "animations/walk/walk.mp4",
              "video_frame_count": 4,
              "frames": []
            }
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

        var directoryFrames = LoadImages(Path.Combine(root, "animations", "walk"));
        if (directoryFrames.Count > 0)
        {
            return directoryFrames;
        }

        if (!string.IsNullOrWhiteSpace(manifest.Animations.Walk.Video))
        {
            var videoPath = Path.Combine(root, NormalizePath(manifest.Animations.Walk.Video));
            if (File.Exists(videoPath))
            {
                var extracted = ExtractWalkVideoFrames(root, manifest, videoPath);
                if (extracted.Count > 0)
                {
                    return extracted;
                }
            }
        }

        return [];
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

    private static IReadOnlyList<BitmapImage> ExtractWalkVideoFrames(string root, AssetManifest manifest, string videoPath)
    {
        var frameCount = Math.Clamp(manifest.Animations.Walk.VideoFrameCount, 2, 24);
        var cacheDirectory = VideoCacheDirectory(root, manifest, videoPath, frameCount);
        Directory.CreateDirectory(cacheDirectory);

        var cachedFrames = Directory.EnumerateFiles(cacheDirectory, "walk_*.png")
            .OrderBy(Path.GetFileName)
            .ToList();
        if (cachedFrames.Count == frameCount)
        {
            return cachedFrames.Select(LoadImage).ToList();
        }

        foreach (var staleFrame in cachedFrames)
        {
            File.Delete(staleFrame);
        }

        using var capture = new VideoCapture(videoPath);
        if (!capture.IsOpened())
        {
            return [];
        }

        var totalFrames = (int)Math.Round(capture.Get(VideoCaptureProperties.FrameCount));
        if (totalFrames <= 0)
        {
            return [];
        }

        var outputFrames = new List<string>();
        for (var i = 0; i < frameCount; i++)
        {
            var position = FramePosition(totalFrames, frameCount, i);
            capture.Set(VideoCaptureProperties.PosFrames, position);

            using var frame = new Mat();
            if (!capture.Read(frame) || frame.Empty())
            {
                continue;
            }

            using var keyed = NormalizeWalkFrame(ApplyGreenScreenKey(frame));
            var outputPath = Path.Combine(cacheDirectory, $"walk_{i + 1:00}.png");
            Cv2.ImWrite(outputPath, keyed);
            outputFrames.Add(outputPath);
        }

        return outputFrames.Select(LoadImage).ToList();
    }

    private static int FramePosition(int totalFrames, int frameCount, int index)
    {
        if (frameCount == 1)
        {
            return totalFrames / 2;
        }

        var start = totalFrames * 0.12;
        var end = totalFrames * 0.88;
        var step = (end - start) / (frameCount - 1);
        return Math.Clamp((int)Math.Round(start + (step * index)), 0, totalFrames - 1);
    }

    private static string VideoCacheDirectory(string root, AssetManifest manifest, string videoPath, int frameCount)
    {
        var info = new FileInfo(videoPath);
        var identity = $"v2-normalized|{root}|{manifest.Id}|{videoPath}|{info.Length}|{info.LastWriteTimeUtc.Ticks}|{frameCount}|{manifest.CanvasWidth}|{manifest.CanvasHeight}";
        var hash = Convert.ToHexString(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(identity)))[..16];
        return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "DockCatWin",
            "VideoCache",
            SanitizePathSegment(manifest.Id),
            hash);
    }

    private static string SanitizePathSegment(string value)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var chars = value.Select(ch => invalid.Contains(ch) ? '_' : ch).ToArray();
        var sanitized = new string(chars).Trim();
        return string.IsNullOrWhiteSpace(sanitized) ? "asset-pack" : sanitized;
    }

    private static unsafe Mat ApplyGreenScreenKey(Mat source)
    {
        using var bgr = source.Channels() == 3 ? source.Clone() : source.CvtColor(ColorConversionCodes.BGRA2BGR);
        using var hsv = new Mat();
        Cv2.CvtColor(bgr, hsv, ColorConversionCodes.BGR2HSV);
        var output = new Mat();
        Cv2.CvtColor(bgr, output, ColorConversionCodes.BGR2BGRA);

        for (var y = 0; y < bgr.Rows; y++)
        {
            var bgrRow = (byte*)bgr.Ptr(y);
            var hsvRow = (byte*)hsv.Ptr(y);
            var outRow = (byte*)output.Ptr(y);

            for (var x = 0; x < bgr.Cols; x++)
            {
                var pixel = x * 3;
                var outPixel = x * 4;
                var blue = bgrRow[pixel];
                var green = bgrRow[pixel + 1];
                var red = bgrRow[pixel + 2];
                var hue = hsvRow[pixel];
                var saturation = hsvRow[pixel + 1];
                var value = hsvRow[pixel + 2];

                var greenDominance = green - Math.Max(red, blue);
                var greenHueDistance = Math.Abs(hue - 60);
                var isGreenHue = greenHueDistance <= 28;
                var isKeyCandidate = isGreenHue
                    && saturation >= 55
                    && value >= 70
                    && greenDominance >= 18
                    && green >= 95;

                if (!isKeyCandidate)
                {
                    continue;
                }

                var dominanceAlpha = 1.0 - Clamp01((greenDominance - 18) / 52.0);
                var hueAlpha = Clamp01((greenHueDistance - 8) / 20.0);
                var alpha = (byte)Math.Round(255 * Math.Max(dominanceAlpha, hueAlpha));

                outRow[outPixel + 1] = Math.Min(green, Math.Max(red, blue));
                outRow[outPixel + 3] = alpha;
            }
        }

        return output;
    }

    private static Mat NormalizeWalkFrame(Mat keyedFrame)
    {
        var bounds = AlphaBounds(keyedFrame);
        if (bounds.Width <= 0 || bounds.Height <= 0)
        {
            return keyedFrame.Clone();
        }

        var targetWidth = 1100;
        var targetHeight = 650;
        using var cropped = new Mat(keyedFrame, bounds);
        var horizontalPadding = 34;
        var topPadding = 18;
        var bottomPadding = 8;
        var availableWidth = targetWidth - (horizontalPadding * 2);
        var availableHeight = targetHeight - topPadding - bottomPadding;
        var scale = Math.Min(
            availableWidth / (double)bounds.Width,
            availableHeight / (double)bounds.Height);
        var drawWidth = Math.Max(1, (int)Math.Round(bounds.Width * scale));
        var drawHeight = Math.Max(1, (int)Math.Round(bounds.Height * scale));
        var offsetX = Math.Max(0, (targetWidth - drawWidth) / 2);
        var offsetY = Math.Max(topPadding, targetHeight - bottomPadding - drawHeight);

        using var resized = new Mat();
        Cv2.Resize(cropped, resized, new OpenCvSharp.Size(drawWidth, drawHeight), 0, 0, InterpolationFlags.Lanczos4);

        var normalized = new Mat(targetHeight, targetWidth, MatType.CV_8UC4, Scalar.All(0));
        var destination = new Rect(offsetX, offsetY, drawWidth, drawHeight);
        resized.CopyTo(new Mat(normalized, destination));
        return normalized;
    }

    private static unsafe Rect AlphaBounds(Mat image)
    {
        var minX = image.Cols;
        var minY = image.Rows;
        var maxX = -1;
        var maxY = -1;

        for (var y = 0; y < image.Rows; y++)
        {
            var row = (byte*)image.Ptr(y);
            for (var x = 0; x < image.Cols; x++)
            {
                var alpha = row[(x * 4) + 3];
                if (alpha <= 8)
                {
                    continue;
                }

                minX = Math.Min(minX, x);
                minY = Math.Min(minY, y);
                maxX = Math.Max(maxX, x);
                maxY = Math.Max(maxY, y);
            }
        }

        return maxX < minX || maxY < minY
            ? new Rect(0, 0, 0, 0)
            : new Rect(minX, minY, maxX - minX + 1, maxY - minY + 1);
    }

    private static double Clamp01(double value)
    {
        return Math.Max(0, Math.Min(1, value));
    }

    private static string NormalizePath(string path)
    {
        return path.Replace('/', Path.DirectorySeparatorChar);
    }
}
