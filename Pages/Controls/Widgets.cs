using Microsoft.Maui.Controls.Shapes;

namespace CogniBoost.Pages.Controls;

public sealed class StatCard : Border
{
    public StatCard(string emoji, string value, string label, Color? accentColor = null)
    {
        var accent = accentColor ?? ThemeColors.Accent;

        StrokeShape = new RoundRectangle { CornerRadius = 16 };
        Stroke = Colors.Transparent;
        BackgroundColor = ThemeColors.CardBg;
        Padding = new Thickness(16, 14);
        Shadow = new Shadow { Brush = new SolidColorBrush(Color.FromArgb("#000000")), Offset = new Point(0, 2), Radius = 8, Opacity = 0.06f };

        var emojiLabel = new Label
        {
            Text = emoji,
            FontSize = 24,
            VerticalOptions = LayoutOptions.Center
        };

        var valueLabel = new Label
        {
            Text = value,
            FontSize = 22,
            FontAttributes = FontAttributes.Bold,
            TextColor = ThemeColors.TextPrimary,
            VerticalOptions = LayoutOptions.Center
        };

        var labelLabel = new Label
        {
            Text = label,
            FontSize = 12,
            TextColor = ThemeColors.TextMuted
        };

        var stack = new VerticalStackLayout
        {
            Spacing = 2,
            VerticalOptions = LayoutOptions.Center,
            Children = { emojiLabel, valueLabel, labelLabel }
        };

        Content = new HorizontalStackLayout
        {
            Spacing = 12,
            Children = { emojiLabel, stack }
        };
    }
}

public sealed class GameCard : Border
{
    public GameCard(string emoji, string title, string subtitle, string? bestScore, Color accent, Action onPlay)
    {
        StrokeShape = new RoundRectangle { CornerRadius = 14 };
        Stroke = Colors.Transparent;
        BackgroundColor = ThemeColors.CardBg;
        Padding = new Thickness(14);
        Shadow = new Shadow { Brush = new SolidColorBrush(Colors.Black), Offset = new Point(0, 1), Radius = 6, Opacity = 0.05f };

        var emojiLabel = new Label
        {
            Text = emoji,
            FontSize = 28,
            VerticalOptions = LayoutOptions.Center,
            WidthRequest = 44,
            HorizontalTextAlignment = TextAlignment.Center
        };

        var titleLabel = new Label
        {
            Text = title,
            FontSize = 15,
            FontAttributes = FontAttributes.Bold,
            TextColor = ThemeColors.TextPrimary
        };

        var subLabel = new Label
        {
            Text = subtitle,
            FontSize = 11,
            TextColor = accent
        };

        var infoStack = new VerticalStackLayout
        {
            Spacing = 1,
            VerticalOptions = LayoutOptions.Center,
            Children = { titleLabel, subLabel }
        };

        var rightContent = new HorizontalStackLayout
        {
            Spacing = 8,
            VerticalOptions = LayoutOptions.Center
        };

        if (!string.IsNullOrEmpty(bestScore))
        {
            rightContent.Children.Add(new Label
            {
                Text = bestScore,
                FontSize = 12,
                TextColor = ThemeColors.TextMuted,
                VerticalOptions = LayoutOptions.Center
            });
        }

        var playButton = new Label
        {
            Text = "▶",
            FontSize = 14,
            TextColor = accent,
            VerticalOptions = LayoutOptions.Center
        };
        rightContent.Children.Add(playButton);

        var grid = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition { Width = GridLength.Auto },
                new ColumnDefinition { Width = GridLength.Star },
                new ColumnDefinition { Width = GridLength.Auto }
            },
            ColumnSpacing = 12,
            Children = { emojiLabel, infoStack, rightContent }
        };
        Grid.SetColumn(infoStack, 1);
        Grid.SetColumn(rightContent, 2);

        Content = grid;

        var tap = new TapGestureRecognizer();
        tap.Tapped += (_, _) => onPlay();
        GestureRecognizers.Add(tap);
    }
}

public sealed class StreakBadge : Border
{
    public StreakBadge(int streak, int bonus = 0)
    {
        StrokeShape = new RoundRectangle { CornerRadius = 14 };
        Stroke = Colors.Transparent;
        BackgroundColor = ThemeColors.Warning;
        Padding = new Thickness(16, 12);

        var fire = new Label
        {
            Text = "🔥",
            FontSize = 28,
            VerticalOptions = LayoutOptions.Center
        };

        var streakText = streak == 0 ? "Начни сегодня" : $"Серия {streak} дн.";
        var streakLabel = new Label
        {
            Text = streakText,
            FontSize = 15,
            FontAttributes = FontAttributes.Bold,
            TextColor = Colors.White,
            VerticalOptions = LayoutOptions.Center
        };

        var bonusLabel = new Label();
        if (bonus > 0)
        {
            bonusLabel.Text = $"+{bonus} ⭐/день";
            bonusLabel.FontSize = 12;
            bonusLabel.TextColor = ThemeColors.WarningLight;
            bonusLabel.VerticalOptions = LayoutOptions.Center;
        }

        var stack = new VerticalStackLayout
        {
            Spacing = 1,
            VerticalOptions = LayoutOptions.Center,
            Children = { streakLabel, bonusLabel }
        };

        var grid = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition { Width = GridLength.Auto },
                new ColumnDefinition { Width = GridLength.Star }
            },
            ColumnSpacing = 12,
            Children = { fire, stack }
        };
        Grid.SetColumn(stack, 1);

        Content = grid;
    }
}

public sealed class EmptyState : VerticalStackLayout
{
    public EmptyState(string emoji, string title, string subtitle, string buttonText, Action? onButton = null)
    {
        Spacing = 12;
        HorizontalOptions = LayoutOptions.Center;
        VerticalOptions = LayoutOptions.Center;
        Padding = new Thickness(24);

        Children.Add(new Label
        {
            Text = emoji,
            FontSize = 56,
            HorizontalOptions = LayoutOptions.Center
        });

        Children.Add(new Label
        {
            Text = title,
            FontSize = 18,
            FontAttributes = FontAttributes.Bold,
            TextColor = ThemeColors.TextPrimary,
            HorizontalTextAlignment = TextAlignment.Center,
            HorizontalOptions = LayoutOptions.Center
        });

        Children.Add(new Label
        {
            Text = subtitle,
            FontSize = 13,
            TextColor = ThemeColors.TextMuted,
            HorizontalTextAlignment = TextAlignment.Center,
            HorizontalOptions = LayoutOptions.Center
        });

        if (!string.IsNullOrEmpty(buttonText) && onButton != null)
        {
            var button = new Button
            {
                Text = buttonText,
                BackgroundColor = ThemeColors.Accent,
                TextColor = Colors.White,
                FontAttributes = FontAttributes.Bold,
                CornerRadius = 12,
                HeightRequest = 48,
                Margin = new Thickness(0, 8, 0, 0)
            };
            button.Clicked += (_, _) => onButton();
            Children.Add(button);
        }
    }
}