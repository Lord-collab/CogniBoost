using CogniBoost.Services;
using Microsoft.Maui.Controls.Shapes;

namespace CogniBoost.Pages;

public sealed class AvatarPickerPage : ContentPage
{
    private static readonly string[] Emojis =
    {
        "🧠","🦊","🐬","🦁","🐉","🦅","🐺","🦋",
        "🐸","🐙","🦄","🐘","🐼","🦊","🦎","🦩",
        "🚀","⚡","🌊","🔥","❄️","🌸","🌙","⭐",
        "🎯","🎮","🎸","🎨","📚","🔬","🏆","💡",
        "🤖","👾","🧙","🥷","🕵️","🧑‍🚀","🧑‍🔬","🧑‍🎤",
    };

    public AvatarPickerPage()
    {
        Title = "Выбери аватар";
        BackgroundColor = ThemeColors.PageBg;
        BuildUi();
    }

    private void BuildUi()
    {
        var grid = new FlexLayout
        {
            Wrap = Microsoft.Maui.Layouts.FlexWrap.Wrap,
            JustifyContent = Microsoft.Maui.Layouts.FlexJustify.Start,
            AlignItems = Microsoft.Maui.Layouts.FlexAlignItems.Start,
        };

        foreach (var emoji in Emojis)
        {
            var btn = new Border
            {
                StrokeShape = new RoundRectangle { CornerRadius = 16 },
                Stroke = Colors.Transparent,
                BackgroundColor = ThemeColors.CardBg,
                Padding = 4,
                Margin = new Thickness(5),
                WidthRequest = 64,
                HeightRequest = 64,
                Content = new Label
                {
                    Text = emoji, FontSize = 36,
                    HorizontalOptions = LayoutOptions.Center,
                    VerticalOptions = LayoutOptions.Center
                }
            };

            var tap = new TapGestureRecognizer();
            tap.Tapped += async (_, _) =>
            {
                AccountStore.SaveAvatar(emoji);
                await Navigation.PopAsync();
            };
            btn.GestureRecognizers.Add(tap);
            grid.Children.Add(btn);
        }

        Content = new ScrollView
        {
            Content = new VerticalStackLayout
            {
                Padding = new Thickness(16),
                Spacing = 12,
                Children =
                {
                    new Label { Text = "Выбери свой аватар", FontSize = 20,
                        FontAttributes = FontAttributes.Bold,
                        TextColor = ThemeColors.TextPrimary },
                    grid
                }
            }
        };
    }
}
