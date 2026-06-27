using System.ComponentModel;
using CogniBoost.Models;
using CogniBoost.Services;

namespace CogniBoost.Pages;

public partial class OnboardingPage : ContentPage
{
    private readonly List<SkillChoice> _choices;

    public OnboardingPage()
    {
        InitializeComponent();

        _choices = BrainSkillInfo.All
            .Select(meta => new SkillChoice(meta))
            .ToList();

        SkillsCollection.ItemsSource = _choices;
    }

    private void OnSkillTapped(object? sender, TappedEventArgs e)
    {
        if (sender is BindableObject bindable && bindable.BindingContext is SkillChoice choice)
        {
            choice.IsSelected = !choice.IsSelected;
            UpdateContinueState();
        }
    }

    private void UpdateContinueState()
    {
        ContinueButton.IsEnabled = _choices.Any(c => c.IsSelected);
    }

    private async void OnContinueClicked(object? sender, EventArgs e)
    {
        var selected = _choices.Where(c => c.IsSelected).Select(c => c.Skill);
        await AccountStore.SaveSelectedSkillsAsync(selected);
        App.ResetRootPage();
    }

    /// <summary>Элемент выбора направления с реактивным состоянием.</summary>
    public sealed class SkillChoice : INotifyPropertyChanged
    {
        private bool _isSelected;

        public SkillChoice(BrainSkillInfo.SkillMeta meta)
        {
            Skill = meta.Skill;
            Title = meta.Title;
            Description = meta.Description;
            Emoji = meta.Emoji;
            AccentColor = Color.FromArgb(meta.AccentHex);
        }

        public BrainSkill Skill { get; }
        public string Title { get; }
        public string Description { get; }
        public string Emoji { get; }
        public Color AccentColor { get; }

        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (_isSelected == value)
                {
                    return;
                }

                _isSelected = value;
                Notify(nameof(IsSelected));
                Notify(nameof(CheckMark));
                Notify(nameof(BorderColor));
                Notify(nameof(CardColor));
            }
        }

        public string CheckMark => _isSelected ? "\u2714" : string.Empty;
        public Color BorderColor => _isSelected ? AccentColor : ThemeColors.Border;
        public Color CardColor => ThemeColors.CardBg;

        public event PropertyChangedEventHandler? PropertyChanged;

        private void Notify(string name)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
