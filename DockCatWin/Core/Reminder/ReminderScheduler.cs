using DockCatWin.Core.Settings;

namespace DockCatWin.Core.Reminder;

public sealed class ReminderScheduler
{
    private DateTime nextWaterDue = DateTime.UtcNow;
    private DateTime nextMovementDue = DateTime.UtcNow;
    private DateTime? nextCustomDue;
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
        nextCustomDue = settings.CustomReminderEnabled
            ? now.AddSeconds(settings.CustomReminderIntervalSeconds)
            : null;
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
        if (now >= nextMovementDue)
        {
            due = ReminderType.Movement;
        }
        else if (settings.CustomReminderEnabled && nextCustomDue is { } customDue && now >= customDue)
        {
            due = ReminderType.Custom;
        }
        else if (now >= nextWaterDue)
        {
            due = ReminderType.Water;
        }

        pendingReminder = due;
        return whenCatInLongDurationState ? due : null;
    }

    public void Complete(ReminderType type, AppSettings settings)
    {
        pendingReminder = null;
        switch (type)
        {
            case ReminderType.Water:
                Schedule(type, TimeSpan.FromSeconds(settings.WaterReminderIntervalSeconds));
                break;
            case ReminderType.Movement:
                Schedule(type, TimeSpan.FromSeconds(settings.MovementReminderIntervalSeconds));
                Schedule(ReminderType.Water, TimeSpan.FromSeconds(settings.WaterReminderIntervalSeconds));
                break;
            case ReminderType.Custom:
                if (settings.CustomReminderEnabled)
                {
                    Schedule(type, TimeSpan.FromSeconds(settings.CustomReminderIntervalSeconds));
                }
                else
                {
                    nextCustomDue = null;
                }
                break;
        }
    }

    public void Snooze(ReminderType type, TimeSpan delay)
    {
        pendingReminder = null;
        Schedule(type, delay);
        if (type == ReminderType.Movement && nextWaterDue <= DateTime.UtcNow.Add(delay))
        {
            Schedule(ReminderType.Water, delay);
        }
    }

    public void Clear()
    {
        pendingReminder = null;
    }

    public void RestartTimersFromNow(AppSettings settings)
    {
        if (!settings.RemindersEnabled)
        {
            Clear();
            return;
        }

        Reset(settings);
    }

    private void Schedule(ReminderType type, TimeSpan delay)
    {
        var next = DateTime.UtcNow.Add(delay);
        if (type == ReminderType.Water)
        {
            nextWaterDue = next;
        }
        else if (type == ReminderType.Movement)
        {
            nextMovementDue = next;
        }
        else
        {
            nextCustomDue = next;
        }
    }
}
