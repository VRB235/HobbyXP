using HobbyXP.Data;
using HobbyXP.Models.Core;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace HobbyXP.Tests.Helpers;

public sealed class TestDbContextFactory : IDbContextFactory<HobbyXpDbContext>, IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<HobbyXpDbContext> _options;

    public TestDbContextFactory(Action<HobbyXpDbContext>? seed = null)
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        _options = new DbContextOptionsBuilder<HobbyXpDbContext>()
            .UseSqlite(_connection)
            .Options;

        using var db = CreateDbContext();
        db.Database.EnsureCreated();

        if (!db.PlayerProfiles.Any())
        {
            db.PlayerProfiles.Add(new PlayerProfile
            {
                CurrentLevel = 1,
                TotalXp = 0,
                SpendableXp = 0,
                SpendableLedgerInitialized = true,
                SpendableProgressBaselineApplied = true,
                BaseXpPerLevel = 1000
            });
            db.SaveChanges();
        }

        seed?.Invoke(db);
        db.SaveChanges();
    }

    public HobbyXpDbContext CreateDbContext() => new(_options);

    public Task<HobbyXpDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(CreateDbContext());
    }

    public void Dispose() => _connection.Dispose();
}
