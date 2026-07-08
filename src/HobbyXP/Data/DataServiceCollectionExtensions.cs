using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace HobbyXP.Data;

public static class DataServiceCollectionExtensions
{
    public static IServiceCollection AddHobbyXpData(this IServiceCollection services)
    {
        services.AddDbContextFactory<HobbyXpDbContext>(options =>
            options.UseSqlite(DatabaseConstants.GetConnectionString()));

        services.AddDbContext<HobbyXpDbContext>(options =>
            options.UseSqlite(DatabaseConstants.GetConnectionString()));

        return services;
    }

    public static async Task EnsureHobbyXpDatabaseAsync(this IServiceProvider serviceProvider, CancellationToken cancellationToken = default)
    {
        await using var scope = serviceProvider.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<HobbyXpDbContext>();
        await dbContext.Database.MigrateAsync(cancellationToken);
        await HobbyXpDatabaseInitializer.EnsurePlayerProfileAsync(dbContext, cancellationToken);
    }
}
