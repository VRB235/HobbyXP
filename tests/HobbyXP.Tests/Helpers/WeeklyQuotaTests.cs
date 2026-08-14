using HobbyXP.Helpers;
using HobbyXP.Services.Internal;

namespace HobbyXP.Tests.Helpers;

public sealed class WeekDateHelperTests
{
    [Fact]
    public void GetWeekStartLocal_ReturnsMonday()
    {
        var wednesday = new DateTime(2026, 8, 12); // miércoles
        var monday = WeekDateHelper.GetWeekStartLocal(wednesday);
        Assert.Equal(new DateTime(2026, 8, 10), monday);
        Assert.Equal(DayOfWeek.Monday, monday.DayOfWeek);
    }

    [Fact]
    public void IsClosedWeek_CurrentWeek_IsFalse()
    {
        var today = new DateTime(2026, 8, 13);
        var weekStart = WeekDateHelper.GetWeekStartLocal(today);
        Assert.False(WeekDateHelper.IsClosedWeek(weekStart, today));
    }

    [Fact]
    public void IsClosedWeek_PreviousWeek_IsTrue()
    {
        var today = new DateTime(2026, 8, 13);
        var previous = WeekDateHelper.GetWeekStartLocal(today).AddDays(-7);
        Assert.True(WeekDateHelper.IsClosedWeek(previous, today));
    }
}

public sealed class WeeklyQuotaRulesTests
{
    [Fact]
    public void Media_RequiresSeriesAndMovies()
    {
        var (primary, secondary) = WeeklyQuotaRules.GetRequired(Models.Enums.MilestoneSourceType.Media);
        Assert.Equal(1, primary);
        Assert.Equal(2, secondary);
        Assert.False(WeeklyQuotaRules.IsMet(1, 1, 2, 1));
        Assert.True(WeeklyQuotaRules.IsMet(1, 1, 2, 2));
    }

    [Fact]
    public void Diet_RequiresFiveGoodDays()
    {
        var (primary, secondary) = WeeklyQuotaRules.GetRequired(Models.Enums.MilestoneSourceType.Diet);
        Assert.Equal(5, primary);
        Assert.Equal(0, secondary);
        Assert.False(WeeklyQuotaRules.IsMet(5, 4, 0, 0));
        Assert.True(WeeklyQuotaRules.IsMet(5, 5, 0, 0));
    }
}

public sealed class HobbyLevelDownXpTests
{
    [Theory]
    [InlineData(3, 3500, 1000, 501)] // threshold L3=3000 → target 2999
    [InlineData(2, 1500, 1000, 501)] // threshold L2=1000 → target 999
    [InlineData(1, 400, 1000, 400)]  // piso: resetea progreso
    [InlineData(1, 0, 1000, 0)]
    public void LevelDownXp_MatchesExpected(int level, int totalXp, int baseXp, int expectedRevoke)
    {
        var threshold = XpLevelCalculator.GetXpThresholdForLevel(level, baseXp);
        var targetXp = level <= 1
            ? 0
            : Math.Max(0, (int)Math.Min(int.MaxValue, threshold) - 1);
        var revoke = Math.Max(0, totalXp - targetXp);
        Assert.Equal(expectedRevoke, revoke);
    }
}
