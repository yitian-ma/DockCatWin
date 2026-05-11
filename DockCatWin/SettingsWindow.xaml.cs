using System.Globalization;
using System.IO;
using System.Windows;
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
        string assetPacksRoot)
    {
        InitializeComponent();
        this.assetPackIDsProvider = assetPackIDsProvider;
        this.assetPackStatusProvider = assetPackStatusProvider;
        this.assetPacksRoot = assetPacksRoot;
        Settings = settings.Clone();
        PopulateAssetPacks(assetPackIDs);
        Populate(Settings);
    }

    public AppSettings Settings { get; private set; }

    private void Populate(AppSettings settings)
    {
        CatNameBox.Text = settings.CatName;
        CatIdentifierBox.Text = settings.CatIdentifier;
        AssetPackBox.Text = settings.SelectedAssetPackID;
        UserSalutationBox.Text = settings.UserSalutation;
        CatScaleBox.Text = Format(settings.CatScalePercent);
        StartPositionBox.Text = Format(settings.StartPositionPercent);
        RestMinBox.Text = Format(settings.RestDurationMinimumSeconds / 60);
        RestMaxBox.Text = Format(settings.RestDurationMaximumSeconds / 60);
        WalkMinBox.Text = Format(settings.WalkDurationMinimumSeconds / 60);
        WalkMaxBox.Text = Format(settings.WalkDurationMaximumSeconds / 60);
        WaterReminderBox.Text = Format(settings.WaterReminderIntervalSeconds / 60);
        MovementReminderBox.Text = Format(settings.MovementReminderIntervalSeconds / 60);
        RemindersEnabledBox.IsChecked = settings.RemindersEnabled;
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
            || !TryReadDouble(MovementReminderBox.Text, out var movementReminder))
        {
            return false;
        }

        settings.CatName = CatNameBox.Text;
        settings.CatIdentifier = CatIdentifierBox.Text;
        settings.SelectedAssetPackID = AssetPackBox.Text;
        settings.UserSalutation = UserSalutationBox.Text;
        settings.CatScalePercent = scale;
        settings.StartPositionPercent = startPosition;
        settings.RestDurationMinimumSeconds = restMin * 60;
        settings.RestDurationMaximumSeconds = restMax * 60;
        settings.WalkDurationMinimumSeconds = walkMin * 60;
        settings.WalkDurationMaximumSeconds = walkMax * 60;
        settings.WaterReminderIntervalSeconds = waterReminder * 60;
        settings.MovementReminderIntervalSeconds = movementReminder * 60;
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
