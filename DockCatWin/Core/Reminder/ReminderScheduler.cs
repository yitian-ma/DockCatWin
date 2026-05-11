using DockCatWin.Core.Settings;

namespace DockCatWin.Core.Reminder;

public sealed class ReminderScheduler
{
    private DateTime nextWaterDue = DateTime.UtcNow;
    private DateTime nextMovementDue = DateTime.UtcNow;
    private ReminderType? pendingReminder;

    public ReminderScheduler(AppSettings settings)
    {
        Reset(settings);
    }

    public void Reset(AppSettings settings)
    {
        var now = DateTime.UtcNow;
        nextWaterDue = now.AddSeconds(settings.WaterReminderIntervalSeconds);
        nextMovementDue = now.AddSeconds(settings.MovementReminderIntervalSeconds);
        pendingReminder = null;
    }

    public ReminderType? DueReminder(AppSettings settings, bool whenCatInLongDurationState)
    {
        if (!settings.RemindersEnabled)
        {
            return null;
        }

        if (pendingReminder is not null)
        {
            return whenCatInLongDurationState ? pendingReminder : null;
        }

        var now = DateTime.UtcNow;
        ReminderType? due = null;
        if (now >= nextWaterDue)
        {
            due = ReminderType.Water;
        }
        else if (now >= nextMovementDue)
        {
            due = ReminderType.Movement;
        }

        pendingReminder = due;
        return whenCatInLongDurationState ? due : null;
    }

    public void Complete(ReminderType type, AppSettings settings)
    {
        pendingReminder = null;
        Schedule(type, type == ReminderType.Water
            ? TimeSpan.FromSeconds(settings.WaterReminderIntervalSeconds)
            : TimeSpan.FromSeconds(settings.MovementReminderIntervalSeconds));
    }

    public void Snooze(ReminderType type, TimeSpan delay)
    {
        pendingReminder = null;
        Schedule(type, delay);
    }

    public void Clear()
    {
        pendingReminder = null;
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
