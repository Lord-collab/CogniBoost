using CogniBoost.Models;
using CogniBoost.Pages.Controls;
using CogniBoost.Services;
using Microsoft.Maui.Controls.Shapes;
using Microsoft.Maui.Graphics;

namespace CogniBoost.Pages;

/// <summary>
/// Страница статистики и прогресса.
///
/// Компоновка:
///   — Верх: общий балл (кольцо 0–1000), ранг, кол-во игр, баллы.
///   — График точности последних 14 игр (линейный).
///   — Навыки: 5 карточек с прогресс-баром и трендом за 7 дней.
///   — История: список последних результатов (игр или тестов).
///
/// Все данные читаются через ProgressStore, который вычисляет баллы
/// на основе лучших результатов по каждой игре (см. GetSkillScoreAsync).
/// </summary>
public partial class StatsPage : ContentPage
{
    private bool _showGames = true;

    public StatsPage()
    {
        InitializeComponent();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _ = RefreshAsync();
    }

    private async Task RefreshAsync()
    {
        var overall = await ProgressStore.GetOverallScoreAsync();
        OverallLabel.Text = overall.ToString();
        GamesPlayedLabel.Text = (await ProgressStore.GetGamesPlayedCountAsync()).ToString();
        PointsLabel.Text = (await PointsService.GetBalanceAsync()).ToString();
        RankLabel.Text = GetRank(overall);

        BuildRing(overall);
        BuildChart();
        BuildSkills();
        BuildHistory();
    }

    private static string GetRank(int score) => score switch
    {
        >= 900 => "Гений",
        >= 700 => "Мастер",
        >= 500 => "Эксперт",
        >= 300 => "Продвинутый",
        >= 150 => "Ученик",
        _ => "Новичок"
    };

    private void BuildRing(int overall)
    {
        var progress = Math.Clamp(overall / 1000.0, 0, 1);
        ProgressRingView.Drawable = new ProgressRingDrawable
        {
            Progress = (float)progress,
            ProgressColor = ThemeColors.Accent,
            TrackColor = ThemeColors.Border,
            Thickness = 10,
            CenterText = $"{overall}",
            CenterTextColor = ThemeColors.TextPrimary
        };
        ProgressRingView.Invalidate();
    }

    // ── График ───────────────────────────────────────────────────────
    private async void BuildChart()
    {
        var history = (await ProgressStore.GetGameHistoryAsync())
            .Take(14)
            .Reverse()
            .Select(r => (double)r.AccuracyPercent)
            .ToList();

        if (history.Count < 2)
        {
            ChartView.Drawable = new EmptyChartDrawable(ThemeColors.TextMuted);
            return;
        }

        ChartView.Drawable = new LineChartDrawable(history, ThemeColors.Accent,
            ThemeColors.Divider, ThemeColors.CardBg2, ThemeColors.TextMuted);
    }

