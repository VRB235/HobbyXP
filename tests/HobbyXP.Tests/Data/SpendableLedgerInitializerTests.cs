using HobbyXP.Data;
using HobbyXP.Models.Core;
using HobbyXP.Models.Enums;
using HobbyXP.Tests.Helpers;
using Microsoft.EntityFrameworkCore;

namespace HobbyXP.Tests.Data;

public sealed class SpendableLedgerInitializerTests : IDisposable
{
    private readonly TestDbContextFactory _factory;

    public SpendableLedgerInitializerTests()
    {
        _factory = new TestDbContextFactory(db =>
        {
            var profile = db.PlayerProfiles.Single();
            profile.TotalXp = 2500;
            profile.CurrentLevel = 3;
            profile.SpendableXp = 0;
            profile.SpendableLedgerInitialized = false;

            db.HobbyProgresses.AddRange(
                new HobbyProgress
                {
                    PlayerProfileId = profile.Id,
                    SourceType = MilestoneSourceType.Running,
                    CurrentLevel = 2,
                    TotalXp = 1200
                },
                new HobbyProgress
                {
                    PlayerProfileId = profile.Id,
                    SourceType = MilestoneSourceType.Gym,
                    CurrentLevel = 1,
                    TotalXp = 300
                });
        });
    }

    public void Dispose() => _factory.Dispose();

    [Fact]
    public async Task EnsureSpendableLedgerAsync_ConvertsProgressToWalletAndResetsLevels()
    {
        await using var db = _factory.CreateDbContext();
        await HobbyXpDatabaseInitializer.EnsureSpendableLedgerAsync(db);

        var profile = await db.PlayerProfiles
            .Include(p => p.HobbyProgresses)
            .SingleAsync();

        Assert.True(profile.SpendableLedgerInitialized);
        Assert.Equal(4000, profile.SpendableXp); // 1200 + 300 + 2500
        Assert.Equal(0, profile.TotalXp);
        Assert.Equal(1, profile.CurrentLevel);
        Assert.All(profile.HobbyProgresses, h =>
        {
            Assert.Equal(0, h.TotalXp);
            Assert.Equal(1, h.CurrentLevel);
        });
    }

    [Fact]
    public async Task EnsureSpendableLedgerAsync_SecondRun_IsNoOp()
    {
        await using var db = _factory.CreateDbContext();
        await HobbyXpDatabaseInitializer.EnsureSpendableLedgerAsync(db);

        var profile = await db.PlayerProfiles.SingleAsync();
        profile.SpendableXp = 50;
        profile.TotalXp = 100;
        profile.CurrentLevel = 1;
        await db.SaveChangesAsync();

        await HobbyXpDatabaseInitializer.EnsureSpendableLedgerAsync(db);

        var again = await db.PlayerProfiles.SingleAsync();
        Assert.Equal(50, again.SpendableXp);
        Assert.Equal(100, again.TotalXp);
        Assert.True(again.SpendableLedgerInitialized);
    }
}
