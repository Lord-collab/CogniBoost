using CogniBoost.Models;
using CogniBoost.Pages.Controls;
using CogniBoost.Services;
using Microsoft.Maui.Controls.Shapes;

namespace CogniBoost.Pages;

public partial class GamesPage : ContentPage
{
    private BrainSkill? _activeCategory;
    private string _searchText = "";

    public GamesPage()
    {
        InitializeComponent();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        BuildCategories();
        BuildCatalog();
        SettingsService.ApplyTextScale(this);
    }

    private void BuildCategories()
    {
        CategoryChips.Children.Clear();

        // "Все" — первый чип
        CategoryChips.Children.Add(MakeCategoryChip("Все", null, _activeCategory == null));

        foreach (var meta in BrainSkillInfo.All)
        {
            var isActive = _activeCategory == meta.Skill;
            CategoryChips.Children.Add(MakeCategoryChip($"{meta.Emoji} {meta.Title}", meta.Skill, isActive));
        }
    }

    private View MakeCategoryChip(string text, BrainSkill? skill, bool isActive)
    {
        var color = skill.HasValue ? ThemeColors.SkillColor(skill.Value) : ThemeColors.Accent;
        var bg = isActive ? color : Colors.Transparent;
        var textColor = isActive ? Colors.White : color;
        var borderColor = isActive ? Colors.Transparent : ThemeColors.Border;

        var chip = new Border
        {
            StrokeShape = new RoundRectangle { CornerRadius = 20 },
            Stroke = borderColor,
            BackgroundColor = bg,
            Padding = new Thickness(16, 8),
            Content = new Label
            {
                Text = text,
                FontSize = 13,
                FontAttributes = FontAttributes.Bold,
                TextColor = textColor,
                VerticalOptions = LayoutOptions.Center
            }
        };

        var tap = new TapGestureRecognizer();
        tap.Tapped += (_, _) =>
        {
            _activeCategory = skill;
            BuildCategories();
            BuildCatalog();
        };
        chip.GestureRecognizers.Add(tap);

        return chip;
    }

    private void OnSearchTextChanged(object? sender, TextChangedEventArgs e)
    {
        _searchText = e.NewTextValue?.ToLower() ?? "";
        BuildCatalog();
    }

    private void BuildCatalog()
    {
        GamesList.Children.Clear();

        var allGames = GameCatalog.All
            .Where(g => UnlockService.IsUnlocked(g))
            .ToList();

        IEnumerable<GameDefinition> filtered = allGames;

        // Фильтр по категории
        if (_activeCategory.HasValue)
            filtered = filtered.Where(g => g.Skill == _activeCategory.Value);

        // Фильтр по поиску
        if (!string.IsNullOrWhiteSpace(_searchText))
        {
            filtered = filtered.Where(g =>
                g.Title.ToLower().Contains(_searchText) ||
                g.Description.ToLower().Contains(_searchText));
        }

        var games = filtered.ToList();

        if (games.Count == 0)
        {
            GamesList.Children.Add(new EmptyState(
                "🔍", "Ничего не найдено",
                "Попробуй другой фильтр или поиск.",
                ""));
            return;
        }

        // Группируем по навыку (если не выбран конкретный)
        if (!_activeCategory.HasValue && string.IsNullOrWhiteSpace(_searchText))
        {
            foreach (var meta in BrainSkillInfo.All)
            {
                var skillGames = games.Where(g => g.Skill == meta.Skill).ToList();
                if (skillGames.Count == 0) continue;

                // Заголовок категории
                GamesList.Children.Add(new Label
                {
                    Text = $"{meta.Emoji}  {meta.Title}",
                    FontSize = 16,
                    FontAttributes = FontAttributes.Bold,
                    TextColor = ThemeColors.SkillColor(meta.Skill),
                    Margin = new Thickness(0, 8, 0, 4)
                });

                // Карточки игр
                foreach (var game in skillGames)
                    GamesList.Children.Add(BuildGameCard(game));
            }
        }
        else
        {
            // Просто список
            foreach (var game in games)
                GamesList.Children.Add(BuildGameCard(game));
        }
    }