    // ── Навыки ───────────────────────────────────────────────────────
    private async void BuildSkills()
    {
        SkillsLayout.Children.Clear();

        foreach (var meta in BrainSkillInfo.All)
        {
            var score = await ProgressStore.GetSkillScoreAsync(meta.Skill);
            var accent = ThemeColors.SkillColor(meta.Skill);
            var lightBg = ThemeColors.SkillColorLight(meta.Skill);
            var fraction = Math.Clamp(score / 1000.0, 0, 1);

            var recent = (await ProgressStore.GetGameHistoryAsync())
                .Where(r => r.Skill == meta.Skill && r.PlayedAtUtc >= DateTime.UtcNow.AddDays(-7))
                .Select(r => r.AccuracyPercent)
                .DefaultIfEmpty(0).Average();
            var all = (await ProgressStore.GetGameHistoryAsync())
                .Where(r => r.Skill == meta.Skill)
                .Select(r => r.AccuracyPercent)
                .DefaultIfEmpty(0).Average();

            var headerRow = new Grid
            {
                ColumnDefinitions =
                {
                    new ColumnDefinition { Width = GridLength.Star },
                    new ColumnDefinition { Width = GridLength.Auto }
                }
            };
            headerRow.Children.Add(new Label
            {
                Text = $"{meta.Emoji}  {meta.Title}", FontSize = 15,
                TextColor = ThemeColors.TextPrimary
            });
            var scoreLabel = new Label
            {
                Text = score.ToString(), FontSize = 15,
                FontAttributes = FontAttributes.Bold, TextColor = accent
            };
            Grid.SetColumn(scoreLabel, 1);
            headerRow.Children.Add(scoreLabel);

            var trendLabel = new Label
            {
                Text = $"7 дн: {(int)recent}%  ·  всего: {(int)all}%",
                FontSize = 11, TextColor = ThemeColors.TextMuted
            };

            var fill = new BoxView
            {
                Color = accent,
                HeightRequest = 10,
                HorizontalOptions = LayoutOptions.Start,
                CornerRadius = 5
            };
            var track = new Grid
            {
                HeightRequest = 10,
                Children =
                {
                    new BoxView { Color = lightBg, HeightRequest = 10, CornerRadius = 5 },
                    fill
                }
            };
            track.SizeChanged += (_, _) =>
            {
                if (track.Width > 0) fill.WidthRequest = track.Width * fraction;
            };

            SkillsLayout.Children.Add(new Border
            {
                StrokeShape = new RoundRectangle { CornerRadius = 14 },
                Stroke = Colors.Transparent,
                BackgroundColor = ThemeColors.CardBg,
                Padding = new Thickness(14),
                Shadow = new Shadow
                {
                    Brush = new SolidColorBrush(Colors.Black),
                    Offset = new Point(0, 1), Radius = 6, Opacity = 0.04f
                },
                Content = new VerticalStackLayout
                {
                    Spacing = 8,
                    Children = { headerRow, trendLabel,
                        new Border
                        {
                            StrokeShape = new RoundRectangle { CornerRadius = 5 },
                            Stroke = Colors.Transparent, Padding = 0, Content = track
                        }
                    }
                }
            });
        }
    }

    // ── История ──────────────────────────────────────────────────────
    private async void BuildHistory()
    {
        HistoryLayout.Children.Clear();

        TabGamesBtn.BackgroundColor = _showGames ? ThemeColors.Accent : ThemeColors.CardBg;
        TabGamesBtn.TextColor = _showGames ? Colors.White : ThemeColors.TextMuted;
        TabGamesBtn.BorderWidth = _showGames ? 0 : 1;
        TabGamesBtn.BorderColor = _showGames ? Colors.Transparent : ThemeColors.Border;

        TabTestsBtn.BackgroundColor = _showGames ? ThemeColors.CardBg : ThemeColors.Accent;
        TabTestsBtn.TextColor = _showGames ? ThemeColors.TextMuted : Colors.White;
        TabTestsBtn.BorderWidth = _showGames ? 1 : 0;
        TabTestsBtn.BorderColor = _showGames ? ThemeColors.Border : Colors.Transparent;

        if (_showGames)
            await BuildGameHistoryAsync();
        else
            await BuildTestHistoryAsync();
    }

    private async Task BuildGameHistoryAsync()
    {
        var games = (await ProgressStore.GetGameHistoryAsync()).Take(10).ToList();
        if (games.Count == 0)
        {
            HistoryLayout.Children.Add(new EmptyState("🎮",
                "Игры не найдены", "Начни тренировку, чтобы увидеть историю!", ""));
            return;
        }

        foreach (var result in games)
        {
            var accent = BrainSkillInfo.Accent(result.Skill);
            HistoryLayout.Children.Add(BuildHistoryRow(
                result.GameTitle,
                result.PlayedAtUtc.ToLocalTime().ToString("dd.MM HH:mm"),
                $"{result.AccuracyPercent}% · +{result.EarnedPoints}⭐",
                accent));
        }
    }

    private async Task BuildTestHistoryAsync()
    {
        var tests = (await ProgressStore.GetTestHistoryAsync()).Take(10).ToList();
        if (tests.Count == 0)
        {
            HistoryLayout.Children.Add(new EmptyState("📝",
                "Тесты не найдены", "Пройди тест, чтобы увидеть результаты!", ""));
            return;
        }

        foreach (var r in tests)
        {
            var color = r.IqScore >= 120 ? ThemeColors.Success
                      : r.IqScore >= 100 ? ThemeColors.Accent
                      : ThemeColors.TextMuted;
            HistoryLayout.Children.Add(BuildHistoryRow(
                r.TestTitle,
                r.PlayedAtUtc.ToLocalTime().ToString("dd.MM HH:mm"),
                $"IQ {r.IqScore}  ·  {r.AccuracyPercent}%",
                color));
        }
    }

