using CogniBoost.Pages;

namespace CogniBoost;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();
        RegisterRoutes();
    }

    private static void RegisterRoutes()
    {
        // Маршруты для страниц вне нижнего меню (навигация через GoToAsync).
        Routing.RegisterRoute("store", typeof(StorePage));
        Routing.RegisterRoute("leaderboard", typeof(LeaderboardPage));
        Routing.RegisterRoute("settings", typeof(SettingsPage));
    }
}
