using CogniBoost.Pages;
using CogniBoost.Services;

namespace CogniBoost;

public partial class App : Application
{
    private static readonly TimeSpan SessionTimeout = TimeSpan.FromMinutes(30);
    private DateTime _backgroundedAt = DateTime.MinValue;

    public App()
    {
        InitializeComponent();
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        var window = new Window(new ContentPage
        {
            BackgroundColor = Color.FromArgb("#0D0D2B"),
            Content = new VerticalStackLayout
            {
                VerticalOptions = LayoutOptions.Center,
                HorizontalOptions = LayoutOptions.Center,
                Children =
                {
                    new ActivityIndicator
                    {
                        IsRunning = true, Color = Color.FromArgb("#6C63FF"),
                        HeightRequest = 48, WidthRequest = 48
                    },
                    new Label
                    {
                        Text = "Загрузка...", FontSize = 16,
                        TextColor = Color.FromArgb("#C0C0D0"),
                        Margin = new Thickness(0, 16, 0, 0)
                    }
                }
            }
        });

        // Инициализация БД без блокировки UI
        _ = Task.Run(async () =>
        {
            await DatabaseService.InitAsync();
            MainThread.BeginInvokeOnMainThread(() =>
            {
                try
                {
                    SettingsService.ApplyTheme();
                    window.Page = BuildRootPage();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[App] Init error: {ex}");
                    window.Page = new ContentPage
                    {
                        BackgroundColor = Color.FromArgb("#0D0D2B"),
                        Content = new Label
                        {
                            Text = $"Ошибка: {ex.Message}",
                            TextColor = Colors.Red,
                            HorizontalOptions = LayoutOptions.Center,
                            VerticalOptions = LayoutOptions.Center
                        }
                    };
                }
            });
        });

        window.Deactivated += (_, _) => _backgroundedAt = DateTime.UtcNow;
        window.Activated   += (_, _) => CheckSessionTimeout();

        return window;
    }

    private void CheckSessionTimeout()
    {
        if (!AccountStore.IsSignedIn) return;
        if (_backgroundedAt == DateTime.MinValue) return;

        var elapsed = DateTime.UtcNow - _backgroundedAt;
        if (elapsed >= SessionTimeout)
        {
            if (Windows.Count > 0)
                Windows[0].Page = new NavigationPage(new SessionLockPage());
        }
    }

    public static Page BuildRootPage()
    {
        if (AccountStore.IsGuest)
            return new AppShell();

        if (!AccountStore.IsSignedIn)
            return new NavigationPage(new WelcomePage());

        if (!AccountStore.IsCurrentUserOnboarded)
            return new NavigationPage(new OnboardingPage());

        return new AppShell();
    }

    public static void ResetRootPage()
    {
        if (Current?.Windows.Count > 0)
            Current.Windows[0].Page = BuildRootPage();
    }
}
