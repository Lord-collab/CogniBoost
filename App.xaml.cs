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
        Services.SettingsService.ApplyTheme();
        L10n.Load();
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        var window = new Window(BuildRootPage());

        // Отслеживаем переход в фон / возврат
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
            // Показываем экран приветствия-блокировки без выхода из аккаунта
            if (Windows.Count > 0)
                Windows[0].Page = new NavigationPage(new SessionLockPage());
        }
    }

    public static Page BuildRootPage()
    {
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
