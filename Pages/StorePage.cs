using CogniBoost.Models;
using CogniBoost.Services;
using Microsoft.Maui.Controls.Shapes;

namespace CogniBoost.Pages;

public sealed class StorePage : ContentPage
{
    private readonly Label _balanceLabel = new();
    private readonly VerticalStackLayout _list = new() { Spacing = 12 };

    public StorePage()
    {
        Title = "Магазин";
        BackgroundColor = ThemeColors.PageBg;
        BuildUi();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _ = RefreshAsync();
    }

    private void BuildUi()
    {
        _balanceLabel.FontSize = 32;
        _balanceLabel.FontAttributes = FontAttributes.Bold;
        _balanceLabel.TextColor = Colors.White;

        var balanceCard = new Border
        {
            StrokeShape = new RoundRectangle { CornerRadius = 20 },
            Stroke = Colors.Transparent,
            BackgroundColor = ThemeColors.Warning,
            Padding = 20,
            Content = new VerticalStackLayout
            {
                Spacing = 4,
                Children =
                {
                    new Label
                    {
                        Text = "Баланс бонусов", FontSize = 14,
                        TextColor = ThemeColors.WarningLight
                    },
                    _balanceLabel
                }
            }
        };

        Content = new ScrollView
        {
            Content = new VerticalStackLayout
            {
                Padding = 20,
                Spacing = 18,
                Children =
                {
                    new Label
                    {
                        Text = "Магазин игр", FontSize = 28, FontAttributes = FontAttributes.Bold,
                        TextColor = ThemeColors.TextPrimary
                    },
                    balanceCard,
                    new Label
                    {
                        Text = "Открывай новые игры за бонусы, заработанные в играх и тестах.",
                        FontSize = 13, TextColor = ThemeColors.TextMuted
                    },
                    _list
                }
            }
        };
    }

    private async Task RefreshAsync()
    {
        _balanceLabel.Text = $"{await PointsService.GetBalanceAsync()} ⭐";

        _list.Children.Clear();
        var lockedGames = GameCatalog.All.Where(g => !g.Starter).ToList();

        if (lockedGames.All(UnlockService.IsUnlocked))
        {
            _list.Children.Add(new Label
            {
                Text = "Все игры открыты! 🎉",
                FontSize = 16,
                TextColor = ThemeColors.Success,
                HorizontalOptions = LayoutOptions.Center,
                Margin = new Thickness(0, 20)
            });
            return;
        }

        foreach (var game in lockedGames)
        {
            _list.Children.Add(BuildStoreCard(game));
        }
    }

    private View BuildStoreCard(GameDefinition game)
    {
        var unlocked = UnlockService.IsUnlocked(game);
        var accent = BrainSkillInfo.Accent(game.Skill);

        var emoji = new Label
        {
            Text = game.Emoji, FontSize = 32,
            VerticalOptions = LayoutOptions.Center
        };

        var info = new VerticalStackLayout
        {
            Spacing = 3,
            VerticalOptions = LayoutOptions.Center,
            Children =
            {
                new Label
                {
                    Text = game.Title, FontSize = 17,
                    FontAttributes = FontAttributes.Bold,
                    TextColor = ThemeColors.TextPrimary
                },
                new Label
                {
                    Text = $"{BrainSkillInfo.Title(game.Skill)} · {game.Description}",
                    FontSize = 12, TextColor = ThemeColors.TextSecondary
                }
            }
        };

        var actionButton = new Button
        {
            Text = unlocked ? "Открыто" : $"{game.UnlockCost} ⭐",
            BackgroundColor = unlocked ? ThemeColors.Success : accent,
            TextColor = Colors.White,
            FontAttributes = FontAttributes.Bold,
            CornerRadius = 12,
            HeightRequest = 40,
            WidthRequest = 110,
            IsEnabled = !unlocked
        };
        if (!unlocked)
            actionButton.Clicked += async (_, _) => await OnBuy(game);

        var grid = new Grid
        {
            ColumnSpacing = 12,
            ColumnDefinitions =
            {
                new ColumnDefinition { Width = GridLength.Auto },
                new ColumnDefinition { Width = GridLength.Star },
                new ColumnDefinition { Width = GridLength.Auto }
            },
            Children = { emoji, info }
        };
        Grid.SetColumn(info, 1);
        Grid.SetColumn(actionButton, 2);
        grid.Children.Add(actionButton);

        return new Border
        {
            StrokeShape = new RoundRectangle { CornerRadius = 14 },
            Stroke = Colors.Transparent,
            BackgroundColor = ThemeColors.CardBg,
            Padding = 14,
            Content = grid
        };
    }

    private async Task OnBuy(GameDefinition game)
    {
        var confirm = await DisplayAlertAsync(
            "Открыть игру",
            $"Открыть «{game.Title}» за {game.UnlockCost} бонусов?",
            "Открыть", "Отмена");
        if (!confirm) return;

        var (success, message) = await UnlockService.TryUnlockAsync(game);
        if (success)
        {
            await DisplayAlertAsync("Готово", message, "Ок");
            await RefreshAsync();
        }
        else
        {
            await DisplayAlertAsync("Недостаточно бонусов", message, "Ок");
        }
    }
}
