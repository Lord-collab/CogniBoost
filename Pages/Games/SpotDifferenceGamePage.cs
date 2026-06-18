using CogniBoost.Models;
using Microsoft.Maui.Controls.Shapes;

namespace CogniBoost.Pages.Games;

/// <summary>
/// «Найди изменение»: показывается пара списков слов — в одном слово заменено.
/// Нужно найти какое слово появилось (новое) и какое исчезло.
/// </summary>
public sealed class SpotDifferenceGamePage : GameBasePage
{
    private const int Rounds = 6;

    private static readonly string[][] Templates =
    {
        new[]{"Роза","Тюльпан","Гвоздика","Ромашка","Лилия"},
        new[]{"Яблоко","Груша","Слива","Вишня","Персик"},
        new[]{"Стол","Стул","Диван","Шкаф","Кровать"},
        new[]{"Красный","Синий","Зелёный","Жёлтый","Белый"},
        new[]{"Кошка","Собака","Корова","Лошадь","Овца"},
        new[]{"Скрипка","Гитара","Пианино","Флейта","Барабан"},
        new[]{"Москва","Берлин","Париж","Рим","Токио"},
        new[]{"Круг","Квадрат","Треугольник","Ромб","Пятиугольник"},
    };

    private static readonly string[] Replacements =
        {"Маяк","Радуга","Ракета","Зеркало","Молния","Пещера","Фонтан","Компас"};

    private readonly Label _statusLabel   = new();
    private readonly Label _beforeLabel   = new();
    private readonly Label _afterLabel    = new();
    private readonly VerticalStackLayout _optionsLayout = new() { Spacing = 10 };
    private readonly Random _rng = new();

    private int    _index;
    private int    _correct;
    private string _removedWord  = "";
    private string _addedWord    = "";
    private bool   _locked;

    public SpotDifferenceGamePage()
        : base(GameCatalog.Get("spot_difference")!)
    {
        BuildUi();
        ShowRound();
    }

    private Color Accent => BrainSkillInfo.Accent(Definition.Skill);

    private void BuildUi()
    {
        _statusLabel.FontSize  = 14;
        _statusLabel.TextColor = Color.FromArgb("#7B7BA8");

        _beforeLabel.FontSize    = 15;
        _beforeLabel.TextColor   = Color.FromArgb("#0D0D2B");
        _beforeLabel.LineBreakMode = LineBreakMode.WordWrap;

        _afterLabel.FontSize     = 15;
        _afterLabel.TextColor    = Color.FromArgb("#0D0D2B");
        _afterLabel.LineBreakMode = LineBreakMode.WordWrap;

        var card = new Border
        {
            StrokeShape = new RoundRectangle { CornerRadius = 18 },
            Stroke = Colors.Transparent, BackgroundColor = Colors.White,
            Padding = 20,
            Content = new VerticalStackLayout
            {
                Spacing = 10,
                Children =
                {
                    new Label { Text = "Список 1", FontSize = 12,
                        TextColor = Color.FromArgb("#7B7BA8") },
                    _beforeLabel,
                    new BoxView { HeightRequest = 1, Color = Color.FromArgb("#E4E5F5") },
                    new Label { Text = "Список 2", FontSize = 12,
                        TextColor = Color.FromArgb("#7B7BA8") },
                    _afterLabel,
                }
            }
        };

        Content = new ScrollView
        {
            Content = new VerticalStackLayout
            {
                Padding = new Thickness(24), Spacing = 16,
                Children =
                {
                    _statusLabel,
                    new Label { Text = "Какое слово ПОЯВИЛОСЬ?", FontSize = 18,
                        FontAttributes = FontAttributes.Bold, TextColor = Accent },
                    card,
                    _optionsLayout
                }
            }
        };
    }

    private void ShowRound()
    {
        _locked = false;
        _statusLabel.Text = $"Раунд {_index + 1} из {Rounds}";

        var template   = Templates[_rng.Next(Templates.Length)];
        var before     = template.OrderBy(_ => _rng.Next()).ToArray();
        var replIdx    = _rng.Next(before.Length);
        _removedWord   = before[replIdx];
        _addedWord     = Replacements[_rng.Next(Replacements.Length)];

        var after = before.ToArray();
        after[replIdx] = _addedWord;
        after = after.OrderBy(_ => _rng.Next()).ToArray();

        _beforeLabel.Text = string.Join("  ·  ", before);
        _afterLabel.Text  = string.Join("  ·  ", after);

        // Варианты: добавленное слово + 3 случайных из before
        var wrong = before.Where(w => w != _removedWord)
                          .OrderBy(_ => _rng.Next()).Take(3).ToList();
        var opts  = new[] { _addedWord }.Concat(wrong).OrderBy(_ => _rng.Next()).ToArray();

        _optionsLayout.Children.Clear();
        foreach (var opt in opts)
        {
            var btn = new Button
            {
                Text = opt, FontSize = 17, HeightRequest = 50, CornerRadius = 14,
                BackgroundColor = Colors.White, TextColor = Color.FromArgb("#0D0D2B"),
                BorderColor = Color.FromArgb("#E4E5F5"), BorderWidth = 1
            };
            var optCopy = opt;
            btn.Clicked += (_, _) => OnAnswer(optCopy, btn);
            _optionsLayout.Children.Add(btn);
        }
    }

    private async void OnAnswer(string chosen, Button button)
    {
        if (_locked) return;
        _locked = true;

        if (string.Equals(chosen, _addedWord, StringComparison.Ordinal))
        {
            _correct++;
            button.BackgroundColor = Accent;
            button.TextColor = Colors.White;
        }
        else
        {
            button.BackgroundColor = Color.FromArgb("#FF5370");
            button.TextColor = Colors.White;
        }

        await Task.Delay(700);
        _index++;
        if (_index >= Rounds) { await FinishAsync(_correct, Rounds); return; }
        ShowRound();
    }
}
