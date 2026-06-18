using CogniBoost.Models;
using Microsoft.Maui.Controls.Shapes;

namespace CogniBoost.Pages.Games;

/// <summary>
/// «Быстрый тап»: появляется цветная цель. Нужно тапать только по зелёным
/// и не трогать красные. Счёт — число верных реакций за N раундов.
/// </summary>
public sealed class ReactionGamePage : GameBasePage
{
    private const int Rounds = 12;

    private readonly Label _statusLabel = new();
    private readonly Label _instructionLabel = new();
    private readonly Border _target;
    private readonly Random _rng = new();

    private int _round;
    private int _score;
    private bool _isGoTarget;
    private bool _awaitingTap;
    private DateTime _shownAt;

    public ReactionGamePage()
        : base(GameCatalog.Get("reaction_tap")!)
    {
        _target = new Border
        {
            StrokeShape = new RoundRectangle { CornerRadius = 24 },
            Stroke = Colors.Transparent,
            BackgroundColor = Color.FromArgb("#E5E7EB"),
            HeightRequest = 220,
            Content = new Label
            {
                Text = "Приготовься…",
                FontSize = 20,
                TextColor = Colors.White,
                HorizontalOptions = LayoutOptions.Center,
                VerticalOptions = LayoutOptions.Center
            }
        };

        var tap = new TapGestureRecognizer();
        tap.Tapped += (_, _) => OnTargetTapped();
        _target.GestureRecognizers.Add(tap);

        BuildUi();
        _ = RunRoundsAsync();
    }

    private Color Accent => BrainSkillInfo.Accent(Definition.Skill);

    private void BuildUi()
    {
        _statusLabel.FontSize = 16;
        _statusLabel.TextColor = Color.FromArgb("#6B7280");

        _instructionLabel.FontSize = 14;
        _instructionLabel.TextColor = Color.FromArgb("#6B7280");
        _instructionLabel.Text = "Тапай по ЗЕЛЁНОМУ. Не трогай КРАСНЫЙ.";

        UpdateStatus();

        Content = new VerticalStackLayout
        {
            Padding = 24,
            Spacing = 18,
            Children =
            {
                new Label
                {
                    Text = Definition.Title,
                    FontSize = 22,
                    FontAttributes = FontAttributes.Bold,
                    TextColor = Accent
                },
                _instructionLabel,
                _statusLabel,
                _target
            }
        };
    }

    private void UpdateStatus()
    {
        _statusLabel.Text = $"Раунд {Math.Min(_round + 1, Rounds)} из {Rounds} · Очки: {_score}";
    }

    private async Task RunRoundsAsync()
    {
        while (_round < Rounds)
        {
            // Пауза перед показом цели (случайная задержка).
            await ShowIdle();
            await Task.Delay(_rng.Next(700, 1600));

            _isGoTarget = _rng.Next(100) < 70; // 70% зелёных
            ShowTarget();
            _awaitingTap = true;
            _shownAt = DateTime.UtcNow;

            // Окно реакции.
            var window = 1100;
            await Task.Delay(window);

            if (_awaitingTap)
            {
                // Не нажали вовремя.
                _awaitingTap = false;
                if (!_isGoTarget)
                {
                    // Правильно проигнорировал красный.
                    _score++;
                }

                _round++;
                UpdateStatus();
            }
        }

        await FinishAsync(_score, Rounds);
    }

    private Task ShowIdle()
    {
        return MainThread.InvokeOnMainThreadAsync(() =>
        {
            _target.BackgroundColor = Color.FromArgb("#E5E7EB");
            SetTargetText("Жди…");
        });
    }

    private void ShowTarget()
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            _target.BackgroundColor = _isGoTarget
                ? Color.FromArgb("#10B981")
                : Color.FromArgb("#EF4444");
            SetTargetText(_isGoTarget ? "ТАП!" : "СТОП");
        });
    }

    private void SetTargetText(string text)
    {
        if (_target.Content is Label label)
        {
            label.Text = text;
        }
    }

    private void OnTargetTapped()
    {
        if (!_awaitingTap)
        {
            return;
        }

        _awaitingTap = false;

        if (_isGoTarget)
        {
            _score++;
        }
        // Тап по красному — промах, очко не начисляется.

        _round++;
        UpdateStatus();
    }
}
