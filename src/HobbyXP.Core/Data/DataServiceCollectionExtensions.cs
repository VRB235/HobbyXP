using HobbyXP.Services.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace HobbyXP.Data;

public static class DataServiceCollectionExtensions
{
    /// <summary>
    /// Registra DbContext con el proveedor que configure el host (SQLite o PostgreSQL).
    /// </summary>
    public static IServiceCollection AddHobbyXpData(
        this IServiceCollection services,
        Action<DbContextOptionsBuilder> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);

        services.AddDbContextFactory<HobbyXpDbContext>(configure);
        services.AddDbContext<HobbyXpDbContext>(configure);
        return services;
    }

    /// <summary>Escritorio WPF: SQLite bajo LocalAppData (o HOBBYXP_DATA_DIR).</summary>
    public static IServiceCollection AddHobbyXpSqlite(this IServiceCollection services) =>
        services.AddHobbyXpData(options =>
            options.UseSqlite(DatabaseConstants.GetConnectionString()));

    /// <summary>Web / VPS: PostgreSQL.</summary>
    public static IServiceCollection AddHobbyXpPostgres(
        this IServiceCollection services,
        string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new ArgumentException("Connection string requerida.", nameof(connectionString));

        return services.AddHobbyXpData(options =>
            options.UseNpgsql(connectionString));
    }

    /// <summary>
    /// Aplica esquema e inicializa datos.
    /// SQLite: migraciones EF. PostgreSQL: EnsureCreated (baseline web) + seed/inicializadores.
    /// </summary>
    public static async Task EnsureHobbyXpDatabaseAsync(
        this IServiceProvider serviceProvider,
        CancellationToken cancellationToken = default)
    {
        await using var scope = serviceProvider.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<HobbyXpDbContext>();
        var provider = dbContext.Database.ProviderName ?? string.Empty;

        if (provider.Contains("Sqlite", StringComparison.OrdinalIgnoreCase))
        {
            await dbContext.Database.MigrateAsync(cancellationToken);
        }
        else
        {
            // Postgres (y otros): schema + HasData desde el modelo. Migraciones SQLite no aplican.
            await dbContext.Database.EnsureCreatedAsync(cancellationToken);
        }

        await HobbyXpDatabaseInitializer.EnsurePlayerProfileAsync(dbContext, cancellationToken);
        await HobbyXpDatabaseInitializer.EnsureGeometricLevelScaleAsync(dbContext, cancellationToken);
        await HobbyXpDatabaseInitializer.EnsureHobbyProgressRowsAsync(dbContext, cancellationToken);
        await HobbyXpDatabaseInitializer.EnsureHobbyXpBackfillAsync(dbContext, cancellationToken);
        await HobbyXpDatabaseInitializer.EnsureSpendableLedgerAsync(dbContext, cancellationToken);
        await HobbyXpDatabaseInitializer.EnsureHobbySpendableLedgerAsync(dbContext, cancellationToken);

        var weeklyQuota = scope.ServiceProvider.GetRequiredService<IWeeklyQuotaService>();
        await weeklyQuota.EvaluateClosedWeeksAsync(cancellationToken);
    }
}
