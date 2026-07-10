using HobbyXP.Models.Achievements;
using HobbyXP.Models.Core;
using HobbyXP.Models.Enums;
using HobbyXP.Services;
using HobbyXP.Tests.Helpers;
using Microsoft.EntityFrameworkCore;

namespace HobbyXP.Tests.Services;

public sealed class XpServiceTests : IDisposable
{
    private readonly TestDbContextFactory _factory;
    private readonly FakeLevelUpMessenger _levelUpMessenger;
    private readonly XpService _sut;

    public XpServiceTests()
    {
        _factory = new TestDbContextFactory();
        _levelUpMessenger = new FakeLevelUpMessenger();
        _sut = new XpService(_factory, _levelUpMessenger);
    }

    public void Dispose() => _factory.Dispose();

    [Theory]
    [InlineData(AchievementActionType.RunningKilometer, 5, 50)]
    [InlineData(AchievementActionType.GymWorkoutSaved, 1, 25)]
    [InlineData(AchievementActionType.BookPageRead, 120, 120)]
    [InlineData(AchievementActionType.VideoGamePercent, 37.5, 375)]
    public async Task CalculatePointsAsync_UsesActiveRule(
        AchievementActionType actionType,
        decimal units,
        int expectedPoints)
    {
        var points = await _sut.CalculatePointsAsync(actionType, units);

        Assert.Equal(expectedPoints, points);
    }

    [Fact]
    public async Task CalculatePointsAsync_FlatBonusRule_ReturnsBonusOnly()
    {
        var points = await _sut.CalculatePointsAsync(AchievementActionType.ProgressiveOverload, 0m);

        Assert.Equal(150, points);
    }

    [Fact]
    public async Task CalculatePointsAsync_InactiveRule_ReturnsZero()
    {
        await using var db = _factory.CreateDbContext();
        var rule = await db.AchievementRules.SingleAsync(r => r.ActionType == AchievementActionType.RunningKilometer);
        rule.IsActive = false;
        await db.SaveChangesAsync();

        var points = await _sut.CalculatePointsAsync(AchievementActionType.RunningKilometer, 10m);

        Assert.Equal(0, points);
    }

    [Fact]
    public async Task AwardXpAsync_PersistsTransactionAndTotalXp()
    {
        var outcome = await _sut.AwardXpAsync(
            AchievementActionType.RunningKilometer,
            3m,
            "Carrera matutina",
            MilestoneSourceType.Running);

        Assert.Equal(30, outcome.AmountAwarded);
        Assert.Equal(30, outcome.NewTotalXp);
        Assert.False(outcome.LeveledUp);

        await using var db = _factory.CreateDbContext();
        var profile = await db.PlayerProfiles.SingleAsync();
        Assert.Equal(30, profile.TotalXp);

        var transaction = await db.XpTransactions.SingleAsync();
        Assert.Equal(30, transaction.Amount);
        Assert.Equal(AchievementActionType.RunningKilometer, transaction.ActionType);
    }

    [Fact]
    public async Task AwardXpAsync_WhenCrossingThreshold_LevelsUpAndPublishes()
    {
        await using (var db = _factory.CreateDbContext())
        {
            var profile = await db.PlayerProfiles.SingleAsync();
            profile.TotalXp = 980;
            profile.CurrentLevel = 1;
            await db.SaveChangesAsync();
        }

        var outcome = await _sut.AwardXpAsync(
            AchievementActionType.OfficialRaceCompleted,
            0m,
            "Primera carrera",
            MilestoneSourceType.Running,
            milestoneTitle: "Carrera oficial");

        Assert.True(outcome.LeveledUp);
        Assert.Equal(2, outcome.NewLevel);
        Assert.Equal(1480, outcome.NewTotalXp);
        Assert.Single(_levelUpMessenger.Published);
        Assert.Equal(2, _levelUpMessenger.Published[0].NewLevel);
    }

    [Fact]
    public async Task AwardFlatBonusAsync_UsesExplicitBonus()
    {
        var outcome = await _sut.AwardFlatBonusAsync(
            AchievementActionType.BookCompleted,
            75,
            "Bono manual",
            MilestoneSourceType.Book);

        Assert.Equal(75, outcome.AmountAwarded);
        Assert.Equal(75, outcome.NewTotalXp);
    }

    [Fact]
    public async Task AwardXpAsync_WithZeroPoints_DoesNotChangeProfile()
    {
        var outcome = await _sut.AwardXpAsync(
            AchievementActionType.RunningKilometer,
            0m,
            "Sin distancia",
            MilestoneSourceType.Running);

        Assert.Equal(0, outcome.AmountAwarded);
        Assert.Equal(0, outcome.NewTotalXp);

        await using var db = _factory.CreateDbContext();
        Assert.Empty(await db.XpTransactions.ToListAsync());
    }

    [Fact]
    public async Task TryDeductXpAsync_WhenInsufficient_ReturnsFalse()
    {
        var deducted = await _sut.TryDeductXpAsync(100, "Canje fallido");

        Assert.False(deducted);
    }

