using System.Globalization;
using System.IO;
using System.Windows;
using DockCatWin.Core.Outing;
using DockCatWin.Core.Statistics;
using DockCatWin.Platform;
using DockCatWin.Core.Settings;

namespace DockCatWin;

public partial class SettingsWindow : Window
{
    private readonly Func<IEnumerable<string>> assetPackIDsProvider;
    private readonly Func<string, string> assetPackStatusProvider;
    private readonly string assetPacksRoot;

    public SettingsWindow(
        AppSettings settings,
        IEnumerable<string> assetPackIDs,
        Func<IEnumerable<string>> assetPackIDsProvider,
        Func<string, string> assetPackStatusProvider,
        string assetPacksRoot,
        UsageStatistics statistics,
        OutingCatalog outingCatalog,
        CollectableInventory collectableInventory)
    {
        InitializeComponent();
        this.assetPackIDsProvider = assetPackIDsProvider;
        this.assetPackStatusProvider = assetPackStatusProvider;
        this.assetPacksRoot = assetPacksRoot;
        Statistics = statistics.Clone();
        OutingCatalog = outingCatalog;
        CollectableInventory = collectableInventory;
        Settings = settings.Clone();
        PopulateAssetPacks(assetPackIDs);
        Populate(Settings);
    }

    public AppSettings Settings { get; private set; }
    public UsageStatistics Statistics { get; }
    public OutingCatalog OutingCatalog { get; }
    public CollectableInventory CollectableInventory { get; }

