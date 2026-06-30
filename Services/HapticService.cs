namespace CogniBoost.Services;

public static class HapticService
{
    public static void Click()
    {
        try { HapticFeedback.Default.Perform(HapticFeedbackType.Click); }
        catch { /* Игнорируем — вибрация может не поддерживаться устройством */ }
    }

    public static void Error()
    {
        try { HapticFeedback.Default.Perform(HapticFeedbackType.LongPress); }
        catch { /* Игнорируем — вибрация может не поддерживаться устройством */ }
    }

    public static void Success()
    {
        try { HapticFeedback.Default.Perform(HapticFeedbackType.Click); }
        catch { /* Игнорируем — вибрация может не поддерживаться устройством */ }
    }
}
