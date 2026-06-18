using CogniBoost.Services;

namespace CogniBoost.Pages;

public partial class LoginPage : ContentPage
{
    public LoginPage()
    {
        InitializeComponent();
    }

    private void OnLoginClicked(object? sender, EventArgs e)
    {
        if (!AccountStore.TrySignIn(
                UsernameEntry.Text ?? string.Empty,
                PasswordEntry.Text ?? string.Empty,
                out var error))
        {
            ErrorLabel.Text = error;
            ErrorLabel.IsVisible = true;
            return;
        }

        App.ResetRootPage();
    }
}
