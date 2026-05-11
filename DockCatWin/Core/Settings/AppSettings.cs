namespace DockCatWin.Core.Settings;

public sealed class AppSettings
{
    public string CatName { get; set; } = "栗子";
    public string CatIdentifier { get; set; } = "Lizz";
    public string UserSalutation { get; set; } = "妈妈";
    public string SelectedAssetPackID { get; set; } = "default-lizz";
    public bool RemindersEnabled { get; set; } = true;
    public double WaterReminderIntervalSeconds { get; set; } = 30 * 60;
    public double MovementReminderIntervalSeconds { get; set; } = 60 * 60;
    public double DefaultOutingDurationSeconds { get; set; } = 25 * 60;
    public double RestDurationMinimumSeconds { get; set; } = 2 * 60;
    public double RestDurationMaximumSeconds { get; set; } = 5 * 60;
    public double WalkDurationMinimumSeconds { get; set; } = 2 * 60;
    public double WalkDurationMaximumSeconds { get; set; } = 5 * 60;
    public double WalkBaseSpeed { get; set; } = 36;
    public double CatScalePercent { get; set; } = 10;
    public double StartPositionPercent { get; set; } = 75;

    public static AppSettings Defaults => new();

    public AppSettings Clone()
    {
        return new AppSettings
        {
            CatName = CatName,
            CatIdentifier = CatIdentifier,
            UserSalutation = UserSalutation,
            SelectedAssetPackID = SelectedAssetPackID,
            RemindersEnabled = RemindersEnabled,
            WaterReminderIntervalSeconds = WaterReminderIntervalSeconds,
            MovementReminderIntervalSeconds = MovementReminderIntervalSeconds,
            DefaultOutingDurationSeconds = DefaultOutingDurationSeconds,
            RestDurationMinimumSeconds = RestDurationMinimumSeconds,
            RestDurationMaximumSeconds = RestDurationMaximumSeconds,
            WalkDurationMinimumSeconds = WalkDurationMinimumSeconds,
            WalkDurationMaximumSeconds = WalkDurationMaximumSeconds,
            WalkBaseSpeed = WalkBaseSpeed,
            CatScalePercent = CatScalePercent,
            StartPositionPercent = StartPositionPercent
        };
    }

    public void Normalize()
    {
        CatName = string.IsNullOrWhiteSpace(CatName) ? "栗子" : CatName.Trim();
        CatIdentifier = string.IsNullOrWhiteSpace(CatIdentifier) ? "Lizz" : CatIdentifier.Trim();
        UserSalutation = string.IsNullOrWhiteSpace(UserSalutation) ? "妈妈" : UserSalutation.Trim();
        SelectedAssetPackID = string.IsNullOrWhiteSpace(SelectedAssetPackID) ? "default-lizz" : SelectedAssetPackID.Trim();
        CatScalePercent = Math.Clamp(CatScalePercent, 4, 30);
        StartPositionPercent = Math.Clamp(StartPositionPercent, 0, 100);
        WalkBaseSpeed = Math.Clamp(WalkBaseSpeed, 10, 180);

        RestDurationMinimumSeconds = Math.Clamp(RestDurationMinimumSeconds, 10, 24 * 60 * 60);
        RestDurationMaximumSeconds = Math.Clamp(RestDurationMaximumSeconds, RestDurationMinimumSeconds, 24 * 60 * 60);
        WalkDurationMinimumSeconds = Math.Clamp(WalkDurationMinimumSeconds, 10, 24 * 60 * 60);
        WalkDurationMaximumSeconds = Math.Clamp(WalkDurationMaximumSeconds, WalkDurationMinimumSeconds, 24 * 60 * 60);
        WaterReminderIntervalSeconds = Math.Clamp(WaterReminderIntervalSeconds, 60, 24 * 60 * 60);
        MovementReminderIntervalSeconds = Math.Clamp(MovementReminderIntervalSeconds, 60, 24 * 60 * 60);
        DefaultOutingDurationSeconds = Math.Clamp(DefaultOutingDurationSeconds, 60, 24 * 60 * 60);
    }
}
