using CogniBoost.Services;
using Microsoft.Maui.Controls.Shapes;

namespace CogniBoost.Pages;

public sealed class SessionLockPage : ContentPage
{
    private readonly Entry _passwordEntry = new() { Placeholder = "Введи пароль", IsPassword = true };
    private readonly Label _errorLabel    = new();
    private readonly Label _greetingLabel = new();

    public SessionLockPage()
    {
        NavigationPage.SetHasNavigationBar(this, false);
        Shell.SetNavBarIsVisible(this, false);
        BackgroundColor = ThemeColors.PageBg;

        _ = LoadProfileAsync();

        _errorLabel.TextColor  = ThemeColors.Error;
        _errorLabel.FontSize   = 13;
        _errorLabel.IsVisible  = false;
        _errorLabel.HorizontalOptions = LayoutOptions.Center;

        _passwordEntry.BackgroundColor = ThemeColors.CardBg;
        _passwordEntry.TextColor = ThemeColors.TextPrimary;
        _passwordEntry.PlaceholderColor = ThemeColors.TextMuted;

        var unlockBtn = new Button
        {
            Text = "Продолжить",
            BackgroundColor = ThemeColors.Accent, TextColor = Colors.White,
            FontAttributes = FontAttributes.Bold, HeightRequest = 52, CornerRadius = 14
        };
        unlockBtn.Clicked += OnUnlock;

        var signOutBtn = new Button
        {
            Text = "Сменить пользователя",
            BackgroundColor = Colors.Transparent, TextColor = ThemeColors.TextPrimary,
            BorderColor = ThemeColors.TextPrimary, BorderWidth = 1,
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
            Stroke = Colors.Transparent, BackgroundColor = ThemeColors.CardBg,
            Padding = new Thickness(24),
            Content = new VerticalStackLayout
            {
                Spacing = 16,
                Children =
                {
                    new Label { Text = "🔒", FontSize = 52, HorizontalOptions = LayoutOptions.Center },
                    new Label { Text = "Сессия заблокирована", FontSize = 20,
                        FontAttributes = FontAttributes.Bold, TextColor = ThemeColors.TextPrimary,
                        HorizontalOptions = LayoutOptions.Center },
                    new Label { Text = "Введи пароль чтобы продолжить", FontSize = 14,
                        TextColor = ThemeColors.TextMuted, HorizontalOptions = LayoutOptions.Center },
                    new Border
                    {
                        StrokeShape = new RoundRectangle { CornerRadius = 12 },
                        Stroke = ThemeColors.Divider, StrokeThickness = 1,
                        Padding = new Thickness(12, 4), Content = _passwordEntry
                    },
                    _errorLabel,
                    unlockBtn,
                    signOutBtn,
                }
            }
        };

        var grid = new Grid
        {
            Padding = 24,
            RowDefinitions =
            {
                new RowDefinition { Height = GridLength.Star },
                new RowDefinition { Height = GridLength.Auto },
                new RowDefinition { Height = GridLength.Star },
            }
        };
        grid.Children.Add(_greetingLabel);
        grid.Children.Add(card);
        Grid.SetRow(_greetingLabel, 0);
        Grid.SetRow(card, 1);
        Content = grid;

        Loaded += (_, _) => _passwordEntry.Focus();
    }

    private async Task LoadProfileAsync()
    {
        var profile = await AccountStore.GetProfileAsync();
        var name = profile?.Username ?? "Добро пожаловать";
        _greetingLabel.Text = $"👋 {name}";
    }

    private async void OnUnlock(object? sender, EventArgs e)
    {
        var (success, error) = await AccountStore.TrySignInAsync(
                AccountStore.GetCurrentUsernameKey(),
                _passwordEntry.Text ?? string.Empty);
        if (!success)
        {
            _errorLabel.Text      = error;
            _errorLabel.IsVisible = true;
            return;
        }

        App.ResetRootPage();
    }
}
