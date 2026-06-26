using CogniBoost.Models;
using CogniBoost.Services;
using Microsoft.Maui.Controls.Shapes;

namespace CogniBoost.Pages.Games;

public sealed class SudokuMiniGamePage : GameBasePage
{
    private const int Size = 4;
    private const int Rounds = 5;

    private static List<SudokuPuzzle>? _puzzles;
    private static readonly Dictionary<int, string[][]> _solutions = new();
    private readonly Label _statusLabel = new();
    private readonly Label _progressLabel = new();
    private readonly Entry[,] _cells = new Entry[Size, Size];
    private readonly Grid _grid = new();

    private string[][]? _currentPuzzle;
    private string[][]? _currentSolution;
    private int _puzzleIndex;
    private int _round;
    private int _score;

    public SudokuMiniGamePage()
        : base(GameCatalog.Get("sudoku_mini")!)
    {
        BuildUi();
        LoadPuzzle();
    }

    private static List<SudokuPuzzle> GetPuzzles()
    {
        if (_puzzles is not null) return _puzzles;
        _puzzles = Task.Run(async () =>
            await ContentLoader.LoadListAsync<SudokuPuzzle>("sudoku_puzzles.json"))
            .GetAwaiter().GetResult();
        if (_puzzles is null || _puzzles.Count == 0)
            _puzzles = DefaultPuzzles();
        return _puzzles;
    }

    private static List<SudokuPuzzle> DefaultPuzzles() => new()
    {
        new(new[] { new[] { "1","","3","" }, new[] { "","4","","2" }, new[] { "3","","1","" }, new[] { "","2","","4" } }),
        new(new[] { new[] { "","2","","4" }, new[] { "1","","3","" }, new[] { "","3","","1" }, new[] { "4","","2","" } }),
    };