    private View BuildGameCard(GameDefinition game)
    {
        var unlocked = UnlockService.IsUnlocked(game);
        var accent = BrainSkillInfo.Accent(game.Skill);
        var best = ProgressStore.GetBestScore(game.Id);

        var isChallenge = DailyChallengeService.IsChallengeGame(game.Id);

        var emoji = new Label
        {
            Text = game.Emoji,
            FontSize = 30,
            VerticalOptions = LayoutOptions.Center,
            WidthRequest = 48,
            HorizontalTextAlignment = TextAlignment.Center
        };

        var challengeBadge = isChallenge ? new Label
        {
            Text = "🔥x2",
            FontSize = 9,
            FontAttributes = FontAttributes.Bold,
            TextColor = Colors.White,
            BackgroundColor = ThemeColors.Success,
            Padding = new Thickness(4, 1),
            HorizontalTextAlignment = TextAlignment.Center,
            VerticalOptions = LayoutOptions.Start,
            HorizontalOptions = LayoutOptions.Start
        } : null;

        var titleLabel = new Label
        {
            Text = game.Title,
            FontSize = 15,
            FontAttributes = FontAttributes.Bold,
            TextColor = ThemeColors.TextPrimary
        };

        var descLabel = new Label
        {
            Text = game.Description,
            FontSize = 11,
            TextColor = ThemeColors.TextMuted,
            LineBreakMode = LineBreakMode.TailTruncation,
            MaxLines = 1
        };

        var metaLabel = new Label
        {
            Text = unlocked
                ? (best > 0 ? $"🏆 {best}" : "Ещё не играл")
                : $"🔒 {game.UnlockCost} ⭐",
            FontSize = 11,
            TextColor = unlocked ? accent : ThemeColors.TextMuted
        };

        var info = new VerticalStackLayout
        {
            Spacing = 1,
            VerticalOptions = LayoutOptions.Center,
            Children = { titleLabel, descLabel, metaLabel }
        };

        var emojiContainer = new Grid { WidthRequest = 48 };
        emojiContainer.Children.Add(emoji);
        if (challengeBadge is not null)
        {
            challengeBadge.Margin = new Thickness(2);
            emojiContainer.Children.Add(challengeBadge);
        }

        var playBtn = new Button
        {
            Text = unlocked ? "Играть" : $"🔥 {game.UnlockCost}",
            BackgroundColor = accent,
            TextColor = Colors.White,
            FontAttributes = FontAttributes.Bold,
            FontSize = 13,
            HeightRequest = 36,
            CornerRadius = 10,
            Padding = new Thickness(16, 0)
        };
        playBtn.Clicked += async (_, _) => await OnGameTapped(game);

        var tutorialBtn = new Button
        {
            Text = "Как играть",
            BackgroundColor = Colors.Transparent,
            TextColor = accent,
            FontAttributes = FontAttributes.Bold,
            FontSize = 13,
            HeightRequest = 36,
            CornerRadius = 10,
            BorderColor = accent,
            BorderWidth = 1,
            Padding = new Thickness(16, 0)
        };
        tutorialBtn.Clicked += async (_, _) =>
        {
            if (!unlocked)
            {
                var go = await DisplayAlertAsync(
                    "Игра закрыта",
                    $"«{game.Title}» открывается за {game.UnlockCost} бонусов. Перейти в магазин?",
                    "В магазин", "Отмена");
                if (go)
                    await Shell.Current.GoToAsync("store");
                return;
            }
            await GameTutorialService.ShowManualAsync(this, game.Id);
        };

        var buttonsRow = new HorizontalStackLayout
        {
            Spacing = 10,
            HorizontalOptions = LayoutOptions.End,
            Children = { playBtn, tutorialBtn }
        };

        var topRow = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition { Width = GridLength.Auto },
                new ColumnDefinition { Width = GridLength.Star }
            },
            ColumnSpacing = 12,
            Children = { emojiContainer, info }
        };
        Grid.SetColumn(info, 1);

        var card = new Border
        {
            StrokeShape = new RoundRectangle { CornerRadius = 14 },
            Stroke = Colors.Transparent,
            BackgroundColor = ThemeColors.CardBg,
            Padding = new Thickness(14),
            Opacity = unlocked ? 1.0 : 0.6,
            Content = new VerticalStackLayout
            {
                Spacing = 10,
                Children = { topRow, buttonsRow }
            },
            Shadow = new Shadow
            {
                Brush = new SolidColorBrush(Colors.Black),
                Offset = new Point(0, 1),
                Radius = 6,
                Opacity = 0.05f
            }
        };

        return card;
    }

    private async Task OnGameTapped(GameDefinition game)
    {
        if (!UnlockService.IsUnlocked(game))
        {
            var go = await DisplayAlertAsync(
                "Игра закрыта",
                $"«{game.Title}» открывается за {game.UnlockCost} бонусов. Перейти в магазин?",
                "В магазин", "Отмена");
            if (go)
                await Shell.Current.GoToAsync("store");
            return;
        }

        await Navigation.PushAsync(game.CreatePage());
    }
}