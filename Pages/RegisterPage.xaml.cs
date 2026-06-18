using CogniBoost.Services;

namespace CogniBoost.Pages;

public partial class RegisterPage : ContentPage
{
    public RegisterPage()
    {
        InitializeComponent();
    }

    private void OnRegisterClicked(object? sender, EventArgs e)
    {
        if (!AccountStore.TryValidateRegistration(
                UsernameEntry.Text ?? string.Empty,
                AgeEntry.Text ?? string.Empty,
                PasswordEntry.Text ?? string.Empty,
                ConfirmEntry.Text ?? string.Empty,
                out var age,
                out var error))
        {
            ShowError(error);
            return;
        }

        AccountStore.SaveAccount(UsernameEntry.Text!.Trim(), age, PasswordEntry.Text!);

        // После регистрации сразу ведём на онбординг (выбор направлений).
        App.ResetRootPage();
    }

    private void ShowError(string message)
    {
        ErrorLabel.Text = message;
        ErrorLabel.IsVisible = true;
    }
}