    [Fact]
    public async Task TryDeductXpAsync_WhenSufficient_DeductsAndRecalculatesLevel()
    {
        await using (var db = _factory.CreateDbContext())
        {
            var seededProfile = await db.PlayerProfiles.SingleAsync();
            seededProfile.TotalXp = 1500;
            seededProfile.CurrentLevel = 2;
            await db.SaveChangesAsync();
        }

        var deducted = await _sut.TryDeductXpAsync(600, "Canje de premio");

        Assert.True(deducted);

        await using var verifyDb = _factory.CreateDbContext();
        var profile = await verifyDb.PlayerProfiles.SingleAsync();
        Assert.Equal(900, profile.TotalXp);
        Assert.Equal(1, profile.CurrentLevel);

        var transaction = await verifyDb.XpTransactions.SingleAsync(t => t.Amount < 0);
        Assert.Equal(-600, transaction.Amount);
    }

    [Fact]
    public async Task RevokeXpForSourceAsync_RemovesEarnedXpMilestonesAndMedals()
    {
        await using (var db = _factory.CreateDbContext())
        {
            var profile = await db.PlayerProfiles.SingleAsync();
            profile.TotalXp = 200;
            profile.CurrentLevel = 1;

            db.XpTransactions.Add(new XpTransaction
            {
                PlayerProfileId = profile.Id,
                Amount = 200,
                ActionType = AchievementActionType.BookCompleted,
                Description = "Libro",
                SourceEntityType = nameof(Models.PersonalGrowth.Book),
                SourceEntityId = 42,
                EarnedAt = DateTime.UtcNow
            });

            db.Milestones.Add(new Milestone
            {
                Title = "Libro",
                PointsEarned = 200,
                SourceType = MilestoneSourceType.Book,
                SourceEntityId = 42,
                CompletedAt = DateTime.UtcNow
            });

            var medalDefinition = await db.MedalDefinitions.FirstAsync();
            db.EarnedMedals.Add(new EarnedMedal
            {
                MedalDefinitionId = medalDefinition.Id,
                SourceEntityType = nameof(Models.PersonalGrowth.Book),
                SourceEntityId = 42,
                EarnedAt = DateTime.UtcNow
            });

            await db.SaveChangesAsync();
        }

        var revoked = await _sut.RevokeXpForSourceAsync(
            MilestoneSourceType.Book,
            nameof(Models.PersonalGrowth.Book),
            42,
            "Eliminación de libro");

        Assert.Equal(200, revoked);

        await using var verifyDb = _factory.CreateDbContext();
        Assert.Equal(0, await verifyDb.PlayerProfiles.Select(p => p.TotalXp).SingleAsync());
        Assert.Empty(await verifyDb.XpTransactions.Where(t => t.Amount > 0).ToListAsync());
        Assert.Empty(await verifyDb.Milestones.ToListAsync());
        Assert.Empty(await verifyDb.EarnedMedals.ToListAsync());
    }

    [Fact]
    public async Task GetDailyXpForLastDaysAsync_GroupsPositiveTransactionsByDay()
    {
        var today = DateTime.UtcNow.Date;

        await using (var db = _factory.CreateDbContext())
        {
            var profile = await db.PlayerProfiles.SingleAsync();
            db.XpTransactions.AddRange(
                new XpTransaction
                {
                    PlayerProfileId = profile.Id,
                    Amount = 40,
                    ActionType = AchievementActionType.RunningKilometer,
                    Description = "Hoy",
                    EarnedAt = today.AddHours(10)
                },
                new XpTransaction
                {
                    PlayerProfileId = profile.Id,
                    Amount = 60,
                    ActionType = AchievementActionType.RunningKilometer,
                    Description = "Hoy tarde",
                    EarnedAt = today.AddHours(18)
                },
                new XpTransaction
                {
                    PlayerProfileId = profile.Id,
                    Amount = 25,
                    ActionType = AchievementActionType.GymWorkoutSaved,
                    Description = "Ayer",
                    EarnedAt = today.AddDays(-1).AddHours(9)
                },
                new XpTransaction
                {
                    PlayerProfileId = profile.Id,
                    Amount = -10,
                    ActionType = AchievementActionType.RewardRedeemed,
                    Description = "Canje",
                    EarnedAt = today
                });
            await db.SaveChangesAsync();
        }

        var points = await _sut.GetDailyXpForLastDaysAsync(3);

        Assert.Equal(3, points.Count);
        Assert.Equal(100, points[^1].TotalXp);
        Assert.Equal(25, points[^2].TotalXp);
        Assert.Equal(0, points[^3].TotalXp);
    }

    [Fact]
    public async Task GetDailyXpForLastDaysAsync_WhenDaysNotPositive_ReturnsEmpty()
    {
        var points = await _sut.GetDailyXpForLastDaysAsync(0);

        Assert.Empty(points);
    }
}
