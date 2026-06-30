namespace CogniBoost.Services;

/// <summary>
/// Вспомогательные анимации для VisualElement:
/// Pop (сжатие+возврат), FadeIn (появление), Shake (тряска), Pulse (пульсация).
/// </summary>
public static class AnimationExtensions
{
    public static async Task PopAsync(this VisualElement view)
    {
        await view.ScaleToAsync(0.92, 60, Easing.CubicIn);
        await view.ScaleToAsync(1.0, 120, Easing.CubicOut);
    }

    public static async Task FadeInAsync(this VisualElement view, uint length = 300)
    {
        view.Opacity = 0;
        await view.FadeToAsync(1, length, Easing.CubicOut);
    }

    public static async Task ShakeAsync(this VisualElement view)
    {
        var x = view.TranslationX;
        for (var i = 0; i < 3; i++)
        {
            await view.TranslateToAsync(x - 8, 0, 40, Easing.CubicIn);
            await view.TranslateToAsync(x + 8, 0, 40, Easing.CubicOut);
        }
        view.TranslationX = x;
    }

    public static async Task PulseAsync(this VisualElement view)
    {
        await view.ScaleToAsync(1.05, 150, Easing.CubicIn);
        await view.ScaleToAsync(1.0, 150, Easing.CubicOut);
    }
}
