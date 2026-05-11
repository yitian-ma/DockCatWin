using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using DockCatWin.Platform;

namespace DockCatWin;

public partial class MainWindow : Window
{
    private readonly DispatcherTimer animationTimer = new() { Interval = TimeSpan.FromMilliseconds(140) };
    private readonly DispatcherTimer movementTimer = new() { Interval = TimeSpan.FromMilliseconds(33) };
    private readonly DispatcherTimer stateTimer = new() { Interval = TimeSpan.FromSeconds(8) };
    private readonly List<BitmapImage> walkFrames = [];
    private readonly List<BitmapImage> restingFrames = [];
    private int frameIndex;
    private int direction = 1;
    private bool isWalking = true;
    private TaskbarActivityArea activityArea;

    public MainWindow()
    {
        InitializeComponent();

        Loaded += OnLoaded;
        animationTimer.Tick += (_, _) => AdvanceAnimation();
        movementTimer.Tick += (_, _) => AdvancePosition();
        stateTimer.Tick += (_, _) => ToggleState();
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        var dpi = VisualTreeHelper.GetDpi(this);
        activityArea = TaskbarGeometry.Current(dpi.DpiScaleX, dpi.DpiScaleY);

        LoadImages();
        CatImage.Source = walkFrames.FirstOrDefault() ?? restingFrames.FirstOrDefault();
        PositionAtTaskbar();

        animationTimer.Start();
        movementTimer.Start();
        stateTimer.Start();
    }

    private void LoadImages()
    {
        var root = Path.Combine(AppContext.BaseDirectory, "Resources", "DefaultCat");
        AddImages(Path.Combine(root, "animations", "walk"), walkFrames);
        AddImages(Path.Combine(root, "poses", "resting"), restingFrames);
    }

    private static void AddImages(string directory, List<BitmapImage> target)
    {
        if (!Directory.Exists(directory))
        {
            return;
        }

        foreach (var file in Directory.EnumerateFiles(directory, "*.png").OrderBy(Path.GetFileName))
        {
            var image = new BitmapImage();
            image.BeginInit();
            image.CacheOption = BitmapCacheOption.OnLoad;
            image.UriSource = new Uri(file, UriKind.Absolute);
            image.EndInit();
            image.Freeze();
            target.Add(image);
        }
    }

    private void PositionAtTaskbar()
    {
        Left = activityArea.MinX + (activityArea.MaxX - activityArea.MinX - Width) * 0.75;
        Top = activityArea.BaselineY - Height + 6;
    }

    private void AdvanceAnimation()
    {
        var frames = isWalking ? walkFrames : restingFrames;
        if (frames.Count == 0)
        {
            return;
        }

        frameIndex = (frameIndex + 1) % frames.Count;
        CatImage.Source = frames[frameIndex];
    }

    private void AdvancePosition()
    {
        if (!isWalking)
        {
            return;
        }

        Left += direction * 1.6;
        if (Left <= activityArea.MinX)
        {
            Left = activityArea.MinX;
            direction = 1;
        }
        else if (Left + Width >= activityArea.MaxX)
        {
            Left = activityArea.MaxX - Width;
            direction = -1;
        }

        MirrorTransform.ScaleX = direction < 0 ? -1 : 1;
    }

    private void ToggleState()
    {
        isWalking = !isWalking;
        frameIndex = 0;

        if (!isWalking && restingFrames.Count > 0)
        {
            CatImage.Source = restingFrames[Random.Shared.Next(restingFrames.Count)];
        }
    }

    private void CatImage_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount == 2)
        {
            ToggleState();
            return;
        }

        DragMove();
    }

    private void CatImage_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
    {
        var menu = new ContextMenu();
        var toggle = new MenuItem { Header = isWalking ? "休息一下" : "散步" };
        toggle.Click += (_, _) => ToggleState();

        var exit = new MenuItem { Header = "退出 DockCatWin" };
        exit.Click += (_, _) => Close();

        menu.Items.Add(toggle);
        menu.Items.Add(new Separator());
        menu.Items.Add(exit);
        menu.IsOpen = true;
    }
}
