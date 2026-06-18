using Plugin.LocalNotification;

namespace CogniBoost.Services;

/// <summary>
/// Управляет локальными уведомлениями: ежедневное напоминание о тренировке,
/// предупреждение о прерывании streak.
/// </summary>
public static class NotificationService
{
    private const string ReminderTimeKey = "cb_notif_time";
    private const string EnabledKey      = "cb_notif_enabled";

    private const int TrainingNotifId  = 1001;
    private const int StreakNotifId    = 1002;

    public static bool IsEnabled
    {
        get => Preferences.Default.Get(EnabledKey, false);
        set => Preferences.Default.Set(EnabledKey, value);
    }

    public static TimeSpan ReminderTime
    {
        get
        {
            var raw = Preferences.Default.Get(ReminderTimeKey, "20:00");
            return TimeSpan.TryParse(raw, out var t) ? t : TimeSpan.FromHours(20);
        }
        set => Preferences.Default.Set(ReminderTimeKey, value.ToString(@"hh\:mm"));
    }

    /// <summary>
    /// Запланировать ежедневное напоминание на выбранное время.
    /// Вызывается при включении уведомлений или смене времени.
    /// </summary>
    public static async Task ScheduleDailyReminderAsync()
    {
        if (!IsEnabled) return;

        var streak = StreakService.GetCurrentStreak();
        var body   = streak >= 3
            ? $"Серия {streak} дней! Не прерывай — сыграй хотя бы одну игру 🔥"
            : "Пора прокачать мозг! Ежедневная тренировка ждёт тебя 🧠";

        var notif = new NotificationRequest
        {
            NotificationId = TrainingNotifId,
            Title          = "CogniBoost — тренировка",
            Description    = body,
            Schedule       = new NotificationRequestSchedule
            {
                NotifyTime   = NextOccurrence(ReminderTime),
                RepeatType   = NotificationRepeat.Daily,
            }
        };

        await LocalNotificationCenter.Current.Show(notif);
    }

    /// <summary>Отменить все запланированные уведомления.</summary>
    public static void CancelAll()
    {
        LocalNotificationCenter.Current.Cancel(TrainingNotifId);
        LocalNotificationCenter.Current.Cancel(StreakNotifId);
    }

    private static DateTime NextOccurrence(TimeSpan time)
    {
        var now  = DateTime.Now;
        var next = now.Date.Add(time);
        if (next <= now) next = next.AddDays(1);
        return next;
    }
}
