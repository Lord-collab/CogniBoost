using System.Xml.Linq;

namespace CogniBoost.Services;

/// <summary>
/// Простая локализация без ResXResourceReader (работает на всех платформах).
/// Строки хранятся в .resx XML-файлах в Resources/Localization/.
/// Переключение языка: установить SettingsService.Language и перезапустить App.ResetRootPage().
/// </summary>
public static class L10n
{
    private static Dictionary<string, string> _strings = new();
    private static string _loadedLanguage = string.Empty;

    public static void Load()
    {
        var lang = SettingsService.Language;
        if (string.Equals(lang, _loadedLanguage, StringComparison.OrdinalIgnoreCase) && _strings.Count > 0)
            return;

        _loadedLanguage = lang;
        _strings = LoadFromAsset(lang == "en" ? "AppStrings.en.resx" : "AppStrings.ru.resx");

        // Если нужный файл пустой — фолбэк на русский
        if (_strings.Count == 0 && lang != "ru")
            _strings = LoadFromAsset("AppStrings.ru.resx");
    }

    public static string Get(string key, params object[] args)
    {
        if (_strings.Count == 0) Load();
        if (!_strings.TryGetValue(key, out var value)) return key;
        return args.Length > 0 ? string.Format(value, args) : value;
    }

    // Shorthand
    public static string T(string key, params object[] args) => Get(key, args);

    private static Dictionary<string, string> LoadFromAsset(string filename)
    {
        try
        {
            using var stream = FileSystem.OpenAppPackageFileAsync(filename).GetAwaiter().GetResult();
            var doc  = XDocument.Load(stream);
            return doc.Descendants("data")
                .Where(e => e.Attribute("name") != null)
                .ToDictionary(
                    e => e.Attribute("name")!.Value,
                    e => e.Element("value")?.Value ?? string.Empty,
                    StringComparer.OrdinalIgnoreCase);
        }
        catch
        {
            return new Dictionary<string, string>();
        }
    }
}
