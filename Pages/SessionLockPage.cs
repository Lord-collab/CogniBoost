using CogniBoost.Services;
using Microsoft.Maui.Controls.Shapes;

namespace CogniBoost.Pages;

/// <summary>
/// Экран блокировки сессии — показывается если приложение было в фоне > 30 минут.
/// Пользователь вводит пароль для продолжения или выходит из аккаунта.
/// </summary>
public sealed class SessionLockPage : ContentPage
{
    private readonly Entry _passwordEntry = new() { Placeholder = "Введи пароль", IsPassword = true };
    private readonly Label _errorLabel    = new();
    private readonly Label _greetingLabel = new();

    public SessionLockPage()
    {
        NavigationPage.SetHasNavigationBar(this, false);
        Shell.SetNavBarIsVisible(this, false);
        BackgroundColor = Color.FromArgb("#0D0D2B");

        AccountStore.TryGetCurrentProfile(out var profile);
        var name = profile?.Username ?? "Добро пожаловать";

        _greetingLabel.Text      = $"👋 {name}";
        _greetingLabel.FontSize  = 28;
        _greetingLabel.FontAttributes = FontAttributes.Bold;
        _greetingLabel.TextColor = Colors.White;
        _greetingLabel.HorizontalOptions = LayoutOptions.Center;

        _errorLabel.TextColor  = Color.FromArgb("#FF5370");
        _errorLabel.FontSize   = 13;
        _errorLabel.IsVisible  = false;
        _errorLabel.HorizontalOptions = LayoutOptions.Center;

        _passwordEntry.BackgroundColor = Colors.White;

        var unlockBtn = new Button
        {
            Text = "Продолжить",
            BackgroundColor = Color.FromArgb("#6C63FF"), TextColor = Colors.White,
            FontAttributes = FontAttributes.Bold, HeightRequest = 52, CornerRadius = 14
        };
        unlockBtn.Clicked += OnUnlock;

        var signOutBtn = new Button
        {
            Text = "Сменить пользователя",
            BackgroundColor = Colors.Transparent, TextColor = Colors.White,
            BorderColor = Colors.White, BorderWidth = 1,
            HeightRequest = 50, CornerRadius = 14
        };
        signOutBtn.Clicked += (_, _) =>
        {
            AccountStore.SignOut();
            App.ResetRootPage();
        };

        var card = new Border
        {
            StrokeShape = new RoundRectangle { CornerRadius = 24 },
            Stroke = Colors.Transparent, BackgroundColor = Color.FromArgb("#13142A"),
            Padding = new Thickness(24),
            Content = new VerticalStackLayout
            {
                Spacing = 16,
                Children =
                {
                    new Label { Text = "🔒", FontSize = 52, HorizontalOptions = LayoutOptions.Center },
                    new Label { Text = "Сессия заблокирована", FontSize = 20,
                        FontAttributes = FontAttributes.Bold, TextColor = Colors.White,
                        HorizontalOptions = LayoutOptions.Center },
                    new Label { Text = "Введи пароль чтобы продолжить", FontSize = 14,
                        TextColor = Color.FromArgb("#6B6C99"), HorizontalOptions = LayoutOptions.Center },
                    new Border
                    {
                        StrokeShape = new RoundRectangle { CornerRadius = 12 },
                        Stroke = Color.FromArgb("#252650"), StrokeThickness = 1,
                        Padding = new Thickness(12, 4), Content = _passwordEntry
                    },
                    _errorLabel,
                    unlockBtn,
                    signOutBtn,
                }
            }
        };

        Content = new Grid
        {
            Padding = 24,
            RowDefinitions =
            {
                new RowDefinition { Height = GridLength.Star },
                new RowDefinition { Height = GridLength.Auto },
                new RowDefinition { Height = GridLength.Star },
            },
            Children =
            {
                _greetingLabel,
                card,
            }
        };
        Grid.SetRow(_greetingLabel, 0);
        Grid.SetRow(card, 1);

        // Фокус на поле пароля
        Loaded += (_, _) => _passwordEntry.Focus();
    }

    private void OnUnlock(object? sender, EventArgs e)
    {
        if (!AccountStore.TrySignIn(
                AccountStore.GetCurrentUsernameKey(),
                _passwordEntry.Text ?? string.Empty,
                out var error))
        {
            _errorLabel.Text      = error;
            _errorLabel.IsVisible = true;
            return;
        }

        App.ResetRootPage();
    }
}
