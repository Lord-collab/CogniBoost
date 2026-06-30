namespace CogniBoost.Services;

/// <summary>
/// Настройки приложения: тема (светлая/тёмная/системная), крупный текст,
/// звук. Хранятся в таблице Settings (SQLite). ApplyAll() применяет тему
/// глобально и рекурсивно корректирует FontSize у всех Label/Button/Entry.
/// </summary>
public static class SettingsService
{
    private const string ThemeKey = "cb_theme";
    private const string LargeTextKey = "cb_large_text";
    private const string SoundKey = "cb_sound";
    public const string AppDownloadUrl = "https://github.com/Lord-collab/CogniBoost/releases/latest/download/CogniBoost.apk";
    public const string AppVersion = "1.2.0";

    private static string _theme = "system";
    private static bool _largeText;
    private static bool _soundEnabled = true;

    public static async Task InitAsync()
    {
        _theme = await DatabaseService.GetSettingAsync(ThemeKey, "system");
        _largeText = await DatabaseService.GetSettingAsync(LargeTextKey, "false") == "true";
        _soundEnabled = await DatabaseService.GetSettingAsync(SoundKey, "true") == "true";
    }

    public static string Theme => _theme;

    public static async Task SetThemeAsync(string value)
    {
        _theme = value;
        await DatabaseService.SetSettingAsync(ThemeKey, value);
    }

    public static bool LargeText => _largeText;

    public static async Task SetLargeTextAsync(bool value)
    {
        _largeText = value;
        await DatabaseService.SetSettingAsync(LargeTextKey, value ? "true" : "false");
    }

    public static bool SoundEnabled => _soundEnabled;

    public static async Task SetSoundEnabledAsync(bool value)
    {
        _soundEnabled = value;
        await DatabaseService.SetSettingAsync(SoundKey, value ? "true" : "false");
    }

    public static double TextScale => _largeText ? 1.15 : 1.0;

    public static void ApplyAll()
    {
        ApplyTheme();
        ApplyTextScaleToActivePage();
    }

    public static void ApplyTheme()
    {
        if (Application.Current is null) return;

        Application.Current.UserAppTheme = _theme switch
        {
            "light" => AppTheme.Light,
            "dark" => AppTheme.Dark,
            _ => AppTheme.Unspecified
        };
    }

    private static void ApplyTextScaleToActivePage()
    {
        if (Application.Current?.Windows is not { Count: > 0 } windows) return;

        var page = windows[0].Page;
        if (page is ContentPage cp) ApplyTextScale(cp);
        else if (page is NavigationPage nav && nav.CurrentPage is ContentPage navCp) ApplyTextScale(navCp);
        else if (page is Shell shell && shell.CurrentPage is ContentPage shellCp) ApplyTextScale(shellCp);
    }

    private static readonly BindableProperty BaseFontSizeProperty = BindableProperty.CreateAttached(
        "BaseFontSize", typeof(double), typeof(SettingsService), -1d);

    public static void ApplyTextScale(ContentPage page)
    {
        if (page.Content is not null)
            ApplyTextScale(page.Content, TextScale);
    }

    private static void ApplyTextScale(Element? element, double scale)
    {
        if (element is null) return;

        switch (element)
        {
            case Label label:
                ScaleFont(label, label.FontSize, v => label.FontSize = v, scale);
                break;
            case Button button:
                ScaleFont(button, button.FontSize, v => button.FontSize = v, scale);
                break;
            case Entry entry:
                ScaleFont(entry, entry.FontSize, v => entry.FontSize = v, scale);
                break;
            case Picker picker:
                ScaleFont(picker, picker.FontSize, v => picker.FontSize = v, scale);
                break;
        }

        foreach (var child in GetChildren(element))
            ApplyTextScale(child, scale);
    }

    private static void ScaleFont(BindableObject target, double currentSize, Action<double> setter, double scale)
    {
        if (currentSize <= 0) return;

        var baseSize = (double)target.GetValue(BaseFontSizeProperty);
        if (baseSize <= 0)
        {
            baseSize = currentSize;
            target.SetValue(BaseFontSizeProperty, baseSize);
        }

        setter(Math.Round(baseSize * scale, 1));
    }

    private static IEnumerable<Element> GetChildren(Element element)
    {
        switch (element)
        {
            case ScrollView scroll when scroll.Content is Element sc:
                yield return sc;
                break;
            case ContentView cv when cv.Content is Element cvc:
                yield return cvc;
                break;
            case Border border when border.Content is Element bc:
                yield return bc;
                break;
        }

        if (element is Layout layout)
        {
            foreach (var child in layout.Children.OfType<Element>())
                yield return child;
        }
    }
}
