using HobbyXP.Helpers;
using HobbyXP.Models.Enums;
using HobbyXP.Models.PersonalGrowth;
using HobbyXP.Services;
using HobbyXP.Tests.Helpers;
using Microsoft.EntityFrameworkCore;

namespace HobbyXP.Tests.Services;

public sealed class AchievementHubTests : IDisposable
{
    private readonly TestDbContextFactory _factory;
    private readonly AchievementProgressService _progress;
    private readonly WeeklyQuotaService _quota;

    public AchievementHubTests()
    {
        _factory = new TestDbContextFactory();
        var medals = new MedalService(_factory);
        _progress = new AchievementProgressService(_factory, medals);
        var xp = new XpService(_factory, new FakeLevelUpMessenger());
        _quota = new WeeklyQuotaService(_factory, xp);
    }

    public void Dispose() => _factory.Dispose();

    [Theory]
    [InlineData(1, 50)]
    [InlineData(5, 50)]
    [InlineData(10, 100)]
    public void SpendableBonus_MatchesThresholdFloor(int threshold, int expected) =>
        Assert.Equal(expected, MedalPrivilegeRules.GetSpendableBonus(threshold));

    [Theory]
    [InlineData(300, 1, 300)]
    [InlineData(300, 2, 600)]
    [InlineData(300, 0, 300)]
    public void EffectiveCost_ScalesWithLevel(int baseCost, int level, int expected) =>
        Assert.Equal(expected, RewardCostCalculator.GetEffectiveCost(baseCost, level));

    [Fact]
    public async Task GetNextMedal_ForNewBook_PointsToFirstBookMedal()
    {
        var next = await _progress.GetNextMedalAsync(MilestoneSourceType.Book);
        Assert.NotNull(next);
        Assert.Equal(MedalCode.BookCompleted, next!.Code);
        Assert.Equal(1, next.Threshold);
        Assert.Equal(0, next.CurrentCount);
    }

    [Fact]
    public async Task GetUnseenMedalCount_IncreasesUntilMarkedSeen()
    {
        await using (var db = _factory.CreateDbContext())
        {
            db.Books.Add(new Book
            {
                Title = "Dune",
                Author = "Herbert",
                TotalPages = 100,
                PagesRead = 100,
                Status = BookStatus.Completed,
                CompletedAt = DateTime.UtcNow
            });
            await db.SaveChangesAsync();
        }

        var engine = new AchievementEngineService(_factory);
        await engine.TryAwardMedalAsync(MedalCode.BookCompleted, MilestoneSourceType.Book);

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
