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
    public async Task AwardXpAsync_CreditsHobbyPoolNotGlobal()
    {
        var outcome = await _sut.AwardXpAsync(
            AchievementActionType.RunningKilometer,
            3m,
            "Carrera matutina",
            MilestoneSourceType.Running);

        Assert.Equal(30, outcome.AmountAwarded);
        Assert.Equal(30, outcome.NewTotalXp);
        Assert.False(outcome.LeveledUp);
        Assert.Equal(0, outcome.GlobalBonusAwarded);

        await using var db = _factory.CreateDbContext();
        var profile = await db.PlayerProfiles.SingleAsync();
        Assert.Equal(0, profile.TotalXp);
        Assert.Equal(30, profile.SpendableXp);

        var hobby = await db.HobbyProgresses.SingleAsync(h => h.SourceType == MilestoneSourceType.Running);
        Assert.Equal(30, hobby.TotalXp);

        var transaction = await db.XpTransactions.SingleAsync();
        Assert.Equal(30, transaction.Amount);
        Assert.False(transaction.IsGlobal);
        Assert.Equal(MilestoneSourceType.Running, transaction.SourceType);
    }

    [Fact]
    public async Task AwardXpAsync_WhenHobbyLevelsUp_AwardsGlobalMetaBonus()
    {
        await using (var db = _factory.CreateDbContext())
        {
            var profile = await db.PlayerProfiles.SingleAsync();
            db.HobbyProgresses.Add(new HobbyProgress
            {
                PlayerProfileId = profile.Id,
                SourceType = MilestoneSourceType.OfficialRace,
                CurrentLevel = 1,
                TotalXp = 600
            });
            await db.SaveChangesAsync();
        }

        var outcome = await _sut.AwardXpAsync(
            AchievementActionType.OfficialRaceCompleted,
            0m,
            "Primera carrera",
            MilestoneSourceType.OfficialRace,
            milestoneTitle: "Carrera oficial");

        Assert.True(outcome.LeveledUp);
        Assert.Equal(2, outcome.NewLevel);
        Assert.Equal(1100, outcome.NewTotalXp);
        Assert.Equal(1000, outcome.GlobalBonusAwarded);
        Assert.True(outcome.GlobalLeveledUp);
        Assert.Equal(2, outcome.NewGlobalLevel);
        Assert.Single(_levelUpMessenger.Published);
        Assert.Equal(2, _levelUpMessenger.Published[0].NewLevel);

        await using var verifyDb = _factory.CreateDbContext();
        var global = await verifyDb.PlayerProfiles.SingleAsync();
        Assert.Equal(1000, global.TotalXp);
        Assert.Equal(2, global.CurrentLevel);
        Assert.Equal(1500, global.SpendableXp); // 500 hobby award + 1000 meta bonus
    }

    [Fact]
    public async Task AwardFlatBonusAsync_UsesExplicitBonusOnHobby()
    {
        var outcome = await _sut.AwardFlatBonusAsync(
            AchievementActionType.BookCompleted,
            75,
            "Bono manual",
            MilestoneSourceType.Book);

        Assert.Equal(75, outcome.AmountAwarded);
        Assert.Equal(75, outcome.NewTotalXp);

        await using var db = _factory.CreateDbContext();
        var profile = await db.PlayerProfiles.SingleAsync();
        Assert.Equal(0, profile.TotalXp);
        Assert.Equal(75, profile.SpendableXp);
        Assert.Equal(75, (await db.HobbyProgresses.SingleAsync(h => h.SourceType == MilestoneSourceType.Book)).TotalXp);
    }

    [Fact]
    public async Task AwardXpAsync_WhenZeroPoints_DoesNotPersist()
    {
        await using (var db = _factory.CreateDbContext())
        {
            var rule = await db.AchievementRules.SingleAsync(r => r.ActionType == AchievementActionType.RunningKilometer);
            rule.IsActive = false;
            await db.SaveChangesAsync();
        }

        var outcome = await _sut.AwardXpAsync(
            AchievementActionType.RunningKilometer,
            10m,
            "Sin puntos",
            MilestoneSourceType.Running);

        Assert.Equal(0, outcome.AmountAwarded);
        Assert.Equal(0, outcome.NewTotalXp);

        await using var verifyDb = _factory.CreateDbContext();
        Assert.Empty(await verifyDb.XpTransactions.ToListAsync());
    }

    [Fact]
    public async Task TryDeductXpAsync_WhenInsufficient_ReturnsFalse()
    {
        var deducted = await _sut.TryDeductXpAsync(100, MilestoneSourceType.Gym, "Canje fallido");

        Assert.False(deducted);
    }

    [Fact]
    public async Task TryDeductXpAsync_WhenSufficient_DeductsSpendableOnly()
    {
        await using (var db = _factory.CreateDbContext())
        {
            var seededProfile = await db.PlayerProfiles.SingleAsync();
            seededProfile.TotalXp = 1500;
            seededProfile.CurrentLevel = 2;
            seededProfile.SpendableXp = 1500;

            db.HobbyProgresses.Add(new HobbyProgress
            {
                PlayerProfileId = seededProfile.Id,
                SourceType = MilestoneSourceType.Gym,
                CurrentLevel = 1,
                TotalXp = 0,
                SpendableXp = 1500
            });

            await db.SaveChangesAsync();
        }

        var deducted = await _sut.TryDeductXpAsync(600, MilestoneSourceType.Gym, "Canje de premio");

        Assert.True(deducted);

        await using var verifyDb = _factory.CreateDbContext();
        var profile = await verifyDb.PlayerProfiles.SingleAsync();
        Assert.Equal(1500, profile.TotalXp);
        Assert.Equal(2, profile.CurrentLevel);
        Assert.Equal(900, profile.SpendableXp);

        var hobby = await verifyDb.HobbyProgresses.SingleAsync(h => h.SourceType == MilestoneSourceType.Gym);
        Assert.Equal(900, hobby.SpendableXp);

        var transaction = await verifyDb.XpTransactions.SingleAsync(t => t.Amount < 0);
        Assert.Equal(-600, transaction.Amount);
        Assert.False(transaction.IsGlobal);
        Assert.Equal(MilestoneSourceType.Gym, transaction.SourceType);
    }

    [Fact]
    public async Task RevokeXpForSourceAsync_RemovesHobbyXpAndGlobalMetaIfLevelDrops()
    {
        await using (var db = _factory.CreateDbContext())
        {
            var profile = await db.PlayerProfiles.SingleAsync();
            profile.TotalXp = 1000;
            profile.CurrentLevel = 2;
            profile.SpendableXp = 2000; // 1000 hobby + 1000 meta ya acreditados al saldo

            db.HobbyProgresses.Add(new HobbyProgress
            {
                PlayerProfileId = profile.Id,
                SourceType = MilestoneSourceType.Running,
                CurrentLevel = 2,
                TotalXp = 1000
            });

            db.XpTransactions.Add(new XpTransaction
            {
                PlayerProfileId = profile.Id,
                Amount = 1000,
                ActionType = AchievementActionType.RunningKilometer,
                Description = "Sesión",
                SourceEntityType = "RunningSession",
                SourceEntityId = 7,
                SourceType = MilestoneSourceType.Running,
                IsGlobal = false,
                EarnedAt = DateTime.UtcNow
            });

            await db.SaveChangesAsync();
        }

        var revoked = await _sut.RevokeXpForSourceAsync(
            MilestoneSourceType.Running,
            "RunningSession",
            7,
            "Eliminado");

        Assert.Equal(1000, revoked);

        await using var verifyDb = _factory.CreateDbContext();
        var hobby = await verifyDb.HobbyProgresses.SingleAsync(h => h.SourceType == MilestoneSourceType.Running);
        Assert.Equal(0, hobby.TotalXp);
        Assert.Equal(1, hobby.CurrentLevel);

        var globalProfile = await verifyDb.PlayerProfiles.SingleAsync();
        Assert.Equal(0, globalProfile.TotalXp);
        Assert.Equal(1, globalProfile.CurrentLevel);
        Assert.Equal(0, globalProfile.SpendableXp);
    }

    [Fact]
    public async Task GetHobbyProgressAsync_ReturnsHobbySnapshot()
    {
        await _sut.AwardXpAsync(
            AchievementActionType.PuzzleCompleted,
            1m,
            "Puzzle",
            MilestoneSourceType.Puzzle);

        var progress = await _sut.GetHobbyProgressAsync(MilestoneSourceType.Puzzle);

        Assert.Equal(1, progress.CurrentLevel);
        Assert.Equal(50, progress.TotalXp);
    }

    [Fact]
    public async Task GetAllHobbyProgressAsync_ReturnsAllTrackedHobbies()
    {
        var all = await _sut.GetAllHobbyProgressAsync();

        Assert.Equal(9, all.Count);
        Assert.Contains(all, h => h.SourceType == MilestoneSourceType.Gym);
        Assert.Contains(all, h => h.SourceType == MilestoneSourceType.Diet);
    }
}
