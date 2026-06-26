namespace CogniBoost.Services;

public static class HapticService
{
    public static void Click()
    {
        try { HapticFeedback.Default.Perform(HapticFeedbackType.Click); }
        catch { }
    }

    public static void Error()
    {
        try { HapticFeedback.Default.Perform(HapticFeedbackType.LongPress); }
        catch { }
    }

    public static void Success()
    {
        try { HapticFeedback.Default.Perform(HapticFeedbackType.Click); }
        catch { }
    }
}
