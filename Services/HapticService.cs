namespace CogniBoost.Services;

/// <summary>
/// Тактильная обратная связь (вибрация). Click / Error / Success.
/// Тихий catch — вибрация не критична, на некоторых устройствах не поддерживается.
/// </summary>
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
