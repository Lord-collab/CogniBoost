namespace CogniBoost.Pages.Controls;

public sealed class ProgressRingDrawable : IDrawable
{
    public float Progress { get; set; }
    public Color TrackColor { get; set; } = ThemeColors.Divider;
    public Color ProgressColor { get; set; } = ThemeColors.Accent;
    public float Thickness { get; set; } = 10;
    public string? CenterText { get; set; }
    public Color? CenterTextColor { get; set; }

    public void Draw(ICanvas canvas, RectF dirtyRect)
    {
        var cx = dirtyRect.Width / 2;
        var cy = dirtyRect.Height / 2;
        var radius = Math.Min(cx, cy) - Thickness / 2;
        var startAngle = 270f;
        var sweepAngle = 360f * Math.Clamp(Progress, 0, 1);

        // Track
        canvas.StrokeColor = TrackColor;
        canvas.StrokeSize = Thickness;
        canvas.StrokeLineCap = LineCap.Round;
        canvas.DrawCircle(cx, cy, radius);

        // Progress
        canvas.StrokeColor = ProgressColor;
        using var path = new PathF();
        path.AddArc(cx - radius, cy - radius, cx + radius, cy + radius, startAngle, startAngle + sweepAngle, false);
        canvas.DrawPath(path);

        // Center text
        if (!string.IsNullOrEmpty(CenterText))
        {
            canvas.FontColor = CenterTextColor ?? Colors.Black;
            canvas.FontSize = radius * 0.45f;
            canvas.Font = Microsoft.Maui.Graphics.Font.Default;
            canvas.DrawString(CenterText, cx, cy, HorizontalAlignment.Center);
        }
    }
}

public sealed class ProgressRing : GraphicsView
{
    private readonly ProgressRingDrawable _drawable;

    public ProgressRing()
    {
        _drawable = new ProgressRingDrawable();
        Drawable = _drawable;
        HeightRequest = 120;
        WidthRequest = 120;
    }

    public float Progress
    {
        get => _drawable.Progress;
        set { _drawable.Progress = value; Invalidate(); }
    }

    public Color RingColor
    {
        get => _drawable.ProgressColor;
        set { _drawable.ProgressColor = value; Invalidate(); }
    }

    public string? CenterText
    {
        get => _drawable.CenterText;
        set { _drawable.CenterText = value; Invalidate(); }
    }
}