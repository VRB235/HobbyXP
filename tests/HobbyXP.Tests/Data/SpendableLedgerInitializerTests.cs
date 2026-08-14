using HobbyXP.Data;
using HobbyXP.Models.Achievements;
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
            profile.SpendableProgressBaselineApplied = false;

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
        Assert.True(profile.SpendableProgressBaselineApplied);
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
        Assert.True(again.SpendableProgressBaselineApplied);
    }

    [Fact]
    public async Task EnsureSpendableLedgerAsync_WhenBaselineMissing_ResetsProgressWithoutTouchingWallet()
    {
        await using var db = _factory.CreateDbContext();
        var profile = await db.PlayerProfiles.Include(p => p.HobbyProgresses).SingleAsync();
        profile.SpendableLedgerInitialized = true;
        profile.SpendableProgressBaselineApplied = false;
        profile.SpendableXp = 4000;
        profile.TotalXp = 1000;
        profile.CurrentLevel = 2;
        foreach (var hobby in profile.HobbyProgresses)
        {
            hobby.TotalXp = 500;
            hobby.CurrentLevel = 2;
        }

        await db.SaveChangesAsync();

        await HobbyXpDatabaseInitializer.EnsureSpendableLedgerAsync(db);

        var again = await db.PlayerProfiles.Include(p => p.HobbyProgresses).SingleAsync();
        Assert.Equal(4000, again.SpendableXp);
        Assert.Equal(0, again.TotalXp);
        Assert.Equal(1, again.CurrentLevel);
        Assert.True(again.SpendableProgressBaselineApplied);
        Assert.All(again.HobbyProgresses, h =>
        {
            Assert.Equal(0, h.TotalXp);
            Assert.Equal(1, h.CurrentLevel);
        });
    }

    [Fact]
    public async Task EnsureHobbyXpBackfillAsync_WhenLedgerInitialized_DoesNotRebuildFromHistory()
    {
        await using var db = _factory.CreateDbContext();
        var profile = await db.PlayerProfiles.Include(p => p.HobbyProgresses).SingleAsync();
        profile.SpendableLedgerInitialized = true;
        profile.SpendableProgressBaselineApplied = true;
        profile.SpendableXp = 4000;
        profile.TotalXp = 0;
        profile.CurrentLevel = 1;
        foreach (var hobby in profile.HobbyProgresses)
        {
            hobby.TotalXp = 0;
            hobby.CurrentLevel = 1;
        }

        db.XpTransactions.Add(new XpTransaction
        {
            PlayerProfileId = profile.Id,
            Amount = 900,
            ActionType = AchievementActionType.RunningKilometer,
            Description = "Histórico",
            SourceType = MilestoneSourceType.Running,
            IsGlobal = false,
            EarnedAt = DateTime.UtcNow.AddDays(-10)
        });
        await db.SaveChangesAsync();

        await HobbyXpDatabaseInitializer.EnsureHobbyXpBackfillAsync(db);

        var again = await db.PlayerProfiles.Include(p => p.HobbyProgresses).SingleAsync();
        Assert.Equal(0, again.TotalXp);
        Assert.Equal(4000, again.SpendableXp);
        Assert.Equal(0, again.HobbyProgresses.Single(h => h.SourceType == MilestoneSourceType.Running).TotalXp);
    }
}
