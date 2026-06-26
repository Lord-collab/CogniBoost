using CogniBoost.Models;

namespace CogniBoost.Pages;

public static class ThemeColors
{
    private static bool IsDark =>
        Application.Current?.RequestedTheme == AppTheme.Dark;

    // Surfaces
    public static Color PageBg     => Color.FromArgb(IsDark ? "#0B0F1A" : "#FAFAFA");
    public static Color CardBg     => Color.FromArgb(IsDark ? "#111827" : "#FFFFFF");
    public static Color CardBg2    => Color.FromArgb(IsDark ? "#1F2937" : "#F9FAFB");
    public static Color Border     => Color.FromArgb(IsDark ? "#374151" : "#E5E7EB");
    public static Color Divider    => Color.FromArgb(IsDark ? "#374151" : "#E5E7EB");

    // Text
    public static Color TextPrimary    => Color.FromArgb(IsDark ? "#F9FAFB" : "#111827");
    public static Color TextSecondary  => Color.FromArgb(IsDark ? "#9CA3AF" : "#6B7280");
    public static Color TextMuted      => Color.FromArgb(IsDark ? "#6B7280" : "#9CA3AF");
    public static Color TextOnPrimary  => Color.FromArgb("#FFFFFF");

    // Accents
    public static Color Accent      => Color.FromArgb("#4F46E5");
    public static Color AccentLight => Color.FromArgb("#EEF2FF");
    public static Color AccentDark  => Color.FromArgb("#6366F1");
    public static Color Secondary   => Color.FromArgb("#06B6D4");
    public static Color Tertiary    => Color.FromArgb("#7C3AED");
    public static Color Success     => Color.FromArgb("#10B981");
    public static Color Warning     => Color.FromArgb("#F59E0B");
    public static Color WarningLight => Color.FromArgb("#FEF3C7");
    public static Color Error       => Color.FromArgb("#EF4444");

    // Skill colors (consistent across app)
    public static Color SkillMemory   => Color.FromArgb(IsDark ? "#60A5FA" : "#3B82F6");
    public static Color SkillFocus    => Color.FromArgb(IsDark ? "#34D399" : "#10B981");
    public static Color SkillLanguage => Color.FromArgb(IsDark ? "#FB923C" : "#F97316");
    public static Color SkillLogic    => Color.FromArgb(IsDark ? "#A78BFA" : "#8B5CF6");
    public static Color SkillSpeed    => Color.FromArgb(IsDark ? "#F87171" : "#EF4444");

    public static Color SkillColor(BrainSkill skill) => skill switch
    {
        BrainSkill.Memory   => SkillMemory,
        BrainSkill.Focus    => SkillFocus,
        BrainSkill.Language => SkillLanguage,
        BrainSkill.Logic    => SkillLogic,
        _ => SkillSpeed
    };

    public static Color SkillColorLight(BrainSkill skill) => skill switch
    {
        BrainSkill.Memory   => Color.FromArgb("#DBEAFE"),
        BrainSkill.Focus    => Color.FromArgb("#D1FAE5"),
        BrainSkill.Language => Color.FromArgb("#FED7AA"),
        BrainSkill.Logic    => Color.FromArgb("#EDE9FE"),
        _ => Color.FromArgb("#FEE2E2")
    };
}