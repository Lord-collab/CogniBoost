using CogniBoost.Services;

namespace CogniBoost.Pages;

/// <summary>Страница регистрации: имя, возраст, пароль, подсказка, подтверждение.</summary>
public partial class RegisterPage : ContentPage
{
    public RegisterPage()
    {
        InitializeComponent();
    }

    private async void OnRegisterClicked(object? sender, EventArgs e)
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

        var hint = string.IsNullOrWhiteSpace(HintEntry.Text) ? null : HintEntry.Text.Trim();

        if (AccountStore.IsGuest)
        {
            await AccountStore.MigrateGuestDataAsync(
                UsernameEntry.Text!.Trim(), age, PasswordEntry.Text!, hint);
        }
        else
        {
            await AccountStore.SaveAccountAsync(UsernameEntry.Text!.Trim(), age, PasswordEntry.Text!, hint);
        }

        // Синхронизируем профиль с облаком (fire-and-forget)
        if (SupabaseConfig.IsConfigured)
            _ = CloudSyncService.SyncCurrentUserAsync();

        // После регистрации сразу ведём на онбординг (выбор направлений).
        App.ResetRootPage();
    }

    private void ShowError(string message)
    {
        ErrorLabel.Text = message;
        ErrorLabel.IsVisible = true;
    }
}
