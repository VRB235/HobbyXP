using HobbyXP.Helpers;
using HobbyXP.Models.Achievements;
using HobbyXP.Models.Enums;
using HobbyXP.Services;
using HobbyXP.Tests.Helpers;
using Microsoft.EntityFrameworkCore;

namespace HobbyXP.Tests.Services;

public sealed class AchievementEngineServiceTests : IDisposable
{
    private readonly TestDbContextFactory _factory;
    private readonly AchievementEngineService _sut;
    private readonly MedalService _medals;

    public AchievementEngineServiceTests()
    {
        _factory = new TestDbContextFactory();
        _sut = new AchievementEngineService(_factory);
        _medals = new MedalService(_factory, _sut);
    }

    public void Dispose() => _factory.Dispose();

    [Fact]
    public async Task GetActiveRuleAsync_ReturnsSeededRule()
    {
        var rule = await _sut.GetActiveRuleAsync(AchievementActionType.RunningKilometer);

        Assert.NotNull(rule);
        Assert.Equal(10m, rule!.PointsPerUnit);
        Assert.True(rule.IsActive);
    }

    [Fact]
    public async Task UpdateRuleAsync_PersistsChanges()
    {
        var existing = await _sut.GetActiveRuleAsync(AchievementActionType.PuzzleCompleted);
        Assert.NotNull(existing);

        existing!.DisplayName = "Puzzle épico";
        existing.PointsPerUnit = 75m;
        existing.FlatBonusPoints = 10;

        var updated = await _sut.UpdateRuleAsync(existing);

        Assert.Equal("Puzzle épico", updated.DisplayName);
        Assert.Equal(75m, updated.PointsPerUnit);
        Assert.Equal(10, updated.FlatBonusPoints);

        await using var db = _factory.CreateDbContext();
        var stored = await db.AchievementRules.SingleAsync(r => r.Id == existing.Id);
        Assert.Equal(75m, stored.PointsPerUnit);
    }

    [Fact]
    public async Task TryAwardHobbyXpMedalsAsync_WhenThresholdMet_GrantsMedalOnce()
    {
        var medal = await _medals.CreateAsync(
            MilestoneSourceType.Book,
            xpThreshold: 100,
            name: "Lector Voraz",
            description: "Alcanza 100 XP en libros.",
            unlockHint: "Lee páginas hasta sumar 100 XP.");

        await using (var db = _factory.CreateDbContext())
            await TestHobbyProgress.SetTotalXpAsync(db, MilestoneSourceType.Book, 100);

        var first = await _sut.TryAwardHobbyXpMedalsAsync(MilestoneSourceType.Book);
        var second = await _sut.TryAwardHobbyXpMedalsAsync(MilestoneSourceType.Book);

        Assert.Single(first);
        Assert.Equal(medal.Id, first[0].MedalDefinitionId);
        Assert.Empty(second);
        Assert.True(await _sut.IsMedalEarnedAsync(medal.Id));

        await using var verifyDb = _factory.CreateDbContext();
        Assert.Single(await verifyDb.EarnedMedals.ToListAsync());
        var profileAfter = await verifyDb.PlayerProfiles.SingleAsync();
        Assert.Equal("Lector Voraz", profileAfter.HonorTitle);
        Assert.Equal(MedalPrivilegeRules.GetSpendableBonus(100), profileAfter.SpendableXp);
        Assert.NotNull(profileAfter.DisciplineImmunityUntilUtc);
        Assert.True(profileAfter.DisciplineImmunityUntilUtc > DateTime.UtcNow);
    }

    [Fact]
    public async Task TryAwardHobbyXpMedalsAsync_AwardsMultipleThresholdsInOnePass()
    {
        await _medals.CreateAsync(
            MilestoneSourceType.Puzzle,
            50,
            "Pieza Inicial",
            "50 XP en puzzles.",
            "Suma XP en rompecabezas.");
        await _medals.CreateAsync(
            MilestoneSourceType.Puzzle,
            200,
            "Pieza Maestra",
            "200 XP en puzzles.",
            "Sigue resolviendo.");

        await using (var db = _factory.CreateDbContext())
            await TestHobbyProgress.SetTotalXpAsync(db, MilestoneSourceType.Puzzle, 250);

        var events = await _sut.TryAwardHobbyXpMedalsAsync(MilestoneSourceType.Puzzle);

        Assert.Equal(2, events.Count);
        Assert.All(events, e => Assert.True(e.IsMedalUnlock));
    }
}
