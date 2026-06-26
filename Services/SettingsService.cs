namespace CogniBoost.Services;

public static class SettingsService
{
    private const string ThemeKey = "cb_theme";
    private const string LargeTextKey = "cb_large_text";
    private const string SoundKey = "cb_sound";
    public const string AppDownloadUrl = "https://github.com/Lord-collab/CogniBoost/releases/latest";
    public const string AppVersion = "1.0.0";

    public static string Theme
    {
        get => DatabaseService.Sync(async () =>
            await DatabaseService.GetSettingAsync(ThemeKey, "system"));
        set => DatabaseService.Sync(async () =>
            await DatabaseService.SetSettingAsync(ThemeKey, value));
    }

    public static bool LargeText
    {
        get => DatabaseService.Sync(async () =>
            await DatabaseService.GetSettingAsync(LargeTextKey, "false")) == "true";
        set => DatabaseService.Sync(async () =>
            await DatabaseService.SetSettingAsync(LargeTextKey, value ? "true" : "false"));
    }

    public static bool SoundEnabled
    {
        get => DatabaseService.Sync(async () =>
            await DatabaseService.GetSettingAsync(SoundKey, "true")) == "true";
        set => DatabaseService.Sync(async () =>
            await DatabaseService.SetSettingAsync(SoundKey, value ? "true" : "false"));
    }

    public static double TextScale => LargeText ? 1.15 : 1.0;

    public static void ApplyAll()
    {
        ApplyTheme();
        if (Application.Current?.Windows.Count > 0)
        {
            var page = Application.Current.Windows[0].Page;
            if (page is ContentPage cp) ApplyTextScale(cp);
            else if (page is NavigationPage nav && nav.CurrentPage is ContentPage navCp) ApplyTextScale(navCp);
            else if (page is Shell shell && shell.CurrentPage is ContentPage shellCp) ApplyTextScale(shellCp);
        }
    }

    public static void ApplyTheme()
    {
        if (Application.Current is null) return;

        Application.Current.UserAppTheme = Theme switch
        {
            "light" => AppTheme.Light,
            "dark" => AppTheme.Dark,
            _ => AppTheme.Unspecified
        };
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
