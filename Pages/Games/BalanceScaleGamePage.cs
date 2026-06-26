using CogniBoost.Models;
using CogniBoost.Services;
using Microsoft.Maui.Controls.Shapes;

namespace CogniBoost.Pages.Games;

public sealed class BalanceScaleGamePage : GameBasePage
{
    private const int Rounds = 8;

    private static readonly (string Prompt, string[] Options, int CorrectIndex)[] Questions =
    {
        ("Яблоко тяжелее груши.\nГруша тяжелее сливы.\nКто самый лёгкий?",
            new[]{ "Яблоко", "Груша", "Слива" }, 2),
        ("Кошка тяжелее мышки.\nСобака тяжелее кошки.\nКто самый тяжёлый?",
            new[]{ "Мышка", "Кошка", "Собака" }, 2),
        ("A > B, B > C, C > D.\nЧто верно?",
            new[]{ "A < D", "A > D", "B < C" }, 1),
        ("Арбуз тяжелее дыни.\nДыня тяжелее тыквы.\nКто средний по весу?",
            new[]{ "Арбуз", "Дыня", "Тыква" }, 1),
        ("Слон тяжелее носорога.\nНосорог тяжелее бегемота.\nБегемот тяжелее зебры.\nКто самый лёгкий?",
            new[]{ "Слон", "Носорог", "Бегемот", "Зебра" }, 3),
        ("Книга тяжелее тетради.\nРюкзак тяжелее книги.\nЧто верно?",
            new[]{ "Рюкзак легче тетради", "Рюкзак тяжелее книги", "Тетрадь тяжелее рюкзака" }, 1),
        ("Петя выше Васи.\nВася выше Коли.\nКто самый низкий?",
            new[]{ "Петя", "Вася", "Коля" }, 2),
        ("Заяц быстрее черепахи.\nВолк быстрее зайца.\nКто самый быстрый?",
            new[]{ "Черепаха", "Заяц", "Волк" }, 2),
        ("Чашка легче тарелки.\nТарелка легче кастрюли.\nЧто самое тяжёлое?",
            new[]{ "Чашка", "Тарелка", "Кастрюля" }, 2),
        ("A > B = C > D.\nЧто верно?",
            new[]{ "A = D", "C > A", "B > D" }, 2),
    };

    private readonly Label _progressLabel = new();
    private readonly Label _promptLabel = new();
    private readonly VerticalStackLayout _optionsLayout = new() { Spacing = 12 };
    private readonly Random _rng = new();

    private readonly int[] _order;
    private int _index;
    private int _correct;
    private bool _locked;

    public BalanceScaleGamePage()
        : base(GameCatalog.Get("balance_scale")!)
    {
        _order = Enumerable.Range(0, Questions.Length).OrderBy(_ => _rng.Next()).ToArray();
        BuildUi();
        ShowQuestion();
    }

    private Color Accent => BrainSkillInfo.Accent(Definition.Skill);

    private void BuildUi()
    {
        _progressLabel.FontSize = 14;
        _progressLabel.TextColor = ThemeColors.TextSecondary;

        _promptLabel.FontSize = 18;
        _promptLabel.FontAttributes = FontAttributes.Bold;
        _promptLabel.HorizontalTextAlignment = TextAlignment.Center;
        _promptLabel.TextColor = ThemeColors.TextPrimary;

        var promptCard = new Border
        {
            StrokeShape = new RoundRectangle { CornerRadius = 20 },
            Stroke = Colors.Transparent,
            BackgroundColor = ThemeColors.CardBg,
            Padding = 24,
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
                    new Label
                    {
                        Text = Definition.Title, FontSize = 22,
                        FontAttributes = FontAttributes.Bold,
                        TextColor = Accent
                    },
                    _progressLabel,
                    new Label
                    {
                        Text = "⚖️", FontSize = 48,
                        HorizontalOptions = LayoutOptions.Center
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
        var q = Questions[_order[_index]];

        _progressLabel.Text = $"Вопрос {_index + 1} из {Rounds}";
        _promptLabel.Text = q.Prompt;

        _optionsLayout.Children.Clear();
        for (var i = 0; i < q.Options.Length; i++)
        {
            var optIndex = i;
            var button = new Button
            {
                Text = q.Options[i],
                BackgroundColor = ThemeColors.CardBg,
                TextColor = ThemeColors.TextPrimary,
                FontSize = 17,
                HeightRequest = 52,
                CornerRadius = 14,
                BorderColor = ThemeColors.Border,
                BorderWidth = 1
            };
            button.Clicked += (_, _) => OnAnswer(optIndex, button, q.CorrectIndex);
            _optionsLayout.Children.Add(button);
        }
    }

    private async void OnAnswer(int chosenIndex, Button button, int correctIndex)
    {
        if (_locked) return;
        _locked = true;

        if (chosenIndex == correctIndex)
        {
            _correct++;
            button.BackgroundColor = ThemeColors.Success;
            button.TextColor = Colors.White;
            HapticService.Click();
            SoundService.PlayCorrect();
            _ = button.PopAsync();
        }
        else
        {
            button.BackgroundColor = ThemeColors.Error;
            button.TextColor = Colors.White;
            HapticService.Error();
            SoundService.PlayWrong();
            _ = button.ShakeAsync();

            if (_optionsLayout.Children.ElementAtOrDefault(correctIndex) is Button correctButton)
            {
                correctButton.BackgroundColor = ThemeColors.Success;
                correctButton.TextColor = Colors.White;
            }
        }

        await Task.Delay(700);

        _index++;
        if (_index >= Rounds)
        {
            await FinishAsync(_correct, Rounds);
        }
        else
        {
            ShowQuestion();
        }
    }
}
