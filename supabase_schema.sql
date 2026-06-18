
-- ---------------------------------------------------------------
-- 1. Таблица игроков
-- ---------------------------------------------------------------
create table if not exists public.players (
    id            uuid primary key default gen_random_uuid(),
    username      text not null unique,
    display_name  text not null default '',
    avatar_emoji  text not null default '🧠',
    age           int  not null default 0,
    created_at    timestamptz not null default now()
);

-- ---------------------------------------------------------------
-- 2. Таблица очков по навыкам
-- ---------------------------------------------------------------
create table if not exists public.player_scores (
    player_id       uuid primary key references public.players(id) on delete cascade,
    overall         int not null default 0,
    memory          int not null default 0,
    focus           int not null default 0,
    language        int not null default 0,
    logic           int not null default 0,
    points_balance  int not null default 0,
    points_lifetime int not null default 0,
    updated_at      timestamptz not null default now()
);

-- ---------------------------------------------------------------
-- 3. Таблица результатов игр
-- ---------------------------------------------------------------
create table if not exists public.game_scores (
    player_id      uuid not null references public.players(id) on delete cascade,
    game_id        text not null,
    best_score     int  not null default 0,
    accuracy_pct   int  not null default 0,
    last_played_at timestamptz not null default now(),
    primary key (player_id, game_id)
);

-- ---------------------------------------------------------------
-- 4. Таблица результатов тестов
-- ---------------------------------------------------------------
create table if not exists public.test_results (
    id          uuid primary key default gen_random_uuid(),
    player_id   uuid not null references public.players(id) on delete cascade,
    test_id     text not null,
    iq_score    int  not null default 0,
    correct     int  not null default 0,
    total       int  not null default 0,
    played_at   timestamptz not null default now()
);

-- ---------------------------------------------------------------
-- 5. Представление таблицы лидеров
-- ---------------------------------------------------------------
create or replace view public.leaderboard_overall as
select
    p.id,
    p.username,
    p.display_name,
    p.avatar_emoji,
    coalesce(s.overall, 0) as overall_score,
    coalesce(s.memory, 0)   as memory,
    coalesce(s.focus, 0)    as focus,
    coalesce(s.language, 0) as language,
    coalesce(s.logic, 0)    as logic,
    s.updated_at
from public.players p
left join public.player_scores s on s.player_id = p.id
order by overall_score desc;

-- ---------------------------------------------------------------
-- 6. RLS — включаем для всех таблиц
-- ---------------------------------------------------------------
alter table public.players      enable row level security;
alter table public.player_scores enable row level security;
alter table public.game_scores   enable row level security;
alter table public.test_results  enable row level security;

-- ---------------------------------------------------------------
-- 7. RLS-политики: анонимный ключ может читать и писать
--    (мобильное приложение использует anon key напрямую)
-- ---------------------------------------------------------------

-- Удаляем старые политики если есть (идемпотентность)
do $$
declare
    pol text;
begin
    for pol in
        select policyname from pg_policies where schemaname = 'public'
        and tablename in ('players','player_scores','game_scores','test_results')
    loop
        execute format('drop policy if exists %I on public.%I', pol, 
            (select tablename from pg_policies where policyname = pol and schemaname = 'public'));
    end loop;
end $$;

-- players: читать всех, писать/обновлять свою строку
create policy "anon_select_players"
    on public.players for select
    using (true);

create policy "anon_insert_players"
    on public.players for insert
    with check (true);

create policy "anon_update_players"
    on public.players for update
    using (true)
    with check (true);

-- player_scores
create policy "anon_select_scores"
    on public.player_scores for select
    using (true);

create policy "anon_insert_scores"
    on public.player_scores for insert
    with check (true);

create policy "anon_update_scores"
    on public.player_scores for update
    using (true)
    with check (true);

-- game_scores
create policy "anon_select_game_scores"
    on public.game_scores for select
    using (true);

create policy "anon_insert_game_scores"
    on public.game_scores for insert
    with check (true);

create policy "anon_update_game_scores"
    on public.game_scores for update
    using (true)
    with check (true);

-- test_results
create policy "anon_select_test_results"
    on public.test_results for select
    using (true);

create policy "anon_insert_test_results"
    on public.test_results for insert
    with check (true);

-- ---------------------------------------------------------------
-- 8. Индексы для быстрого запроса рейтинга
-- ---------------------------------------------------------------
create index if not exists idx_player_scores_overall
    on public.player_scores(overall desc);

create index if not exists idx_game_scores_player
    on public.game_scores(player_id);

create index if not exists idx_test_results_player
    on public.test_results(player_id);

-- готово
select 'CogniBoost schema created successfully' as status;
