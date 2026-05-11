using System.Windows.Media.Imaging;

namespace DockCatWin.Core.Assets;

public sealed class CatAssetPack
{
    public CatAssetPack(
        AssetManifest manifest,
        IReadOnlyList<BitmapImage> walkFrames,
        IReadOnlyList<BitmapImage> restingPoses,
        IReadOnlyList<BitmapImage> transitionPoses,
        IReadOnlyList<BitmapImage> heldPoses,
        IReadOnlyList<BitmapImage> dialoguePoses,
        string rootPath,
        string? loadError = null)
    {
        Manifest = manifest;
        WalkFrames = walkFrames;
        RestingPoses = restingPoses;
        TransitionPoses = transitionPoses;
        HeldPoses = heldPoses;
        DialoguePoses = dialoguePoses;
        RootPath = rootPath;
        LoadError = loadError;
    }

    public AssetManifest Manifest { get; }
    public IReadOnlyList<BitmapImage> WalkFrames { get; }
    public IReadOnlyList<BitmapImage> RestingPoses { get; }
    public IReadOnlyList<BitmapImage> TransitionPoses { get; }
    public IReadOnlyList<BitmapImage> HeldPoses { get; }
    public IReadOnlyList<BitmapImage> DialoguePoses { get; }
    public string RootPath { get; }
    public string? LoadError { get; }

    public double WalkFps => Math.Clamp(Manifest.Animations.Walk.Fps, 1, 24);
    public double SourceWidth => Manifest.CanvasWidth > 0 ? Manifest.CanvasWidth : 1254;
    public double SourceHeight => Manifest.CanvasHeight > 0 ? Manifest.CanvasHeight : 1254;
}