    private void BuildUi()
    {
        _progressLabel.FontSize = 14;
        _progressLabel.TextColor = ThemeColors.TextSecondary;

        _statusLabel.FontSize = 16;
        _statusLabel.TextColor = ThemeColors.TextPrimary;
        _statusLabel.HorizontalOptions = LayoutOptions.Center;

        _grid.ColumnSpacing = 4;
        _grid.RowSpacing = 4;
        _grid.HorizontalOptions = LayoutOptions.Center;

        for (var r = 0; r < Size; r++)
        {
            _grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(60) });
            _grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(60) });
        }

        for (var r = 0; r < Size; r++)
        {
            for (var c = 0; c < Size; c++)
            {
                var entry = new Entry
                {
                    Text = "", FontSize = 24,
                    HorizontalTextAlignment = TextAlignment.Center,
                    Keyboard = Keyboard.Numeric, MaxLength = 1,
                    BackgroundColor = ThemeColors.CardBg,
                    TextColor = ThemeColors.TextPrimary
                };
                entry.TextChanged += (_, _) => UpdateStatus();

                var border = new Border
                {
                    StrokeShape = new RoundRectangle { CornerRadius = 8 },
                    Stroke = ThemeColors.Border,
                    StrokeThickness = (r % 2 == 0 || c % 2 == 0) ? 2.5f : 1.5f,
                    WidthRequest = 60, HeightRequest = 60,
                    Padding = 0, Content = entry
                };

                Grid.SetRow(border, r);
                Grid.SetColumn(border, c);
                _grid.Children.Add(border);
                _cells[r, c] = entry;
            }
        }

        var submitBtn = new Button
        {
            Text = "Проверить",
            BackgroundColor = BrainSkillInfo.Accent(Definition.Skill),
            TextColor = Colors.White,
            FontAttributes = FontAttributes.Bold,
            HeightRequest = 50, CornerRadius = 14
        };
        submitBtn.Clicked += OnSubmit;

        Content = new ScrollView
        {
            Content = new VerticalStackLayout
            {
                Padding = 24, Spacing = 16,
                Children =
                {
                    new Label
                    {
                        Text = Definition.Title, FontSize = 22,
                        FontAttributes = FontAttributes.Bold,
                        TextColor = BrainSkillInfo.Accent(Definition.Skill),
                        HorizontalOptions = LayoutOptions.Center
                    },
                    _progressLabel, _statusLabel, _grid, submitBtn,
                }
            }
        };
    }

    private void LoadPuzzle()
    {
        var puzzles = GetPuzzles();
        _puzzleIndex = new Random().Next(puzzles.Count);
        _currentPuzzle = puzzles[_puzzleIndex].Grid;

        if (!_solutions.ContainsKey(_puzzleIndex))
            _solutions[_puzzleIndex] = Solve(_currentPuzzle);
        _currentSolution = _solutions[_puzzleIndex];

        _progressLabel.Text = $"Пазл {_round + 1} из {Rounds}";

        for (var r = 0; r < Size; r++)
        {
            for (var c = 0; c < Size; c++)
            {
                var val = _currentPuzzle[r][c];
                _cells[r, c].Text = val;
                _cells[r, c].IsReadOnly = !string.IsNullOrEmpty(val);
                _cells[r, c].FontAttributes = string.IsNullOrEmpty(val)
                    ? FontAttributes.None : FontAttributes.Bold;
            }
        }

        UpdateStatus();
    }

    private void UpdateStatus()
    {
        var filled = 0;
        for (var r = 0; r < Size; r++)
        for (var c = 0; c < Size; c++)
            if (!string.IsNullOrEmpty(_cells[r, c].Text)) filled++;
        _statusLabel.Text = $"Заполнено: {filled} из 16";
    }

    private async void OnSubmit(object? sender, EventArgs e)
    {
        if (_currentPuzzle is null) return;
        var correct = true;

        for (var r = 0; r < Size; r++)
        {
            for (var c = 0; c < Size; c++)
            {
                var expected = _currentPuzzle[r][c];
                var actual = _cells[r, c].Text?.Trim() ?? "";

                if (!string.IsNullOrEmpty(expected))
                {
                    _cells[r, c].BackgroundColor = ThemeColors.CardBg2;
                    continue;
                }

                if (IsCorrect(r, c, actual))
                    _cells[r, c].BackgroundColor = ThemeColors.Success.WithAlpha(0.3f);
                else
                {
                    _cells[r, c].BackgroundColor = ThemeColors.Error.WithAlpha(0.3f);
                    correct = false;
                }
            }
        }

        if (correct)
        {
            _score++;
            _round++;
            HapticService.Success();
            SoundService.PlayCorrect();
            if (_round >= Rounds) { await FinishAsync(_score, Rounds); return; }
            await Task.Delay(800);
            LoadPuzzle();
        }
        else
        {
            HapticService.Error();
            SoundService.PlayWrong();
            await Task.Delay(1500);
            LoadPuzzle();
        }
    }

    private bool IsCorrect(int row, int col, string val)
    {
        if (!int.TryParse(val, out var num) || num < 1 || num > 4) return false;
        return _currentSolution is not null && _currentSolution[row][col] == val;
    }

    private static string[][] Solve(string[][] puzzle)
    {
        var grid = puzzle.Select(r => r.ToArray()).ToArray();
        SolveInternal(grid);
        return grid;
    }

    private static bool SolveInternal(string[][] grid)
    {
        for (var r = 0; r < Size; r++)
        for (var c = 0; c < Size; c++)
        {
            if (!string.IsNullOrEmpty(grid[r][c])) continue;
            for (var n = 1; n <= 4; n++)
            {
                var v = n.ToString();
                if (IsValidPlacement(grid, r, c, v))
                {
                    grid[r][c] = v;
                    if (SolveInternal(grid)) return true;
                    grid[r][c] = "";
                }
            }
            return false;
        }
        return true;
    }

    private static bool IsValidPlacement(string[][] grid, int row, int col, string val)
    {
        for (var i = 0; i < Size; i++)
            if (grid[row][i] == val || grid[i][col] == val) return false;

        var br = row / 2 * 2;
        var bc = col / 2 * 2;
        for (var r = br; r < br + 2; r++)
        for (var c = bc; c < bc + 2; c++)
            if (grid[r][c] == val) return false;

        return true;
    }

    private record SudokuPuzzle(string[][] Grid);
}
