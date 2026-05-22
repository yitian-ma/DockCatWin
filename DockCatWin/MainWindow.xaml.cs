using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Microsoft.Win32;
using DockCatWin.Core.Assets;
using DockCatWin.Core.Backup;
using DockCatWin.Core.Outing;
using DockCatWin.Core.Reminder;
using DockCatWin.Core.Settings;
using DockCatWin.Core.StateMachine;
using DockCatWin.Core.Statistics;
using DockCatWin.Platform;
using DockCatWin.UI.CatWindow;
using DockCatWin.UI.Tray;

namespace DockCatWin;

public partial class MainWindow : Window
{
    private readonly DispatcherTimer animationTimer = new();
    private readonly DispatcherTimer movementTimer = new() { Interval = TimeSpan.FromMilliseconds(33) };
    private readonly DispatcherTimer stateTimer = new();
    private readonly DispatcherTimer reminderTimer = new() { Interval = TimeSpan.FromSeconds(10) };
    private readonly DispatcherTimer statisticsTimer = new() { Interval = TimeSpan.FromMinutes(1) };
    private readonly DispatcherTimer outingTimer = new();
    private readonly SettingsStore settingsStore = new();
    private readonly UsageStatisticsStore usageStatisticsStore = new();
    private readonly UserDataBackupStore userDataBackupStore = new();
    private readonly AssetPackLoader assetPackLoader = new();
    private readonly OutingCatalogLoader outingCatalogLoader = new();
    private readonly CollectableInventoryStore collectableInventoryStore = new();
    private readonly CatStateMachine stateMachine = new();
    private readonly Random random = new();

    private AppSettings settings = AppSettings.Defaults;
    private CatAssetPack assetPack = null!;
    private CatWindowController catWindow = null!;
    private ReminderScheduler reminderScheduler = null!;
    private TrayIconController trayIcon = null!;
    private UsageStatistics usageStatistics = new();
    private OutingCatalog outingCatalog = null!;
    private CollectableInventory collectableInventory = new();
    private TaskbarActivityArea activityArea;
    private IReadOnlyList<BitmapImage> activeFrames = [];
    private int frameIndex;
    private int direction = 1;
    private bool isExitRequested;
    private ReminderType? activeReminder;
    private TimeSpan? pendingOutingDuration;
    private OutingReward? pendingOutingReward;
    private bool forceEventReturn;

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
        reminderTimer.Tick += (_, _) => PollReminders();
        reminderTimer.Tick += (_, _) => UpdateTray();
        statisticsTimer.Tick += (_, _) => RecordCompanionMinute();
        outingTimer.Tick += (_, _) =>
        {
            outingTimer.Stop();
            ReturnFromOuting(drawReward: true);
        };
        Closing += MainWindow_Closing;
        Closed += (_, _) =>
        {
            SystemEvents.DisplaySettingsChanged -= SystemEvents_DisplaySettingsChanged;
            SystemEvents.PowerModeChanged -= SystemEvents_PowerModeChanged;
            SaveUserData();
            trayIcon?.Dispose();
        };
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        settings = settingsStore.Load();
        usageStatistics = usageStatisticsStore.Load();
        collectableInventory = collectableInventoryStore.Load();
        outingCatalog = outingCatalogLoader.LoadCatalog();
        assetPack = assetPackLoader.LoadSelectedPack(settings.SelectedAssetPackID);
        reminderScheduler = new ReminderScheduler(settings);
        trayIcon = new TrayIconController(assetPack.DialoguePoses.FirstOrDefault()?.UriSource.LocalPath);
        trayIcon.PetRequested += () => Dispatcher.Invoke(stateMachine.Pet);
        trayIcon.ToggleStateRequested += () => Dispatcher.Invoke(stateMachine.ToggleLongDurationState);
        trayIcon.OutingRequested += () => Dispatcher.Invoke(stateMachine.BeginOutingPrompt);
        trayIcon.RecallRequested += () => Dispatcher.Invoke(ShowRecallConfirmation);
        trayIcon.SettingsRequested += () => Dispatcher.Invoke(ShowSettingsWindow);
        trayIcon.RestoreDataRequested += () => Dispatcher.Invoke(BeginUserDataRestore);
        trayIcon.ToggleVisibilityRequested += () => Dispatcher.Invoke(ToggleVisibilityFromTray);
        trayIcon.ExitRequested += () => Dispatcher.Invoke(ExitApplication);
        catWindow = new CatWindowController(
            this,
            CatImage,
            MirrorTransform,
            assetPack.DefaultSourceSize);

