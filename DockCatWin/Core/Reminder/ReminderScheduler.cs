using DockCatWin.Core.Settings;

namespace DockCatWin.Core.Reminder;

public sealed class ReminderScheduler
{
    private DateTime nextWaterDue = DateTime.UtcNow;
    private DateTime nextMovementDue = DateTime.UtcNow;

    public ReminderScheduler(AppSettings settings)
    {
        Reset(settings);
    }

    public void Reset(AppSettings settings)
    {
        var now = DateTime.UtcNow;
        nextWaterDue = now.AddSeconds(settings.WaterReminderIntervalSeconds);
        nextMovementDue = now.AddSeconds(settings.MovementReminderIntervalSeconds);
    }

    public ReminderType? DueReminder(AppSettings settings)
    {
        if (!settings.RemindersEnabled)
        {
            return null;
        }

        var now = DateTime.UtcNow;
        if (now >= nextWaterDue)
        {
            return ReminderType.Water;
        }

        if (now >= nextMovementDue)
        {
            return ReminderType.Movement;
        }

        return null;
    }

    public void Complete(ReminderType type, AppSettings settings)
    {
        Schedule(type, type == ReminderType.Water
            ? TimeSpan.FromSeconds(settings.WaterReminderIntervalSeconds)
            : TimeSpan.FromSeconds(settings.MovementReminderIntervalSeconds));
    }

    public void Snooze(ReminderType type, TimeSpan delay)
    {
        Schedule(type, delay);
    }

    private void Schedule(ReminderType type, TimeSpan delay)
    {
        var next = DateTime.UtcNow.Add(delay);
        if (type == ReminderType.Water)
        {
            nextWaterDue = next;
        }
        else
        {
            nextMovementDue = next;
        }
    }
}
