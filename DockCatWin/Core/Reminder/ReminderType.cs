namespace DockCatWin.Core.Reminder;

using DockCatWin.Core.Settings;

public enum ReminderType
{
    Water,
    Movement,
    Custom
}

public static class ReminderTypeExtensions
{
    public static string Message(this ReminderType type, AppSettings settings)
    {
        return type.Message(settings.UserSalutation, settings.ReminderMessageSuffix(type));
    }

    public static string Message(this ReminderType type, string salutation, string suffix)
    {
        var trimmedSuffix = string.IsNullOrWhiteSpace(suffix) ? "休息一下吧" : suffix.Trim();
        return $"{salutation}，{trimmedSuffix}";
    }
}
