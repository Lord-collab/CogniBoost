using CogniBoost.Pages;

namespace CogniBoost.Services;

public static class GameTutorialService
{
    private const string SeenPrefix = "cb_tutorial_";

    public static bool HasSeenTutorial(string gameId)
    {
        var key = $"{SeenPrefix}{UserKey()}_{gameId}";
        return Preferences.Default.Get(key, false);
    }

    public static void MarkTutorialSeen(string gameId)
    {
        var key = $"{SeenPrefix}{UserKey()}_{gameId}";
        Preferences.Default.Set(key, true);
    }

    public static async Task ShowIfNeededAsync(ContentPage page, string gameId)
    {
        if (HasSeenTutorial(gameId))
            return;

        var text = GameCatalog.GetTutorial(gameId);
        if (string.IsNullOrWhiteSpace(text))
        {
            MarkTutorialSeen(gameId);
            return;
        }

        var def = GameCatalog.Get(gameId);
        var title = def?.Title ?? gameId;
        var emoji = def?.Emoji ?? "🎮";

        await ShowTutorialAsync(page, gameId, title, emoji, text);
    }

    public static async Task ShowManualAsync(ContentPage page, string gameId)
    {
        var text = GameCatalog.GetTutorial(gameId);
        if (string.IsNullOrWhiteSpace(text))
        {
            await page.DisplayAlertAsync("Обучение", "Для этой игры нет обучения.", "OK");
            return;
        }

        var def = GameCatalog.Get(gameId);
        var title = def?.Title ?? gameId;
        var emoji = def?.Emoji ?? "🎮";

        await ShowTutorialAsync(page, gameId, title, emoji, text);
    }

    private static async Task ShowTutorialAsync(
        ContentPage page, string gameId, string title, string emoji, string text)
    {
        var wrapper = new Grid();
        var originalContent = page.Content;
        wrapper.Children.Add(originalContent);
        page.Content = wrapper;

        var backdrop = new BoxView { Color = Colors.Black.WithAlpha(0.6f) };

        var button = new Button
        {
            Text = "Понятно!",
            BackgroundColor = Color.FromArgb("#6C63FF"),
            TextColor = Colors.White,
            FontAttributes = FontAttributes.Bold,
            HeightRequest = 48,
            CornerRadius = 14,
            Margin = new Thickness(0, 8, 0, 0)
        };

        var card = new Border
        {
            StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 24 },
            Stroke = Colors.Transparent,
            BackgroundColor = Color.FromArgb("#1E1E2E"),
            Padding = new Thickness(28, 24),
            Margin = new Thickness(32, 0),
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
                Spacing = 14,
                HorizontalOptions = LayoutOptions.Center,
                Children =
                {
                    new Label { Text = emoji, FontSize = 48, HorizontalOptions = LayoutOptions.Center },
                    new Label
                    {
                        Text = title, FontSize = 22,
                        FontAttributes = FontAttributes.Bold,
                        TextColor = Colors.White,
                        HorizontalOptions = LayoutOptions.Center
                    },
                    new Label
                    {
                        Text = text, FontSize = 14,
                        TextColor = Color.FromArgb("#C0C0D0"),
                        HorizontalOptions = LayoutOptions.Center,
                        HorizontalTextAlignment = TextAlignment.Center,
                        LineBreakMode = LineBreakMode.WordWrap
                    },
                    button
                }
            }
        };

        var overlay = new Grid { Children = { backdrop, card } };
        wrapper.Children.Add(overlay);

        overlay.Opacity = 0;
        card.TranslationY = -200;
        card.Opacity = 0;

        await Task.WhenAll(
            overlay.FadeToAsync(1, 250, Easing.CubicOut),
            card.TranslateToAsync(0, 0, 400, Easing.CubicOut),
            card.FadeToAsync(1, 300, Easing.CubicOut));

        var tcs = new TaskCompletionSource();
        button.Clicked += (_, _) => tcs.TrySetResult();
        backdrop.GestureRecognizers.Add(new TapGestureRecognizer());
        var tap = backdrop.GestureRecognizers[0] as TapGestureRecognizer;
        if (tap is not null)
            tap.Tapped += (_, _) => tcs.TrySetResult();

        await tcs.Task;

        await Task.WhenAll(
            overlay.FadeToAsync(0, 200, Easing.CubicIn),
            card.TranslateToAsync(0, -200, 250, Easing.CubicIn));

        wrapper.Children.Remove(overlay);

        MarkTutorialSeen(gameId);
    }

    private static string UserKey()
    {
        var k = AccountStore.GetCurrentUsernameKey();
        return string.IsNullOrWhiteSpace(k) ? "guest" : k;
    }
}
