using HobbyXP.Helpers;
using HobbyXP.Models.Enums;
using HobbyXP.Models.Physical;
using HobbyXP.Services;
using HobbyXP.Tests.Helpers;
using Microsoft.EntityFrameworkCore;

namespace HobbyXP.Tests.Services;

public sealed class DietWeeklyQuotaTests : IDisposable
{
    private readonly TestDbContextFactory _factory;
    private readonly WeeklyQuotaService _sut;

    public DietWeeklyQuotaTests()
    {
        _factory = new TestDbContextFactory();
        var xp = new XpService(_factory, new FakeLevelUpMessenger());
        _sut = new WeeklyQuotaService(_factory, xp);
    }

    public void Dispose() => _factory.Dispose();

    [Fact]
    public async Task EvaluateClosedWeeks_WithoutDietLogs_DoesNotPenalizeDiet()
    {
        await using (var db = _factory.CreateDbContext())
        {
            var profile = await db.PlayerProfiles.SingleAsync();
            profile.WeeklyQuotaTrackingStartedAtUtc = DateTime.SpecifyKind(
                DateTime.Today.AddDays(-21),
                DateTimeKind.Utc);
            await db.SaveChangesAsync();
        }

        await _sut.EvaluateClosedWeeksAsync();

        await using (var db = _factory.CreateDbContext())
        {
            Assert.False(await db.WeeklyQuotaEvaluations.AnyAsync(e => e.SourceType == MilestoneSourceType.Diet));
        }
    }

    [Fact]
    public async Task GetCurrentWeekProgress_CountsGoodDaysOnly()
    {
        var weekStart = WeekDateHelper.GetWeekStartLocal(DateTime.Today);
        await using (var db = _factory.CreateDbContext())
        {
            db.DietDayLogs.Add(GoodDay(weekStart));
            db.DietDayLogs.Add(GoodDay(weekStart.AddDays(1)));
            db.DietDayLogs.Add(NotGoodDay(weekStart.AddDays(2)));
            await db.SaveChangesAsync();
        }

        var progress = await _sut.GetCurrentWeekProgressAsync();
        var diet = progress.Single(p => p.SourceType == MilestoneSourceType.Diet);

        Assert.Equal(5, diet.RequiredPrimary);
        Assert.Equal(2, diet.ActualPrimary);
        Assert.False(diet.IsMet);
    }

    private static DietDayLog GoodDay(DateTime localDate)
    {
        var log = new DietDayLog
        {
            DayDate = DateTimeHelper.ToUtcFromLocalDate(localDate),
            BreakfastStatus = DietMealStatus.OnPlan,
            LunchStatus = DietMealStatus.OnPlan,
            DinnerStatus = DietMealStatus.OnPlan,
            SnackStatus = DietMealStatus.OffPlan
        };
        log.RecalculateScore();
        return log;
    }

    private static DietDayLog NotGoodDay(DateTime localDate)
    {
        var log = new DietDayLog
        {
            DayDate = DateTimeHelper.ToUtcFromLocalDate(localDate),
            BreakfastStatus = DietMealStatus.OnPlan,
            LunchStatus = DietMealStatus.OffPlan,
            DinnerStatus = DietMealStatus.Unlogged,
            SnackStatus = DietMealStatus.Unlogged
        };
        log.RecalculateScore();
        return log;
    }
}
