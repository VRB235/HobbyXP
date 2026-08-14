using HobbyXP.Helpers;
using HobbyXP.Models.Enums;
using HobbyXP.Services;
using HobbyXP.Services.Abstractions;
using HobbyXP.Tests.Helpers;
using Microsoft.EntityFrameworkCore;

namespace HobbyXP.Tests.Services;

public sealed class DietServiceTests : IDisposable
{
    private readonly TestDbContextFactory _factory;
    private readonly DietService _sut;

    public DietServiceTests()
    {
        _factory = new TestDbContextFactory();
        var xp = new XpService(_factory, new FakeLevelUpMessenger());
        var achievements = new AchievementEngineService(_factory);
        var quota = new WeeklyQuotaService(_factory, xp);
        _sut = new DietService(_factory, xp, achievements, quota);
    }

    public void Dispose() => _factory.Dispose();

    [Fact]
    public async Task SaveDayAsync_InsertsSingleRowAndAwardsMealXp()
    {
        var day = DateTime.Today;
        var result = await _sut.SaveDayAsync(new DietDayDraft(
            day,
            DietMealStatus.OnPlan,
            DietMealStatus.OnPlan,
            DietMealStatus.OnPlan,
            DietMealStatus.Unlogged,
            Notes: null));

        Assert.Equal(3, result.Value.OnPlanCount);
        Assert.Equal(45, result.Value.XpEarned);
        Assert.True(DietDayRules.IsGoodDay(result.Value));
        Assert.Contains(result.Events, e => e.MedalUnlocked == MedalCode.DietGoodDays1);

        await using var db = _factory.CreateDbContext();
        Assert.Equal(1, await db.DietDayLogs.CountAsync());
        var hobby = await db.HobbyProgresses.SingleAsync(h => h.SourceType == MilestoneSourceType.Diet);
        Assert.Equal(45, hobby.TotalXp);
    }

    [Fact]
    public async Task SaveDayAsync_SameDay_UpsertsWithoutDuplicating()
    {
        var day = DateTime.Today;
        await _sut.SaveDayAsync(new DietDayDraft(
            day,
            DietMealStatus.OnPlan,
            DietMealStatus.OnPlan,
            DietMealStatus.OnPlan,
            DietMealStatus.OnPlan,
            Notes: null));

        var updated = await _sut.SaveDayAsync(new DietDayDraft(
            day,
            DietMealStatus.OnPlan,
            DietMealStatus.OnPlan,
            DietMealStatus.OnPlan,
            DietMealStatus.OffPlan,
            Notes: null));

        Assert.Equal(3, updated.Value.OnPlanCount);
        Assert.Equal(45, updated.Value.XpEarned);
        Assert.True(DietDayRules.IsGoodDay(updated.Value));
        Assert.False(DietDayRules.IsPerfectDay(updated.Value));

        await using var db = _factory.CreateDbContext();
        Assert.Equal(1, await db.DietDayLogs.CountAsync());
        var hobby = await db.HobbyProgresses.SingleAsync(h => h.SourceType == MilestoneSourceType.Diet);
        Assert.Equal(45, hobby.TotalXp);
    }

    [Fact]
    public async Task SaveDayAsync_PerfectDay_AddsFlatBonus()
    {
        var result = await _sut.SaveDayAsync(new DietDayDraft(
            DateTime.Today,
            DietMealStatus.OnPlan,
            DietMealStatus.OnPlan,
            DietMealStatus.OnPlan,
            DietMealStatus.OnPlan,
            Notes: null));

        Assert.Equal(100, result.Value.XpEarned);
        Assert.Contains(result.Events, e => e.Title == "¡Día perfecto!");
    }

    [Fact]
    public async Task SaveDayAsync_AllUnlogged_Throws()
    {
        await Assert.ThrowsAsync<ArgumentException>(() => _sut.SaveDayAsync(new DietDayDraft(
            DateTime.Today,
            DietMealStatus.Unlogged,
            DietMealStatus.Unlogged,
            DietMealStatus.Unlogged,
            DietMealStatus.Unlogged,
            Notes: null)));
    }

    [Fact]
    public async Task DeleteDayAsync_RevokesXp()
    {
        var saved = await _sut.SaveDayAsync(new DietDayDraft(
            DateTime.Today,
            DietMealStatus.OnPlan,
            DietMealStatus.OnPlan,
            DietMealStatus.OnPlan,
            DietMealStatus.Unlogged,
            Notes: null));

        Assert.True(await _sut.DeleteDayAsync(saved.Value.Id));

        await using var db = _factory.CreateDbContext();
        Assert.Equal(0, await db.DietDayLogs.CountAsync());
        var hobby = await db.HobbyProgresses.SingleAsync(h => h.SourceType == MilestoneSourceType.Diet);
        Assert.Equal(0, hobby.TotalXp);
    }
}
