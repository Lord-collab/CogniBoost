using CogniBoost.Models;
using Microsoft.Maui.Controls.Shapes;

namespace CogniBoost.Pages.Games;

/// <summary>
/// Один вопрос с вариантами ответа.
/// </summary>
public sealed record QuizQuestion(string Prompt, string[] Options, int CorrectIndex);

/// <summary>
/// Базовая игра «вопрос — варианты ответа» с фиксированным числом раундов.
/// Используется для логических, языковых и других тестовых мини-игр.
/// </summary>
public abstract class QuizGamePage : GameBasePage
{
    private readonly List<QuizQuestion> _questions;
    private readonly Label _progressLabel = new();
    private readonly Label _promptLabel = new();
    private readonly VerticalStackLayout _optionsLayout = new() { Spacing = 12 };

    private int _index;
    private int _correct;
    private bool _locked;

    protected QuizGamePage(GameDefinition definition, IEnumerable<QuizQuestion> questions)
        : base(definition)
    {
        _questions = questions.ToList();
        BuildUi();
        ShowQuestion();
    }

    protected Color Accent => BrainSkillInfo.Accent(Definition.Skill);

    private void BuildUi()
    {
        _progressLabel.FontSize = 14;
        _progressLabel.TextColor = Color.FromArgb("#6B7280");

        _promptLabel.FontSize = 26;
        _promptLabel.FontAttributes = FontAttributes.Bold;
        _promptLabel.HorizontalTextAlignment = TextAlignment.Center;
        _promptLabel.TextColor = Color.FromArgb("#1A1A2E");

        var promptCard = new Border
        {
            StrokeShape = new RoundRectangle { CornerRadius = 20 },
            Stroke = Colors.Transparent,
            BackgroundColor = Colors.White,
            Padding = 28,
            Content = _promptLabel
        };

        Content = new ScrollView
        {
            Content = new VerticalStackLayout
            {
                Padding = 24,
                Spacing = 20,
                Children =
                {
                    _progressLabel,
                    new Label
                    {
                        Text = Definition.Title,
                        FontSize = 22,
                        FontAttributes = FontAttributes.Bold,
                        TextColor = Accent
                    },
                    promptCard,
                    _optionsLayout
                }
            }
        };
    }

    private void ShowQuestion()
    {
        _locked = false;
        var question = _questions[_index];

        _progressLabel.Text = $"Вопрос {_index + 1} из {_questions.Count}";
        _promptLabel.Text = question.Prompt;

        _optionsLayout.Children.Clear();
        for (var i = 0; i < question.Options.Length; i++)
        {
            var optionIndex = i;
            var button = new Button
            {
                Text = question.Options[i],
                BackgroundColor = Colors.White,
                TextColor = Color.FromArgb("#1A1A2E"),
                FontSize = 18,
                HeightRequest = 54,
                CornerRadius = 14,
                BorderColor = Color.FromArgb("#E5E7EB"),
                BorderWidth = 1
            };
            button.Clicked += (_, _) => OnAnswer(optionIndex, button);
            _optionsLayout.Children.Add(button);
        }
    }

    private async void OnAnswer(int chosenIndex, Button button)
    {
        if (_locked)
        {
            return;
        }

        _locked = true;
        var question = _questions[_index];
        var isCorrect = chosenIndex == question.CorrectIndex;

        if (isCorrect)
        {
            _correct++;
            button.BackgroundColor = Accent;
            button.TextColor = Colors.White;
        }
        else
        {
            button.BackgroundColor = Color.FromArgb("#EF4444");
            button.TextColor = Colors.White;

            // Подсветить правильный вариант.
            if (_optionsLayout.Children.ElementAtOrDefault(question.CorrectIndex) is Button correctButton)
            {
                correctButton.BackgroundColor = Color.FromArgb("#10B981");
                correctButton.TextColor = Colors.White;
            }
        }

        await Task.Delay(700);

        _index++;
        if (_index >= _questions.Count)
        {
            await FinishAsync(_correct, _questions.Count);
        }
        else
        {
            ShowQuestion();
        }
    }
}
