using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using DockCatWin.Core.Assets;
using DockCatWin.Core.Settings;
using DockCatWin.Core.StateMachine;
using DockCatWin.Platform;
using DockCatWin.UI.CatWindow;
using WpfSize = System.Windows.Size;

namespace DockCatWin;

public partial class MainWindow : Window
{
    private readonly DispatcherTimer animationTimer = new();
    private readonly DispatcherTimer movementTimer = new() { Interval = TimeSpan.FromMilliseconds(33) };
    private readonly DispatcherTimer stateTimer = new();
    private readonly SettingsStore settingsStore = new();
    private readonly AssetPackLoader assetPackLoader = new();
    private readonly CatStateMachine stateMachine = new();
    private readonly Random random = new();

    private AppSettings settings = AppSettings.Defaults;
    private CatAssetPack assetPack = null!;
    private CatWindowController catWindow = null!;
    private TaskbarActivityArea activityArea;
    private IReadOnlyList<BitmapImage> activeFrames = [];
    private int frameIndex;
    private int direction = 1;

    public MainWindow()
    {
        InitializeComponent();

        Loaded += OnLoaded;
        animationTimer.Tick += (_, _) => AdvanceAnimation();
        movementTimer.Tick += (_, _) => AdvancePosition();
        stateTimer.Tick += (_, _) =>
        {
            stateTimer.Stop();
            if (stateTimer.Tag is CatState scheduledState)
            {
                stateMachine.FinishScheduledState(scheduledState);
            }
        };
        stateMachine.Transitioned += (_, newState) => ApplyState(newState);
        stateMachine.DurationScheduled += ScheduleStateTimer;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        settings = settingsStore.Load();
        assetPack = assetPackLoader.LoadSelectedPack(settings.SelectedAssetPackID);
        catWindow = new CatWindowController(
            this,
            CatImage,
            MirrorTransform,
            new WpfSize(assetPack.SourceWidth, assetPack.SourceHeight));

        ApplySettings(reposition: true);
        stateMachine.Start();
        movementTimer.Start();
    }

    private void ApplySettings(bool reposition)
    {
        settings.Normalize();
        catWindow.SetImageScale(settings.CatScalePercent);

        var dpi = VisualTreeHelper.GetDpi(this);
        activityArea = TaskbarGeometry.Current(dpi.DpiScaleX, dpi.DpiScaleY);
        stateMachine.UpdateDurations(
            TimeSpan.FromSeconds(settings.WalkDurationMinimumSeconds),
            TimeSpan.FromSeconds(settings.WalkDurationMaximumSeconds),
            TimeSpan.FromSeconds(settings.RestDurationMinimumSeconds),
            TimeSpan.FromSeconds(settings.RestDurationMaximumSeconds));

        if (reposition)
        {
            var anchor = activityArea.AnchorForPercent(settings.StartPositionPercent, catWindow.CatSize);
            catWindow.SetAnchor(anchor, activityArea.Edge);
        }
    }

    private void ApplyState(CatState state)
    {
        animationTimer.Stop();
        activeFrames = [];
        frameIndex = 0;

        switch (state.Kind)
        {
            case CatStateKind.Walking:
                activeFrames = assetPack.WalkFrames;
                animationTimer.Interval = TimeSpan.FromSeconds(1 / assetPack.WalkFps);
                SetFrame(0);
                animationTimer.Start();
                break;
            case CatStateKind.Resting:
                SetRandomImage(assetPack.RestingPoses);
                break;
            case CatStateKind.Transitioning:
                SetRandomImage(assetPack.TransitionPoses);
                break;
            case CatStateKind.Dragged:
                SetRandomImage(assetPack.HeldPoses);
                break;
        }
    }

    private void ScheduleStateTimer(CatState scheduledState, TimeSpan duration)
    {
        stateTimer.Stop();
        stateTimer.Interval = duration;
        stateTimer.Tag = scheduledState;
        stateTimer.Start();
    }

    private void AdvanceAnimation()
    {
        if (activeFrames.Count == 0)
        {
            return;
        }

        frameIndex = (frameIndex + 1) % activeFrames.Count;
        SetFrame(frameIndex);
    }

    private void AdvancePosition()
    {
        if (stateMachine.State.Kind != CatStateKind.Walking)
        {
            return;
        }

        var current = catWindow.CurrentAnchor(activityArea.Edge);
        var delta = direction * settings.WalkBaseSpeed * movementTimer.Interval.TotalSeconds;
        var moved = activityArea.MoveAnchor(current, delta, catWindow.CatSize);

        if (activityArea.IsAtStart(moved, catWindow.CatSize))
        {
            direction = 1;
        }
        else if (activityArea.IsAtEnd(moved, catWindow.CatSize))
        {
            direction = -1;
        }

        catWindow.SetAnchor(moved, activityArea.Edge);
        catWindow.SetMirrored(activityArea.UsesHorizontalMovement && direction < 0);
    }

    private void SetFrame(int index)
    {
        if (index >= 0 && index < activeFrames.Count)
        {
            catWindow.SetImage(activeFrames[index]);
        }
    }

    private void SetRandomImage(IReadOnlyList<BitmapImage> images)
    {
        if (images.Count > 0)
        {
            catWindow.SetImage(images[random.Next(images.Count)]);
            return;
        }

        catWindow.SetImage(assetPack.WalkFrames.FirstOrDefault());
    }

    private void CatImage_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount == 2)
        {
            stateMachine.Pet();
            e.Handled = true;
            return;
        }

        stateTimer.Stop();
        stateMachine.BeginDrag();
        DragMove();
        var anchor = catWindow.CurrentAnchor(activityArea.Edge);
        catWindow.SetAnchor(activityArea.ClampAnchor(anchor, catWindow.CatSize), activityArea.Edge);
        stateMachine.EndDrag();
        e.Handled = true;
    }

    private void CatImage_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
    {
        var menu = new ContextMenu();

        var pet = new MenuItem { Header = $"摸摸{settings.CatName}" };
        pet.Click += (_, _) => stateMachine.Pet();

        var toggle = new MenuItem { Header = stateMachine.State.Kind == CatStateKind.Walking ? "休息一下" : "散步" };
        toggle.Click += (_, _) => stateMachine.ToggleLongDurationState();

        var settingsItem = new MenuItem { Header = "设置..." };
        settingsItem.Click += (_, _) => ShowSettingsWindow();

        var exit = new MenuItem { Header = "退出 DockCatWin" };
        exit.Click += (_, _) => Close();

        menu.Items.Add(pet);
        menu.Items.Add(toggle);
        menu.Items.Add(new Separator());
        menu.Items.Add(settingsItem);
        menu.Items.Add(new Separator());
        menu.Items.Add(exit);
        menu.IsOpen = true;
        e.Handled = true;
    }

    private void ShowSettingsWindow()
    {
        var previousAssetPackID = settings.SelectedAssetPackID;
        var window = new SettingsWindow(settings, assetPackLoader.CustomPackIDs())
        {
            Owner = this
        };

        if (window.ShowDialog() != true)
        {
            return;
        }

        settings = window.Settings;
        settingsStore.Save(settings);
        if (settings.SelectedAssetPackID != previousAssetPackID)
        {
            assetPack = assetPackLoader.LoadSelectedPack(settings.SelectedAssetPackID);
            catWindow = new CatWindowController(
                this,
                CatImage,
                MirrorTransform,
                new WpfSize(assetPack.SourceWidth, assetPack.SourceHeight));
        }
        ApplySettings(reposition: true);
        ApplyState(stateMachine.State);
    }
}
