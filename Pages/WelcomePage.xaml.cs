using CogniBoost.Services;

namespace CogniBoost.Pages;

public partial class WelcomePage : ContentPage
{
    public WelcomePage()
    {
        InitializeComponent();
    }

    private async void OnRegisterClicked(object? sender, EventArgs e)
    {
        await Navigation.PushAsync(new RegisterPage());
    }

    private async void OnLoginClicked(object? sender, EventArgs e)
    {
        await Navigation.PushAsync(new LoginPage());
    }

    private async void OnGuestClicked(object? sender, EventArgs e)
    {
        await AccountStore.EnterGuestModeAsync();
        App.ResetRootPage();
    }
}
