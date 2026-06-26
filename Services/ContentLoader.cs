using System.Text.Json;

namespace CogniBoost.Services;

public static class ContentLoader
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public static async Task<List<T>> LoadListAsync<T>(string filename)
    {
        try
        {
            using var stream = await FileSystem.OpenAppPackageFileAsync($"games/{filename}");
            using var reader = new StreamReader(stream);
            var json = await reader.ReadToEndAsync();
            return JsonSerializer.Deserialize<List<T>>(json, JsonOpts) ?? new();
        }
        catch
        {
            return new();
        }
    }

    public static async Task<T?> LoadSingleAsync<T>(string filename) where T : class
    {
        try
        {
            using var stream = await FileSystem.OpenAppPackageFileAsync($"games/{filename}");
            using var reader = new StreamReader(stream);
            var json = await reader.ReadToEndAsync();
            return JsonSerializer.Deserialize<T>(json, JsonOpts);
        }
        catch
        {
            return null;
        }
    }
}
