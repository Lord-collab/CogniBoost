using CogniBoost.Models;

namespace CogniBoost.Services;

/// <summary>
/// Показывает анимированный попап нового достижения.
/// Карточка влетает сверху с затемнением фона. Авто-закрытие через 2.5 с
/// или по тапу. Несколько достижений показываются последовательно.
/// </summary>
public static class AchievementPopupService
{
    public static async Task ShowAsync(ContentPage page, IReadOnlyList<Achievement> achievements)
    {
        if (achievements is null || achievements.Count == 0)
            return;

        // Сохраняем текущий контент
        var originalContent = page.Content;
        var root = new Grid { Children = { originalContent } };

        foreach (var ach in achievements)
        {
            var overlay = BuildOverlay(ach);
            root.Children.Add(overlay);
            page.Content = root;

            // Slide card down + fade overlay
            overlay.Opacity = 0;
            var card = (View)overlay.Children[0];
            card.TranslationY = -200;
            card.Opacity = 0;

            await Task.WhenAll(
                overlay.FadeToAsync(1, 250, Easing.CubicOut),
                card.TranslateToAsync(0, 0, 400, Easing.CubicOut),
                card.FadeToAsync(1, 300, Easing.CubicOut));

            // Звук
            SoundService.PlayComplete();

            // Ждём тапа или 2.5 секунды
            var tcs = new TaskCompletionSource();
            TapGestureRecognizer tap = null!;
            tap = new TapGestureRecognizer();
            tap.Tapped += (_, _) =>
            {
                tap.Tapped -= null;
                tcs.TrySetResult();
            };
            overlay.GestureRecognizers.Add(tap);

            // Auto-dismiss через 2.5 с
            var delay = Task.Delay(2500);
            await Task.WhenAny(delay, tcs.Task);

            // Slide out
            await Task.WhenAll(
                overlay.FadeToAsync(0, 200, Easing.CubicIn),
                card.TranslateToAsync(0, -200, 250, Easing.CubicIn));

            root.Children.Remove(overlay);
            overlay.GestureRecognizers.Clear();
        }

        page.Content = originalContent;
    }

    private static Grid BuildOverlay(Achievement ach)
    {
        var card = new Border
        {
            StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 24 },
            Stroke = Colors.Transparent,
            BackgroundColor = Color.FromArgb("#1E1E2E"),
            Padding = new Thickness(28, 24),
            WidthRequest = 300,
            HorizontalOptions = LayoutOptions.Center,
            VerticalOptions = LayoutOptions.Center,
            Shadow = new Shadow
            {
                Brush = new SolidColorBrush(Colors.Black),
                Offset = new Point(0, 8),
                Radius = 24,
                Opacity = 0.4f
            },
            Content = new VerticalStackLayout
            {
                Spacing = 8,
                HorizontalOptions = LayoutOptions.Center,
                Children =
                {
                    new Label
                    {
                        Text = ach.Emoji,
                        FontSize = 48,
                        HorizontalOptions = LayoutOptions.Center
                    },
                    new Label
                    {
                        Text = "НОВОЕ ДОСТИЖЕНИЕ!",
                        FontSize = 11,
                        FontAttributes = FontAttributes.Bold,
                        TextColor = Color.FromArgb("#FBBF24"),
                        HorizontalOptions = LayoutOptions.Center,
                        CharacterSpacing = 2
                    },
                    new Label
                    {
                        Text = ach.Title,
                        FontSize = 20,
                        FontAttributes = FontAttributes.Bold,
                        TextColor = Colors.White,
                        HorizontalOptions = LayoutOptions.Center,
                        HorizontalTextAlignment = TextAlignment.Center
                    },
                    new Label
                    {
                        Text = ach.Description,
                        FontSize = 13,
                        TextColor = Color.FromArgb("#A0A0B0"),
                        HorizontalOptions = LayoutOptions.Center,
                        HorizontalTextAlignment = TextAlignment.Center
                    }
                }
            }
        };

        var backdrop = new BoxView
        {
            Color = Colors.Black.WithAlpha(0.55f),
            InputTransparent = false
        };

        return new Grid
        {
            Children = { backdrop, card }
        };
    }
}
