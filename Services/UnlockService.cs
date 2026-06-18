using System.Text.Json;
using CogniBoost.Models;

namespace CogniBoost.Services;

/// <summary>
/// Управляет разблокировкой игр за бонусные очки.
/// Стартовые игры доступны всегда; остальные нужно купить.
/// </summary>
public static class UnlockService
{
    private const string UnlockedPrefix = "cb_unlocked_games_";

    public static bool IsUnlocked(GameDefinition game)
    {
        if (game.Starter)
        {
            return true;
        }

        return LoadUnlocked().Contains(game.Id, StringComparer.OrdinalIgnoreCase);
    }

    public static bool IsUnlocked(string gameId)
    {
        var game = Pages.GameCatalog.Get(gameId);
        return game is null || IsUnlocked(game);
    }

    public static bool TryUnlock(GameDefinition game, out string message)
    {
        if (IsUnlocked(game))
        {
            message = $"«{game.Title}» уже открыта.";
            return true;
        }

        if (!PointsService.TrySpend(game.UnlockCost, out message))
        {
            return false;
        }

        var unlocked = LoadUnlocked();
        unlocked.Add(game.Id);
        SaveUnlocked(unlocked);
        message = $"«{game.Title}» открыта за {game.UnlockCost} ⭐";
        return true;
    }

    private static HashSet<string> LoadUnlocked()
    {
        var raw = Preferences.Default.Get(UnlockedKey(), string.Empty);
        if (string.IsNullOrWhiteSpace(raw))
        {
            return new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        }

        try
        {
            var list = JsonSerializer.Deserialize<List<string>>(raw) ?? new List<string>();
            return new HashSet<string>(list, StringComparer.OrdinalIgnoreCase);
        }
        catch
        {
            return new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        }
    }

    private static void SaveUnlocked(HashSet<string> unlocked)
    {
        Preferences.Default.Set(UnlockedKey(), JsonSerializer.Serialize(unlocked.ToList()));
    }

    private static string UnlockedKey() => $"{UnlockedPrefix}{UserKey()}";

    private static string UserKey()
    {
        var key = AccountStore.GetCurrentUsernameKey();
        return string.IsNullOrWhiteSpace(key) ? "guest" : key;
    }
}
