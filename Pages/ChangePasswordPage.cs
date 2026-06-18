using CogniBoost.Services;
using Microsoft.Maui.Controls.Shapes;

namespace CogniBoost.Pages;

public sealed class ChangePasswordPage : ContentPage
{
    private readonly Entry _oldEntry  = new() { Placeholder = "Текущий пароль",  IsPassword = true };
    private readonly Entry _newEntry  = new() { Placeholder = "Новый пароль",     IsPassword = true };
    private readonly Entry _confEntry = new() { Placeholder = "Повторите пароль", IsPassword = true };
    private readonly Label _errorLabel = new();

    private static bool IsDark => Application.Current?.RequestedTheme == AppTheme.Dark;
    private static Color Card  => Color.FromArgb(IsDark ? "#13142A" : "#FFFFFF");
    private static Color Txt   => Color.FromArgb(IsDark ? "#E8E8FF" : "#0D0D2B");

    public ChangePasswordPage()
    {
        Title           = "Смена пароля";
        BackgroundColor = Color.FromArgb(IsDark ? "#0A0B18" : "#F0F1FA");

        _errorLabel.TextColor  = Color.FromArgb("#FF5370");
        _errorLabel.FontSize   = 13;
        _errorLabel.IsVisible  = false;

        var saveBtn = new Button
        {
            Text = "Изменить пароль", BackgroundColor = Color.FromArgb("#6C63FF"),
            TextColor = Colors.White, FontAttributes = FontAttributes.Bold,
            HeightRequest = 52, CornerRadius = 14
        };
        saveBtn.Clicked += OnSave;

        Content = new ScrollView
        {
            Content = new VerticalStackLayout
            {
                Padding = new Thickness(24, 20), Spacing = 16,
                Children =
                {
                    new Label { Text = "Смена пароля", FontSize = 26,
                        FontAttributes = FontAttributes.Bold, TextColor = Txt },
                    new Label { Text = "Пароль должен быть не короче 6 символов и содержать цифру.",
                        FontSize = 13, TextColor = Color.FromArgb("#7B7BA8") },
                    BuildCard(new VerticalStackLayout
                    {
                        Spacing = 12,
                        Children =
                        {
                            WrapEntry(_oldEntry),
                            WrapEntry(_newEntry),
                            WrapEntry(_confEntry),
                        }
                    }),
                    _errorLabel,
                    saveBtn,
                }
            }
        };
    }

    private void OnSave(object? sender, EventArgs e)
    {
        if (!AccountStore.TryChangePassword(
                _oldEntry.Text ?? string.Empty,
                _newEntry.Text ?? string.Empty,
                _confEntry.Text ?? string.Empty,
                out var error))
        {
            _errorLabel.Text      = error;
            _errorLabel.IsVisible = true;
            return;
        }

        _ = DisplayAlertAsync("Готово", "Пароль успешно изменён.", "OK")
            .ContinueWith(_ => MainThread.BeginInvokeOnMainThread(() => Navigation.PopAsync()));
    }

    private static Border BuildCard(View content)
        => new()
        {
            StrokeShape = new RoundRectangle { CornerRadius = 18 },
            Stroke = Colors.Transparent, BackgroundColor = Card,
            Padding = new Thickness(16), Content = content
        };

    private static Border WrapEntry(Entry entry)
        => new()
        {
            StrokeShape = new RoundRectangle { CornerRadius = 12 },
            Stroke = Color.FromArgb("#E4E5F5"), StrokeThickness = 1,
            Padding = new Thickness(12, 4), Content = entry
        };
}
