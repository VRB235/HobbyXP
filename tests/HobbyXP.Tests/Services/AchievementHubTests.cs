using HobbyXP.Helpers;
using HobbyXP.Models.Enums;
using HobbyXP.Services;
using HobbyXP.Tests.Helpers;
using Microsoft.EntityFrameworkCore;

namespace HobbyXP.Tests.Services;

public sealed class AchievementHubTests : IDisposable
{
    private readonly TestDbContextFactory _factory;
    private readonly AchievementProgressService _progress;
    private readonly MedalService _medals;
    private readonly WeeklyQuotaService _quota;

    public AchievementHubTests()
    {
        _factory = new TestDbContextFactory();
        var engine = new AchievementEngineService(_factory);
        _medals = new MedalService(_factory, engine);
        _progress = new AchievementProgressService(_factory, _medals);
        var xp = new XpService(_factory, new FakeLevelUpMessenger());
        _quota = new WeeklyQuotaService(_factory, xp);
    }

    public void Dispose() => _factory.Dispose();

    [Theory]
    [InlineData(50, 50)]
    [InlineData(1000, 50)]
    [InlineData(2000, 100)]
    [InlineData(20000, 500)]
    public void SpendableBonus_ScalesWithXpThreshold(int xpThreshold, int expected) =>
        Assert.Equal(expected, MedalPrivilegeRules.GetSpendableBonus(xpThreshold));

    [Theory]
    [InlineData(300, 1, 300)]
    [InlineData(300, 2, 600)]
    [InlineData(300, 0, 300)]
    public void EffectiveCost_ScalesWithLevel(int baseCost, int level, int expected) =>
        Assert.Equal(expected, RewardCostCalculator.GetEffectiveCost(baseCost, level));

    [Fact]
    public async Task GetNextMedal_ForNewBook_PointsToLowestXpThreshold()
    {
        await _medals.CreateAsync(
            MilestoneSourceType.Book,
            100,
            "Lector Voraz",
            "100 XP en libros.",
            "Lee hasta 100 XP.");
        await _medals.CreateAsync(
            MilestoneSourceType.Book,
            500,
            "Biblioteca Viva",
            "500 XP en libros.",
            "Sigue leyendo.");

        var next = await _progress.GetNextMedalAsync(MilestoneSourceType.Book);
        Assert.NotNull(next);
        Assert.Equal("Lector Voraz", next!.Name);
        Assert.Equal(100, next.XpThreshold);
        Assert.Equal(0, next.CurrentXp);
    }

    [Fact]
    public async Task GetUnseenMedalCount_IncreasesUntilMarkedSeen()
    {
        var medal = await _medals.CreateAsync(
            MilestoneSourceType.Book,
            50,
            "Primer Capítulo",
            "50 XP en libros.",
            "Lee un poco.");

        await using (var db = _factory.CreateDbContext())
            await TestHobbyProgress.SetTotalXpAsync(db, MilestoneSourceType.Book, 50);

        var engine = new AchievementEngineService(_factory);
        await engine.TryAwardHobbyXpMedalsAsync(MilestoneSourceType.Book);
        Assert.True(await engine.IsMedalEarnedAsync(medal.Id));

        Assert.Equal(1, await _progress.GetUnseenMedalCountAsync());
        await _progress.MarkMedalsSeenAsync();
        Assert.Equal(0, await _progress.GetUnseenMedalCountAsync());
    }

    [Fact]
    public async Task EvaluateClosedWeeks_WithImmunity_WaivesPenalty()
    {
        await using (var db = _factory.CreateDbContext())
        {
            var profile = await db.PlayerProfiles.SingleAsync();
            profile.WeeklyQuotaTrackingStartedAtUtc = DateTime.SpecifyKind(
                DateTime.Today.AddDays(-14),
                DateTimeKind.Utc);
            profile.DisciplineImmunityUntilUtc = DateTime.UtcNow.AddDays(7);
            await db.SaveChangesAsync();
        }

        await _quota.EvaluateClosedWeeksAsync();

        await using (var db = _factory.CreateDbContext())
        {
            var evals = await db.WeeklyQuotaEvaluations.ToListAsync();
            Assert.NotEmpty(evals);
            Assert.DoesNotContain(evals, e => e.Status == WeeklyQuotaStatus.Penalized);
            Assert.Contains(evals, e => e.Status == WeeklyQuotaStatus.Waived);
        }
    }
}
