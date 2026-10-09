using HobbyXP.Data;
using Microsoft.EntityFrameworkCore;

// Import one-shot: SQLite local → PostgreSQL (misma forma de entidades).
// Uso:
//   dotnet run --project src/HobbyXP.Tools/SqliteToPostgres -- --sqlite "C:\...\hobbyxp.db" --postgres "Host=...;..."
// O variables: HOBBYXP_SQLITE_PATH / HOBBYXP_POSTGRES

static string? Arg(string[] args, string name)
{
    for (var i = 0; i < args.Length - 1; i++)
    {
        if (string.Equals(args[i], name, StringComparison.OrdinalIgnoreCase))
            return args[i + 1];
    }

    return null;
}

var sqlitePath = Arg(args, "--sqlite")
    ?? Environment.GetEnvironmentVariable("HOBBYXP_SQLITE_PATH")
    ?? DatabaseConstants.GetDatabasePath();

var postgres = Arg(args, "--postgres")
    ?? Environment.GetEnvironmentVariable("HOBBYXP_POSTGRES");

if (string.IsNullOrWhiteSpace(postgres))
{
    Console.Error.WriteLine("Falta --postgres o HOBBYXP_POSTGRES.");
    return 1;
}

if (!File.Exists(sqlitePath))
{
    Console.Error.WriteLine($"No existe SQLite: {sqlitePath}");
    return 1;
}

Console.WriteLine($"Origen SQLite: {sqlitePath}");
Console.WriteLine("Destino PostgreSQL: (connection string oculta)");

var sqliteOptions = new DbContextOptionsBuilder<HobbyXpDbContext>()
    .UseSqlite($"Data Source={sqlitePath}")
    .Options;

var pgOptions = new DbContextOptionsBuilder<HobbyXpDbContext>()
    .UseNpgsql(postgres)
    .Options;

await using var source = new HobbyXpDbContext(sqliteOptions);
await using var dest = new HobbyXpDbContext(pgOptions);

Console.WriteLine("Creando esquema Postgres (EnsureCreated)…");
await dest.Database.EnsureCreatedAsync();

async Task ClearAsync()
{
    // Orden respetando FKs (similar a ResetApplicationData).
    await dest.GymWorkoutEntries.ExecuteDeleteAsync();
    await dest.GymWorkouts.ExecuteDeleteAsync();
    await dest.RunningSessionSeries.ExecuteDeleteAsync();
    await dest.RunningSessions.ExecuteDeleteAsync();
    await dest.OfficialRaces.ExecuteDeleteAsync();
    await dest.DietDayLogs.ExecuteDeleteAsync();
    await dest.MediaSeriesChapterLogs.ExecuteDeleteAsync();
    await dest.MediaSeries.ExecuteDeleteAsync();
    await dest.MediaEntries.ExecuteDeleteAsync();
    await dest.VideoGameProgressLogs.ExecuteDeleteAsync();
    await dest.VideoGames.ExecuteDeleteAsync();
    await dest.BookReadingLogs.ExecuteDeleteAsync();
    await dest.Books.ExecuteDeleteAsync();
    await dest.CourseSessionLogs.ExecuteDeleteAsync();
    await dest.Courses.ExecuteDeleteAsync();
    await dest.Puzzles.ExecuteDeleteAsync();
    await dest.Suggestions.ExecuteDeleteAsync();
    await dest.EarnedMedals.ExecuteDeleteAsync();
    await dest.XpTransactions.ExecuteDeleteAsync();
    await dest.Milestones.ExecuteDeleteAsync();
    await dest.WeeklyQuotaEvaluations.ExecuteDeleteAsync();
    await dest.DailyQuotaEvaluations.ExecuteDeleteAsync();
    await dest.ModuleDisciplinePauses.ExecuteDeleteAsync();
    await dest.Rewards.ExecuteDeleteAsync();
    await dest.HobbyProgresses.ExecuteDeleteAsync();
    await dest.PlayerProfiles.ExecuteDeleteAsync();
    await dest.Exercises.ExecuteDeleteAsync();
    await dest.MedalDefinitions.ExecuteDeleteAsync();
    await dest.AchievementRules.ExecuteDeleteAsync();
}

Console.WriteLine("Limpiando destino…");
await ClearAsync();

async Task CopySetAsync<T>(DbSet<T> from, DbSet<T> to, string label) where T : class
{
    var rows = await from.AsNoTracking().ToListAsync();
    if (rows.Count == 0)
    {
        Console.WriteLine($"  {label}: 0");
        return;
    }

    await to.AddRangeAsync(rows);
    await dest.SaveChangesAsync();
    dest.ChangeTracker.Clear();
    Console.WriteLine($"  {label}: {rows.Count}");
}