    private void Populate(AppSettings settings)
    {
        CatNameBox.Text = settings.CatName;
        CatIdentifierBox.Text = settings.CatIdentifier;
        AssetPackBox.Text = settings.SelectedAssetPackID;
        UserSalutationBox.Text = settings.UserSalutation;
        PopulateDisplays(settings.ActivityDisplayID);
        CatScaleBox.Text = Format(settings.CatScalePercent);
        StartPositionBox.Text = Format(settings.StartPositionPercent);
        RestMinBox.Text = Format(settings.RestDurationMinimumSeconds / 60);
        RestMaxBox.Text = Format(settings.RestDurationMaximumSeconds / 60);
        WalkMinBox.Text = Format(settings.WalkDurationMinimumSeconds / 60);
        WalkMaxBox.Text = Format(settings.WalkDurationMaximumSeconds / 60);
        WaterReminderBox.Text = Format(settings.WaterReminderIntervalSeconds / 60);
        WaterReminderMessageBox.Text = settings.WaterReminderMessageSuffix;
        MovementReminderBox.Text = Format(settings.MovementReminderIntervalSeconds / 60);
        MovementReminderMessageBox.Text = settings.MovementReminderMessageSuffix;
        CustomReminderEnabledBox.IsChecked = settings.CustomReminderEnabled;
        CustomReminderBox.Text = Format(settings.CustomReminderIntervalSeconds / 60);
        CustomReminderMessageBox.Text = settings.CustomReminderMessageSuffix;
        DefaultOutingBox.Text = Format(settings.DefaultOutingDurationSeconds / 60);
        OutingDepartureMessageBox.Text = settings.OutingDepartureMessageSuffix;
        RemindersEnabledBox.IsChecked = settings.RemindersEnabled;
        StatisticsText.Text = StatisticsTextValue();
        CollectablesText.Text = CollectablesTextValue();
        UpdateAssetPackStatus();
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        if (!TryReadSettings(out var updated))
        {
            System.Windows.MessageBox.Show(this, "请检查数值设置。", "无法保存", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        Settings = updated;
        DialogResult = true;
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }

    private void RefreshAssetPacks_Click(object sender, RoutedEventArgs e)
    {
        PopulateAssetPacks(assetPackIDsProvider());
        UpdateAssetPackStatus();
    }

    private void OpenAssetFolder_Click(object sender, RoutedEventArgs e)
    {
        Directory.CreateDirectory(assetPacksRoot);
        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
        {
            FileName = assetPacksRoot,
            UseShellExecute = true
        });
    }

    private void AssetPackBox_Changed(object sender, EventArgs e)
    {
        UpdateAssetPackStatus();
    }

    private bool TryReadSettings(out AppSettings settings)
    {
        settings = Settings.Clone();

        if (!TryReadDouble(CatScaleBox.Text, out var scale)
            || !TryReadDouble(StartPositionBox.Text, out var startPosition)
            || !TryReadDouble(RestMinBox.Text, out var restMin)
            || !TryReadDouble(RestMaxBox.Text, out var restMax)
            || !TryReadDouble(WalkMinBox.Text, out var walkMin)
            || !TryReadDouble(WalkMaxBox.Text, out var walkMax)
            || !TryReadDouble(WaterReminderBox.Text, out var waterReminder)
            || !TryReadDouble(MovementReminderBox.Text, out var movementReminder)
            || !TryReadDouble(CustomReminderBox.Text, out var customReminder)
            || !TryReadDouble(DefaultOutingBox.Text, out var defaultOuting))
        {
            return false;
        }

        settings.CatName = CatNameBox.Text;
        settings.CatIdentifier = CatIdentifierBox.Text;
        settings.SelectedAssetPackID = AssetPackBox.Text;
        settings.UserSalutation = UserSalutationBox.Text;
        settings.ActivityDisplayID = DisplayBox.SelectedValue as string;
        settings.CatScalePercent = scale;
        settings.StartPositionPercent = startPosition;
        settings.RestDurationMinimumSeconds = restMin * 60;
        settings.RestDurationMaximumSeconds = restMax * 60;
        settings.WalkDurationMinimumSeconds = walkMin * 60;
        settings.WalkDurationMaximumSeconds = walkMax * 60;
        settings.WaterReminderIntervalSeconds = waterReminder * 60;
        settings.WaterReminderMessageSuffix = WaterReminderMessageBox.Text;
        settings.MovementReminderIntervalSeconds = movementReminder * 60;
        settings.MovementReminderMessageSuffix = MovementReminderMessageBox.Text;
        settings.CustomReminderEnabled = CustomReminderEnabledBox.IsChecked == true;
        settings.CustomReminderIntervalSeconds = customReminder * 60;
        settings.CustomReminderMessageSuffix = CustomReminderMessageBox.Text;
        settings.DefaultOutingDurationSeconds = defaultOuting * 60;
        settings.OutingDepartureMessageSuffix = OutingDepartureMessageBox.Text;
        settings.RemindersEnabled = RemindersEnabledBox.IsChecked == true;
        settings.Normalize();
        return true;
    }

    private void PopulateAssetPacks(IEnumerable<string> assetPackIDs)
    {
        var current = AssetPackBox.Text;
        AssetPackBox.Items.Clear();
        AssetPackBox.Items.Add("default-lizz");
        foreach (var id in assetPackIDs.Where(id => id != "default-lizz").Distinct().Order(StringComparer.OrdinalIgnoreCase))
        {
            AssetPackBox.Items.Add(id);
        }

        if (!string.IsNullOrWhiteSpace(current))
        {
            AssetPackBox.Text = current;
        }
    }

    private void PopulateDisplays(string? selectedDisplayID)
    {
        DisplayBox.Items.Clear();
        DisplayBox.Items.Add(new DisplayOption("", "主显示器"));
        foreach (var option in TaskbarGeometry.DisplayOptions())
        {
            DisplayBox.Items.Add(option);
        }

        DisplayBox.SelectedValue = selectedDisplayID ?? "";
        if (DisplayBox.SelectedIndex < 0)
        {
            DisplayBox.SelectedIndex = 0;
        }
    }

    private string StatisticsTextValue()
    {
        var total = TimeSpan.FromSeconds(Statistics.TotalCompanionSeconds);
        var collectedKinds = CollectableInventory.Entries.Count;
        var totalKinds = OutingCatalog.Collectables.Count;
        var recent = CollectableInventory.RecentNewCollectableID is { Length: > 0 } recentID
            ? OutingCatalog.Collectables.FirstOrDefault(item => item.Id == recentID)?.ChineseName
            : null;
        var recentText = string.IsNullOrWhiteSpace(recent) ? "" : $"，最近获得 {recent}";
        return $"陪伴 {Math.Floor(total.TotalHours):0}小时{total.Minutes:00}分钟，喝水完成 {Statistics.CompletedWaterReminders} 次，走动完成 {Statistics.CompletedMovementReminders} 次，出门见闻 {Statistics.OutingEvents} 次，带回礼物 {Statistics.OutingCollectables} 次，收藏 {collectedKinds}/{totalKinds} 种{recentText}";
    }

    private string CollectablesTextValue()
    {
        if (CollectableInventory.Entries.Count == 0)
        {
            return "还没有收藏品。";
        }

        var names = CollectableInventory.Entries.Values
            .OrderByDescending(entry => entry.LastAcquiredAt)
            .Take(8)
            .Select(entry =>
            {
                var item = OutingCatalog.Collectables.FirstOrDefault(collectable => collectable.Id == entry.CollectableID);
                var name = item?.ChineseName ?? entry.CollectableID;
                return entry.Count > 1 ? $"{name} x{entry.Count}" : name;
            });
        return string.Join("、", names);
    }

    private void UpdateAssetPackStatus()
    {
        if (AssetPackStatusText is null)
        {
            return;
        }

        AssetPackStatusText.Text = assetPackStatusProvider(AssetPackBox.Text);
    }

    private static bool TryReadDouble(string text, out double value)
    {
        return double.TryParse(text, NumberStyles.Float, CultureInfo.CurrentCulture, out value)
            || double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out value);
    }

    private static string Format(double value)
    {
        return value.ToString("0.##", CultureInfo.CurrentCulture);
    }
}
