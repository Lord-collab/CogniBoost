namespace CogniBoost.Services;

/// <summary>
/// Конфигурация подключения к Supabase.
///
/// ЗАПОЛНИ ЭТИ ЗНАЧЕНИЯ, когда создашь проект Supabase:
///   1. Создай проект на supabase.com.
///   2. Settings → API: скопируй Project URL и anon public key.
///   3. Вставь их ниже.
///   4. Создай таблицы (см. комментарий SupabaseSchema ниже) и включи RLS-политики
///      на чтение/запись, иначе запросы будут возвращать 401/403.
///
/// Пока значения пустые, приложение работает полностью офлайн (локальное хранилище).
/// </summary>
public static class SupabaseConfig
{
    private const string UrlKey = "supabase_project_url";
    private const string AnonKeyPref = "supabase_anon_key";

    private const string DefaultProjectUrl = "https://lpkyisjajsfpkbkqpnor.supabase.co";
    private const string DefaultAnonKey = "sb_publishable_LhrcF7asVwIJcuWYVVl2xg_ZAfsgAQm";

    public static string ProjectUrl => LoadFromSecure(UrlKey) ?? DefaultProjectUrl;
    public static string AnonKey => LoadFromSecure(AnonKeyPref) ?? DefaultAnonKey;

    public static bool IsConfigured =>
        !string.IsNullOrWhiteSpace(ProjectUrl) && !string.IsNullOrWhiteSpace(AnonKey);

    private static string? LoadFromSecure(string key)
    {
        try { return SecureStorage.Default.GetAsync(key).GetAwaiter().GetResult(); }
        catch { return null; }
    }
}

/// <summary>
/// Рекомендуемая схема таблиц Supabase (для справки при настройке):
///
/// players(id uuid pk default gen_random_uuid(), username text unique,
///         display_name text, avatar_emoji text, age int, created_at timestamptz default now())
///
/// player_scores(player_id uuid references players(id), overall int,
///               memory int, focus int, language int, logic int,
///               points_balance int, points_lifetime int, updated_at timestamptz)
///
/// game_scores(player_id uuid, game_id text, best_score int,
///             accuracy int, last_played_at timestamptz, primary key(player_id, game_id))
///
/// test_results(id uuid pk default gen_random_uuid(), player_id uuid,
///              test_id text, iq_score int, correct int, total int, played_at timestamptz)
///
/// Представление leaderboard_overall: select из player_scores + players,
///   отсортированное по overall desc.
/// </summary>
public static class SupabaseSchema
{
    // Только документация. Реальные миграции выполняются в SQL-редакторе Supabase.
}