Console.WriteLine("Copiando…");

// Catálogos primero
await CopySetAsync(source.AchievementRules, dest.AchievementRules, "AchievementRules");
await CopySetAsync(source.MedalDefinitions, dest.MedalDefinitions, "MedalDefinitions");
await CopySetAsync(source.Exercises, dest.Exercises, "Exercises");
await CopySetAsync(source.PlayerProfiles, dest.PlayerProfiles, "PlayerProfiles");
await CopySetAsync(source.HobbyProgresses, dest.HobbyProgresses, "HobbyProgresses");
await CopySetAsync(source.Rewards, dest.Rewards, "Rewards");
await CopySetAsync(source.OfficialRaces, dest.OfficialRaces, "OfficialRaces");
await CopySetAsync(source.RunningSessions, dest.RunningSessions, "RunningSessions");
await CopySetAsync(source.RunningSessionSeries, dest.RunningSessionSeries, "RunningSessionSeries");
await CopySetAsync(source.GymWorkouts, dest.GymWorkouts, "GymWorkouts");
await CopySetAsync(source.GymWorkoutEntries, dest.GymWorkoutEntries, "GymWorkoutEntries");
await CopySetAsync(source.DietDayLogs, dest.DietDayLogs, "DietDayLogs");
await CopySetAsync(source.Puzzles, dest.Puzzles, "Puzzles");
await CopySetAsync(source.MediaEntries, dest.MediaEntries, "MediaEntries");
await CopySetAsync(source.MediaSeries, dest.MediaSeries, "MediaSeries");
await CopySetAsync(source.MediaSeriesChapterLogs, dest.MediaSeriesChapterLogs, "MediaSeriesChapterLogs");
await CopySetAsync(source.VideoGames, dest.VideoGames, "VideoGames");
await CopySetAsync(source.VideoGameProgressLogs, dest.VideoGameProgressLogs, "VideoGameProgressLogs");
await CopySetAsync(source.Books, dest.Books, "Books");
await CopySetAsync(source.BookReadingLogs, dest.BookReadingLogs, "BookReadingLogs");
await CopySetAsync(source.Courses, dest.Courses, "Courses");
await CopySetAsync(source.CourseSessionLogs, dest.CourseSessionLogs, "CourseSessionLogs");
await CopySetAsync(source.Suggestions, dest.Suggestions, "Suggestions");
await CopySetAsync(source.XpTransactions, dest.XpTransactions, "XpTransactions");
await CopySetAsync(source.Milestones, dest.Milestones, "Milestones");
await CopySetAsync(source.EarnedMedals, dest.EarnedMedals, "EarnedMedals");
await CopySetAsync(source.WeeklyQuotaEvaluations, dest.WeeklyQuotaEvaluations, "WeeklyQuotaEvaluations");
await CopySetAsync(source.DailyQuotaEvaluations, dest.DailyQuotaEvaluations, "DailyQuotaEvaluations");
await CopySetAsync(source.ModuleDisciplinePauses, dest.ModuleDisciplinePauses, "ModuleDisciplinePauses");

// Resincronizar secuencias de identidad en Postgres
await dest.Database.ExecuteSqlRawAsync("""
DO $$
DECLARE r record;
BEGIN
  FOR r IN
    SELECT quote_ident(c.relname) AS tbl,
           quote_ident(a.attname) AS col
    FROM pg_class c
    JOIN pg_namespace n ON n.oid = c.relnamespace
    JOIN pg_attribute a ON a.attrelid = c.oid
    JOIN pg_attrdef d ON d.adrelid = c.oid AND d.adnum = a.attnum
    WHERE c.relkind = 'r'
      AND n.nspname = 'public'
      AND a.attnum > 0
      AND NOT a.attisdropped
      AND pg_get_expr(d.adbin, d.adrelid) LIKE 'nextval%'
  LOOP
    EXECUTE format(
      'SELECT setval(pg_get_serial_sequence(%L, %L), COALESCE((SELECT MAX(%I) FROM %I), 1))',
      r.tbl, r.col, r.col, r.tbl);
  END LOOP;
END $$;
""");

Console.WriteLine("Importación completada.");
Console.WriteLine("Nota: copie también la carpeta de fotos (Avatar, covers, etc.) al MediaRoot del VPS si aplica.");
return 0;