        ApplySettings(reposition: true);
        SystemEvents.DisplaySettingsChanged += SystemEvents_DisplaySettingsChanged;
        SystemEvents.PowerModeChanged += SystemEvents_PowerModeChanged;
        if (!RestoreActiveOutingIfNeeded())
        {
            stateMachine.Start();
        }
        movementTimer.Start();
        reminderTimer.Start();
        statisticsTimer.Start();
        UpdateTray();
    }

    private void ApplySettings(bool reposition)
    {
        settings.Normalize();
        catWindow.SetImageScale(settings.CatScalePercent);

        var dpi = VisualTreeHelper.GetDpi(this);
        activityArea = TaskbarGeometry.Current(dpi.DpiScaleX, dpi.DpiScaleY, settings.ActivityDisplayID);
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
        UpdateTray();
    }

    private void SystemEvents_DisplaySettingsChanged(object? sender, EventArgs e)
    {
        Dispatcher.Invoke(() =>
        {
            var anchor = catWindow.CurrentAnchor(activityArea.Edge);
            ApplySettings(reposition: false);
            catWindow.SetAnchor(activityArea.ClampAnchor(anchor, catWindow.CatSize), activityArea.Edge);
        });
    }

    private void SystemEvents_PowerModeChanged(object sender, PowerModeChangedEventArgs e)
    {
        Dispatcher.Invoke(() =>
        {
            if (e.Mode == PowerModes.Suspend)
            {
                SaveUserData();
                return;
            }

            if (e.Mode != PowerModes.Resume)
            {
                return;
            }

            ResolveActiveOutingAfterWake();
            var anchor = catWindow.CurrentAnchor(activityArea.Edge);
            ApplySettings(reposition: false);
            catWindow.SetAnchor(activityArea.ClampAnchor(anchor, catWindow.CatSize), activityArea.Edge);
        });
    }

    private void ApplyState(CatState state)
    {
        animationTimer.Stop();
        activeFrames = [];
        frameIndex = 0;
        ApplyStateSourceSize(state.Kind);

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
            case CatStateKind.OutingAsking:
                SetRandomImage(assetPack.DialoguePoses);
                ShowOutingDurationBubble();
                break;
            case CatStateKind.OutingConfirmingDeparture:
                SetRandomImage(assetPack.DialoguePoses);
                ShowBubble(
                    OutingDepartureMessage(),
                    ("好的", StartConfirmedOuting));
                break;
            case CatStateKind.OutingLeaving:
                StartOutingWalkOut();
                break;
            case CatStateKind.OutingAway:
                HideBubble();
                Hide();
                break;
            case CatStateKind.OutingReturning:
                StartOutingWalkIn();
                break;
            case CatStateKind.OutingReturned:
                SetRandomImage(assetPack.DialoguePoses);
                ShowOutingReturnBubble();
                break;
        }
        UpdateTray();
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
        if (stateMachine.State.Kind == CatStateKind.OutingLeaving)
        {
            AdvanceOutingWalkOut();
            return;
        }

        if (stateMachine.State.Kind == CatStateKind.OutingReturning)
        {
            AdvanceOutingWalkIn();
            return;
        }

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

    private void AdvanceOutingWalkOut()
    {
        var current = catWindow.CurrentAnchor(activityArea.Edge);
        var delta = settings.WalkBaseSpeed * 1.5 * movementTimer.Interval.TotalSeconds;
        var next = new System.Windows.Point(current.X + delta, current.Y);
        if (next.X >= activityArea.Screen.Right + catWindow.CatSize.Width)
        {
            animationTimer.Stop();
            stateMachine.MarkAway();
            return;
        }

        catWindow.SetAnchor(next, activityArea.Edge);
        catWindow.SetMirrored(false);
    }

    private void AdvanceOutingWalkIn()
    {
        var current = catWindow.CurrentAnchor(activityArea.Edge);
        var target = activityArea.AnchorForPercent(settings.StartPositionPercent, catWindow.CatSize);
        var delta = settings.WalkBaseSpeed * 1.5 * movementTimer.Interval.TotalSeconds;
        var next = new System.Windows.Point(current.X - delta, target.Y);
        if (next.X <= target.X)
        {
            animationTimer.Stop();
            catWindow.SetAnchor(target, activityArea.Edge);
            catWindow.SetMirrored(true);
            stateMachine.FinishReturnWalk();
            return;
        }

        catWindow.SetAnchor(next, activityArea.Edge);
        catWindow.SetMirrored(true);
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

    private void ApplyStateSourceSize(CatStateKind stateKind)
    {
        if (catWindow is null)
        {
            return;
        }

        var anchor = catWindow.CurrentAnchor(activityArea.Edge);
        catWindow.SetSourceSize(stateKind == CatStateKind.Dragged
            ? assetPack.HeldSourceSize
            : assetPack.DefaultSourceSize);
        catWindow.SetAnchor(anchor, activityArea.Edge);
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
        menu.Items.Add(new MenuItem { Header = StatusText(), IsEnabled = false });
        if (RemainingText() is { Length: > 0 } remaining)
        {
            menu.Items.Add(new MenuItem { Header = remaining, IsEnabled = false });
        }
        menu.Items.Add(new Separator());

        if (stateMachine.State.Kind == CatStateKind.OutingAway)
        {
            var recall = new MenuItem { Header = $"召回{settings.CatName}" };
            recall.Click += (_, _) => ShowRecallConfirmation();
            menu.Items.Add(recall);
        }
        else if (!stateMachine.State.IsOuting)
        {
            var pet = new MenuItem { Header = $"摸摸{settings.CatName}" };
            pet.Click += (_, _) => stateMachine.Pet();

            var toggle = new MenuItem { Header = stateMachine.State.Kind == CatStateKind.Walking ? "休息一下" : "散步" };
            toggle.Click += (_, _) => stateMachine.ToggleLongDurationState();

            var outing = new MenuItem { Header = "出门玩吧" };
            outing.Click += (_, _) => stateMachine.BeginOutingPrompt();
            menu.Items.Add(pet);
            menu.Items.Add(toggle);
            menu.Items.Add(outing);
        }
        else
        {
            var disabled = new MenuItem { Header = "小猫正在准备出门", IsEnabled = false };
            menu.Items.Add(disabled);
        }

        var settingsItem = new MenuItem { Header = "设置..." };
        settingsItem.Click += (_, _) => ShowSettingsWindow();

        var restoreItem = new MenuItem { Header = "恢复备份..." };
        restoreItem.Click += (_, _) => BeginUserDataRestore();

        var visibility = new MenuItem { Header = IsVisible ? "隐藏小猫" : "显示小猫" };
        visibility.Click += (_, _) => ToggleVisibilityFromTray();

        var exit = new MenuItem { Header = "退出 DockCatWin" };
        exit.Click += (_, _) => ExitApplication();

        menu.Items.Add(new Separator());
        menu.Items.Add(settingsItem);
        menu.Items.Add(restoreItem);
        menu.Items.Add(visibility);
        menu.Items.Add(new Separator());
        menu.Items.Add(exit);
        menu.IsOpen = true;
        e.Handled = true;
    }

    private void ShowSettingsWindow()
    {
        var previousAssetPackID = settings.SelectedAssetPackID;
        var window = new SettingsWindow(
            settings,
            assetPackLoader.CustomPackIDs(),
            assetPackLoader.CustomPackIDs,
            assetPackLoader.ValidationSummary,
            assetPackLoader.CustomPacksRoot(),
            usageStatistics,
            outingCatalog,
            collectableInventory)
        {
            Owner = this
        };

        if (window.ShowDialog() != true)
        {
            return;
        }

        settings = window.Settings;
        settingsStore.Save(settings);
        SaveUserData();
        reminderScheduler.Reset(settings);
        if (settings.SelectedAssetPackID != previousAssetPackID)
        {
            assetPack = assetPackLoader.LoadSelectedPack(settings.SelectedAssetPackID);
            catWindow = new CatWindowController(
                this,
                CatImage,
                MirrorTransform,
                assetPack.DefaultSourceSize);
        }
        ApplySettings(reposition: true);
        ApplyState(stateMachine.State);
    }

    private void BeginUserDataRestore()
    {
        var confirm = System.Windows.MessageBox.Show(
            this,
            "恢复备份会覆盖当前设置、使用统计和收藏品记录。要继续吗？",
            "恢复备份",
            MessageBoxButton.OKCancel,
            MessageBoxImage.Warning);
        if (confirm != MessageBoxResult.OK)
        {
            return;
        }

        Directory.CreateDirectory(userDataBackupStore.BackupDirectoryPath);
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Title = "选择 DockCatWin 备份文件",
            Filter = "JSON 备份文件 (*.json)|*.json|所有文件 (*.*)|*.*",
            InitialDirectory = userDataBackupStore.BackupDirectoryPath
        };
        if (dialog.ShowDialog(this) != true)
        {
            return;
        }

        try
        {
            var result = userDataBackupStore.RestoreData(dialog.FileName, outingCatalog);
            ApplyUserDataRestore(result);
            var message = "备份已恢复。";
            if (result.SkippedCollectableNames.Count > 0)
            {
                message += "\n\n以下收藏品在当前版本中不存在，已跳过：\n"
                    + string.Join("\n", result.SkippedCollectableNames.Select(name => $"• {name}"));
            }
            System.Windows.MessageBox.Show(this, message, "恢复完成", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception error)
        {
            System.Windows.MessageBox.Show(
                this,
                $"无法恢复这个备份文件。\n\n{error.Message}",
                "恢复失败",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }
    }

    private void ApplyUserDataRestore(UserDataRestoreResult result)
    {
        var previousAssetPackID = settings.SelectedAssetPackID;
        settings = result.Settings;
        usageStatistics = result.UsageStatistics;
        collectableInventory = result.CollectableInventory;

        settingsStore.Save(settings);
        usageStatisticsStore.Save(usageStatistics);
        collectableInventoryStore.Save(collectableInventory);
        userDataBackupStore.Save(settings, usageStatistics, collectableInventory, outingCatalog);

        reminderScheduler.Reset(settings);
        outingTimer.Stop();
        pendingOutingDuration = null;
        pendingOutingReward = null;
        activeReminder = null;
        forceEventReturn = false;
        HideBubble();

        if (settings.SelectedAssetPackID != previousAssetPackID)
        {
            assetPack = assetPackLoader.LoadSelectedPack(settings.SelectedAssetPackID);
            catWindow = new CatWindowController(
                this,
                CatImage,
                MirrorTransform,
                assetPack.DefaultSourceSize);
        }

        Show();
        ApplySettings(reposition: true);
        stateMachine.Start();
        UpdateTray();
    }

    private void ShowOutingDurationBubble()
    {
        BubbleInputBox.Text = Math.Max(1, (int)(settings.DefaultOutingDurationSeconds / 60)).ToString();
        ShowBubble(
            $"要让{settings.CatName}出门多久呢？",
            image: null,
            showInput: true,
            ("出门", ConfirmOutingFromBubble),
            ("取消", CancelOutingPrompt));
    }

    private void ConfirmOutingFromBubble()
    {
        var minutes = int.TryParse(BubbleInputBox.Text, out var parsed)
            ? Math.Max(1, parsed)
            : Math.Max(1, (int)(settings.DefaultOutingDurationSeconds / 60));
        pendingOutingDuration = TimeSpan.FromMinutes(minutes);
        stateMachine.ConfirmOuting();
    }

    private void CancelOutingPrompt()
    {
        HideBubble();
        stateMachine.CancelOutingPrompt();
    }

    private void StartConfirmedOuting()
    {
        var duration = pendingOutingDuration ?? TimeSpan.FromSeconds(settings.DefaultOutingDurationSeconds);
        settings.ActiveOutingEndDate = DateTime.UtcNow.Add(duration);
        settings.ActiveOutingDurationSeconds = duration.TotalSeconds;
        settingsStore.Save(settings);
        SaveUserData();
        outingTimer.Stop();
        outingTimer.Interval = duration <= TimeSpan.Zero ? TimeSpan.FromMilliseconds(100) : duration;
        outingTimer.Start();
        reminderScheduler.Clear();
        pendingOutingDuration = null;
        activeReminder = null;
        HideBubble();
        stateMachine.DepartOuting();
    }

    private bool RestoreActiveOutingIfNeeded()
    {
        if (settings.ActiveOutingEndDate is null)
        {
            return false;
        }

        var duration = TimeSpan.FromSeconds(settings.ActiveOutingDurationSeconds ?? settings.DefaultOutingDurationSeconds);
        var remaining = settings.ActiveOutingEndDate.Value - DateTime.UtcNow;
        stateMachine.RestoreOutingAway();
        if (remaining <= TimeSpan.Zero)
        {
            ReturnFromOuting(drawReward: true, plannedDuration: duration);
        }
        else
        {
            outingTimer.Interval = remaining;
            outingTimer.Start();
        }

        return true;
    }

    private void ResolveActiveOutingAfterWake()
    {
        if (!stateMachine.State.IsOuting)
        {
            return;
        }

        if (settings.ActiveOutingEndDate is null)
        {
            return;
        }

        var duration = TimeSpan.FromSeconds(settings.ActiveOutingDurationSeconds ?? settings.DefaultOutingDurationSeconds);
        var remaining = settings.ActiveOutingEndDate.Value - DateTime.UtcNow;
        if (remaining <= TimeSpan.Zero)
        {
            ReturnFromOuting(drawReward: true, plannedDuration: duration);
            return;
        }

        outingTimer.Stop();
        outingTimer.Interval = remaining;
        outingTimer.Start();
    }

    private void ReturnFromOuting(bool drawReward, TimeSpan? plannedDuration = null)
    {
        outingTimer.Stop();
        if (forceEventReturn)
        {
            pendingOutingReward = new OutingRewardGenerator(outingCatalog).EventReward();
        }
        else if (drawReward)
        {
            pendingOutingReward = new OutingRewardGenerator(outingCatalog).RewardForDuration(
                plannedDuration ?? TimeSpan.FromSeconds(settings.ActiveOutingDurationSeconds ?? settings.DefaultOutingDurationSeconds));
        }
        else
        {
            pendingOutingReward = null;
        }

        forceEventReturn = false;
        settings.ActiveOutingEndDate = null;
        settings.ActiveOutingDurationSeconds = null;
        settingsStore.Save(settings);
        SaveUserData();
        Show();
        stateMachine.ReturnFromOuting();
    }

    private void StartOutingWalkOut()
    {
        HideBubble();
        activeFrames = assetPack.WalkFrames;
        animationTimer.Interval = TimeSpan.FromSeconds(1 / assetPack.WalkFps);
        SetFrame(0);
        animationTimer.Start();
        direction = 1;
    }

    private void StartOutingWalkIn()
    {
        activeFrames = assetPack.WalkFrames;
        animationTimer.Interval = TimeSpan.FromSeconds(1 / assetPack.WalkFps);
        SetFrame(0);
        animationTimer.Start();
        direction = -1;
        var start = new System.Windows.Point(activityArea.Screen.Right + catWindow.CatSize.Width, activityArea.AnchorForPercent(settings.StartPositionPercent, catWindow.CatSize).Y);
        catWindow.SetAnchor(start, activityArea.Edge);
    }

    private void PollReminders()
    {
        if (activeReminder is not null || stateMachine.State.Kind == CatStateKind.Dragged)
        {
            return;
        }

        var due = reminderScheduler.DueReminder(settings, stateMachine.State.IsLongDuration);
        if (due is null)
        {
            return;
        }

        activeReminder = due;
        ShowBubble(
            due.Value.Message(settings),
            ("完成啦", () => CompleteReminder(due.Value)),
            ("稍等5分钟", () => SnoozeReminder(due.Value)));
    }

    private void CompleteReminder(ReminderType reminder)
    {
        reminderScheduler.Complete(reminder, settings);
        if (reminder == ReminderType.Water)
        {
            usageStatistics.CompletedWaterReminders++;
        }
        else if (reminder == ReminderType.Movement)
        {
            usageStatistics.CompletedMovementReminders++;
        }
        SaveUserData();
        activeReminder = null;
        HideBubble();
    }

    private void SnoozeReminder(ReminderType reminder)
    {
        reminderScheduler.Snooze(reminder, TimeSpan.FromMinutes(5));
        activeReminder = null;
        HideBubble();
    }

    private void ShowBubble(string message, params (string Title, Action Action)[] actions)
    {
        ShowBubble(message, image: null, showInput: false, actions);
    }

    private void ShowBubble(
        string message,
        ImageSource? image,
        bool showInput,
        params (string Title, Action Action)[] actions)
    {
        var anchor = catWindow.CurrentAnchor(activityArea.Edge);
        BubbleText.Text = message;
        BubbleImage.Source = image;
        BubbleImage.Visibility = image is null ? Visibility.Collapsed : Visibility.Visible;
        BubbleInputPanel.Visibility = showInput ? Visibility.Visible : Visibility.Collapsed;
        BubbleButtons.Children.Clear();
        foreach (var action in actions)
        {
            var button = new System.Windows.Controls.Button
            {
                Content = action.Title,
                MinWidth = 76,
                Margin = new Thickness(4, 0, 4, 0)
            };
            button.Click += (_, _) => action.Action();
            BubbleButtons.Children.Add(button);
        }

        BubbleBorder.Visibility = Visibility.Visible;
        RootLayout.UpdateLayout();
        catWindow.SetExtraTopContent(Math.Max(280, BubbleBorder.ActualWidth), BubbleBorder.ActualHeight + 8);
        catWindow.SetAnchor(activityArea.ClampAnchor(anchor, catWindow.CatSize), activityArea.Edge);
    }

    private void HideBubble()
    {
        var anchor = catWindow.CurrentAnchor(activityArea.Edge);
        BubbleBorder.Visibility = Visibility.Collapsed;
        BubbleImage.Source = null;
        BubbleImage.Visibility = Visibility.Collapsed;
        BubbleInputPanel.Visibility = Visibility.Collapsed;
        BubbleButtons.Children.Clear();
        catWindow.SetExtraTopContent(0, 0);
        catWindow.SetAnchor(activityArea.ClampAnchor(anchor, catWindow.CatSize), activityArea.Edge);
    }

    private void ToggleVisibilityFromTray()
    {
        if (IsVisible)
        {
            Hide();
        }
        else
        {
            Show();
            Activate();
        }
        UpdateTray();
    }

    private void UpdateTray()
    {
        trayIcon?.Update(
            IsVisible,
            stateMachine.State.Kind == CatStateKind.Walking,
            stateMachine.State.Kind == CatStateKind.OutingAway,
            StatusText(),
            RemainingText());
    }

    private void MainWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        if (isExitRequested)
        {
            return;
        }

        e.Cancel = true;
        Hide();
        UpdateTray();
    }

    private void ExitApplication()
    {
        isExitRequested = true;
        SaveUserData();
        Close();
        System.Windows.Application.Current.Shutdown();
    }

    private void RecordCompanionMinute()
    {
        usageStatistics.TotalCompanionSeconds += statisticsTimer.Interval.TotalSeconds;
        SaveUserData();
    }

    private void SaveUserData()
    {
        usageStatisticsStore.Save(usageStatistics);
        userDataBackupStore.Save(settings, usageStatistics, collectableInventory, outingCatalog);
    }

    private string StatusText()
    {
        return stateMachine.State.Kind switch
        {
            CatStateKind.Walking => $"{settings.CatName}正在散步",
            CatStateKind.Resting => $"{settings.CatName}正在休息",
            CatStateKind.Transitioning => $"{settings.CatName}伸了个懒腰",
            CatStateKind.Dragged => $"{settings.CatName}被抱起来了",
            CatStateKind.OutingAsking => $"{settings.CatName}正在问出门多久",
            CatStateKind.OutingConfirmingDeparture => $"{settings.CatName}准备出门",
            CatStateKind.OutingLeaving => $"{settings.CatName}正在出门",
            CatStateKind.OutingAway => $"{settings.CatName}出门中",
            CatStateKind.OutingReturning => $"{settings.CatName}正在回家",
            CatStateKind.OutingReturned => $"{settings.CatName}回来了",
            _ => "DockCatWin"
        };
    }

    private string OutingDepartureMessage()
    {
        var suffix = string.IsNullOrWhiteSpace(settings.OutingDepartureMessageSuffix)
            ? "工作要加油呀！"
            : settings.OutingDepartureMessageSuffix.Trim();
        return $"我出门啦，{settings.UserSalutation}{suffix}";
    }

    private string? RemainingText()
    {
        if (stateMachine.State.Kind == CatStateKind.OutingAway && settings.ActiveOutingEndDate is not null)
        {
            var remaining = settings.ActiveOutingEndDate.Value - DateTime.UtcNow;
            return remaining <= TimeSpan.Zero ? "马上回来" : $"剩余 {FormatDuration(remaining)}";
        }

        return null;
    }

    private static string FormatDuration(TimeSpan duration)
    {
        if (duration.TotalHours >= 1)
        {
            return $"{(int)duration.TotalHours}小时{duration.Minutes:00}分钟";
        }

        return $"{Math.Max(0, duration.Minutes)}分{duration.Seconds:00}秒";
    }

    private void ShowRecallConfirmation()
    {
        if (stateMachine.State.Kind != CatStateKind.OutingAway)
        {
            return;
        }

        Show();
        SetRandomImage(assetPack.DialoguePoses);
        catWindow.SetAnchor(activityArea.AnchorForPercent(settings.StartPositionPercent, catWindow.CatSize), activityArea.Edge);
        ShowBubble(
            $"提前召回会丢失可能的收藏品，确定要召回{settings.CatName}吗？",
            ("确认", () =>
            {
                forceEventReturn = true;
                HideBubble();
                ReturnFromOuting(drawReward: false);
            }),
            ("取消", () =>
            {
                HideBubble();
                Hide();
            }));
    }

    private void ShowOutingReturnBubble()
    {
        switch (pendingOutingReward)
        {
            case OutingReward.Event eventReward:
                if (collectableInventory.RecentNewCollectableID is not null)
                {
                    collectableInventory.ClearRecentNewMarker();
                    collectableInventoryStore.Save(collectableInventory);
                }
                usageStatistics.OutingEvents++;
                SaveUserData();
                ShowBubble(
                    $"{settings.UserSalutation}，我回来啦。{eventReward.Value.ChineseDescription}",
                    ("欢迎回来", FinishOutingReturn));
                break;
            case OutingReward.Collectable collectableReward:
                usageStatistics.OutingCollectables++;
                collectableInventory.RecordCollectable(collectableReward.Value.Id);
                collectableInventoryStore.Save(collectableInventory);
                SaveUserData();
                ShowBubble(
                    $"我回来啦，给{settings.UserSalutation}带了礼物：{collectableReward.Value.ChineseName}",
                    LoadBubbleImage(outingCatalog.OpenImageStreamFor(collectableReward.Value)),
                    showInput: false,
                    ("收下礼物", FinishOutingReturn));
                break;
            default:
                ShowBubble(
                    $"{settings.UserSalutation}，我回来啦",
                    ("欢迎回来", FinishOutingReturn));
                break;
        }
    }

    private void FinishOutingReturn()
    {
        pendingOutingReward = null;
        HideBubble();
        stateMachine.WelcomeBack();
    }

    private static BitmapImage? LoadBubbleImage(Stream? stream)
    {
        if (stream is null)
        {
            return null;
        }

        using (stream)
        {
            var image = new BitmapImage();
            image.BeginInit();
            image.CacheOption = BitmapCacheOption.OnLoad;
            image.StreamSource = stream;
            image.EndInit();
            image.Freeze();
            return image;
        }
    }
}
