using CogniBoost.Services;

namespace CogniBoost.Pages;

public partial class LoginPage : ContentPage
{
    public LoginPage()
    {
        InitializeComponent();
    }

    private async void OnLoginClicked(object? sender, EventArgs e)
    {
        var username = UsernameEntry.Text ?? string.Empty;
        var password = PasswordEntry.Text ?? string.Empty;

        if (!AccountStore.TrySignIn(username, password, out var error))
        {
            // Если пользователь не найден локально — пробуем облако
            if (error == "Пользователь не найден." && SupabaseConfig.IsConfigured)
            {
                ErrorLabel.Text = "Проверка облака...";
                ErrorLabel.IsVisible = true;

                var restore = await CloudSyncService.RestorePlayerDataAsync(username, password);
                if (restore.IsSuccess)
                {
                    App.ResetRootPage();
                    return;
                }

                ErrorLabel.Text = restore.Message;
                ErrorLabel.IsVisible = true;
                return;
            }

            ErrorLabel.Text = error;
            ErrorLabel.IsVisible = true;
            return;
        }

        // После локального входа — пытаемся подтянуть данные из облака
        if (SupabaseConfig.IsConfigured)
            _ = CloudSyncService.SyncCurrentUserAsync();

        App.ResetRootPage();
    }
}
