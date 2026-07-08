using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace HobbyXP.Data;

/// <summary>
/// Factory de diseño para migraciones EF Core desde la CLI.
/// </summary>
public sealed class HobbyXpDesignTimeDbContextFactory : IDesignTimeDbContextFactory<HobbyXpDbContext>
{
    public HobbyXpDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<HobbyXpDbContext>();
        optionsBuilder.UseSqlite(DatabaseConstants.GetConnectionString());

        return new HobbyXpDbContext(optionsBuilder.Options);
    }
}
