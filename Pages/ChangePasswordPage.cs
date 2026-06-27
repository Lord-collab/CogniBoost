using CogniBoost.Services;
using Microsoft.Maui.Controls.Shapes;

namespace CogniBoost.Pages;

public sealed class ChangePasswordPage : ContentPage
{
    private readonly Entry _oldEntry  = new() { Placeholder = "Текущий пароль",  IsPassword = true };
    private readonly Entry _newEntry  = new() { Placeholder = "Новый пароль",     IsPassword = true };
    private readonly Entry _confEntry = new() { Placeholder = "Повторите пароль", IsPassword = true };
    private readonly Label _errorLabel = new();

    public ChangePasswordPage()
    {
        Title           = "Смена пароля";
        BackgroundColor = ThemeColors.PageBg;

        _errorLabel.TextColor  = ThemeColors.Error;
        _errorLabel.FontSize   = 13;
        _errorLabel.IsVisible  = false;

        var saveBtn = new Button
        {
            Text = "Изменить пароль", BackgroundColor = ThemeColors.Accent,
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
                        FontAttributes = FontAttributes.Bold, TextColor = ThemeColors.TextPrimary },
                    new Label { Text = "Пароль должен быть не короче 6 символов и содержать цифру.",
                        FontSize = 13, TextColor = ThemeColors.TextMuted },
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

    private async void OnSave(object? sender, EventArgs e)
    {
        var (success, error) = await AccountStore.TryChangePasswordAsync(
                _oldEntry.Text ?? string.Empty,
                _newEntry.Text ?? string.Empty,
                _confEntry.Text ?? string.Empty);
        if (!success)
        {
            _errorLabel.Text      = error;
            _errorLabel.IsVisible = true;
            return;
        }

        await DisplayAlertAsync("Готово", "Пароль успешно изменён.", "OK");
        await Navigation.PopAsync();
    }

    private static Border BuildCard(View content)
        => new()
        {
            StrokeShape = new RoundRectangle { CornerRadius = 18 },
            Stroke = Colors.Transparent, BackgroundColor = ThemeColors.CardBg,
            Padding = new Thickness(16), Content = content
        };

    private static Border WrapEntry(Entry entry)
    {
        entry.BackgroundColor = ThemeColors.CardBg;
        entry.TextColor = ThemeColors.TextPrimary;
        entry.PlaceholderColor = ThemeColors.TextMuted;
        return new Border
        {
            StrokeShape = new RoundRectangle { CornerRadius = 12 },
            Stroke = ThemeColors.Divider, StrokeThickness = 1,
            Padding = new Thickness(12, 4), Content = entry
        };
    }
}