    private View BuildHistoryRow(string title, string date, string right, Color accent)
    {
        var leftStack = new VerticalStackLayout
        {
            Spacing = 2, VerticalOptions = LayoutOptions.Center,
            Children =
            {
                new Label
                {
                    Text = title, FontSize = 14, FontAttributes = FontAttributes.Bold,
                    TextColor = ThemeColors.TextPrimary
                },
                new Label
                {
                    Text = date, FontSize = 11, TextColor = ThemeColors.TextMuted
                }
            }
        };
        var rightLabel = new Label
        {
            Text = right, FontSize = 13, FontAttributes = FontAttributes.Bold,
            TextColor = accent, VerticalOptions = LayoutOptions.Center
        };

        var grid = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition { Width = GridLength.Star },
                new ColumnDefinition { Width = GridLength.Auto }
            },
            Children = { leftStack }
        };
        Grid.SetColumn(rightLabel, 1);
        grid.Children.Add(rightLabel);

        return new Border
        {
            StrokeShape = new RoundRectangle { CornerRadius = 14 },
            Stroke = Colors.Transparent,
            BackgroundColor = ThemeColors.CardBg,
            Padding = new Thickness(14),
            Content = grid
        };
    }

    private void OnTabGames(object? sender, EventArgs e) { _showGames = true; BuildHistory(); }
    private void OnTabTests(object? sender, EventArgs e) { _showGames = false; BuildHistory(); }

    private async void OnLeaderboardClicked(object? sender, EventArgs e)
        => await Shell.Current.GoToAsync("leaderboard");
}

// ── Отрисовка графика ────────────────────────────────────────────────
file sealed class LineChartDrawable : IDrawable
{
    private readonly List<double> _values;
    private readonly Color _lineColor;
    private readonly Color _gridColor;
    private readonly Color _bgColor;
    private readonly Color _textColor;

    public LineChartDrawable(List<double> values, Color lineColor, Color gridColor, Color bgColor, Color textColor)
    {
        _values = values;
        _lineColor = lineColor;
        _gridColor = gridColor;
        _bgColor = bgColor;
        _textColor = textColor;
    }

    public void Draw(ICanvas canvas, RectF dirtyRect)
    {
        if (_values.Count < 2) return;

        var w = dirtyRect.Width;
        var h = dirtyRect.Height;
        var pad = 10f;
        var min = (float)_values.Min();
        var max = (float)Math.Max(_values.Max(), min + 1);
        var range = max - min;

        float X(int i) => pad + i * (w - 2 * pad) / (_values.Count - 1);
        float Y(double v) => h - pad - (float)((v - min) / range) * (h - 2 * pad);

        canvas.FillColor = _bgColor;
        canvas.FillRectangle(dirtyRect);

        canvas.StrokeColor = _gridColor;
        canvas.StrokeSize = 1;
        for (var i = 0; i <= 4; i++)
        {
            var y = pad + i * (h - 2 * pad) / 4;
            canvas.DrawLine(pad, y, w - pad, y);
        }

        canvas.StrokeColor = _lineColor;
        canvas.StrokeSize = 2.5f;
        canvas.StrokeLineCap = LineCap.Round;
        canvas.StrokeLineJoin = LineJoin.Round;
        for (var i = 0; i < _values.Count - 1; i++)
            canvas.DrawLine(X(i), Y(_values[i]), X(i + 1), Y(_values[i + 1]));

        canvas.FillColor = Colors.White;
        canvas.StrokeColor = _lineColor;
        canvas.StrokeSize = 2;
        for (var i = 0; i < _values.Count; i++)
        {
            canvas.DrawCircle(X(i), Y(_values[i]), 5);
            canvas.FillCircle(X(i), Y(_values[i]), 5);
        }
    }
}

file sealed class EmptyChartDrawable : IDrawable
{
    private readonly Color _textColor;
    public EmptyChartDrawable(Color textColor) => _textColor = textColor;

    public void Draw(ICanvas canvas, RectF dirtyRect)
    {
        canvas.FontColor = _textColor;
        canvas.FontSize = 13;
        canvas.DrawString("Сыграй несколько игр, чтобы увидеть график",
            dirtyRect.X, dirtyRect.Y, dirtyRect.Width, dirtyRect.Height,
            HorizontalAlignment.Center, VerticalAlignment.Center);
    }
}
