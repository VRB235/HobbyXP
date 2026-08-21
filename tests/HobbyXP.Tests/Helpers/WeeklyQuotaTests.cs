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
    public void Media_RequiresCompletedSeriesAndMovies()
    {
        var (primary, secondary) = WeeklyQuotaRules.GetRequired(Models.Enums.MilestoneSourceType.Media);
        Assert.Equal(1, primary);
        Assert.Equal(2, secondary);
        Assert.False(WeeklyQuotaRules.IsMet(1, 1, 2, 1));
        Assert.True(WeeklyQuotaRules.IsMet(1, 1, 2, 2));
        Assert.Contains("serie terminada", WeeklyQuotaRules.FormatRequirement(Models.Enums.MilestoneSourceType.Media), StringComparison.OrdinalIgnoreCase);
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

    [Fact]
    public void Course_RequiresFiveSessions()
    {
        var (primary, secondary) = WeeklyQuotaRules.GetRequired(Models.Enums.MilestoneSourceType.Course);
        Assert.Equal(5, primary);
        Assert.Equal(0, secondary);
    }

    [Fact]
    public void Book_WeeklyRequiresOneCompletedBook()
    {
        var (primary, secondary) = WeeklyQuotaRules.GetRequired(Models.Enums.MilestoneSourceType.Book);
        Assert.Equal(1, primary);
        Assert.Equal(0, secondary);
        Assert.Contains("libro terminado", WeeklyQuotaRules.FormatRequirement(Models.Enums.MilestoneSourceType.Book), StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(1, 1)]
    [InlineData(100, 20)]
    [InlineData(101, 21)]
    [InlineData(4, 1)]
    public void Book_RequiredPages_IsTwentyPercentCeiling(int totalPages, int expected)
    {
        Assert.Equal(expected, WeeklyQuotaRules.GetBookRequiredPages(totalPages));
        Assert.Equal(expected, DailyQuotaRules.GetBookRequiredPages(totalPages));
    }

    [Fact]
    public void Book_CompletingBook_MeetsDailyQuotaEvenIfPagesBelowTwentyPercent()
    {
        Assert.True(DailyQuotaRules.IsBookQuotaMet(100, 10, completedBookToday: true));
        Assert.False(DailyQuotaRules.IsBookQuotaMet(100, 10, completedBookToday: false));
        Assert.True(DailyQuotaRules.IsBookQuotaMet(100, 100, completedBookToday: false));
    }

    [Fact]
    public void Book_PageBank_ExcessCoversFollowingDays()
    {
        // 164 con cuota 82 ⇒ lunes y martes cubiertos; miércoles no.
        var pages = new[] { 164, 0, 0 };
        Assert.True(DailyQuotaRules.IsBookDayMetByPageBank(82, pages, 0));
        Assert.True(DailyQuotaRules.IsBookDayMetByPageBank(82, pages, 1));
        Assert.False(DailyQuotaRules.IsBookDayMetByPageBank(82, pages, 2));
    }

    [Fact]
    public void Book_PageBank_DoesNotBackfillMissedDay()
    {
        // Lunes/martes fallidos; el exceso del miércoles no los salva.
        var pages = new[] { 40, 0, 164 };
        Assert.False(DailyQuotaRules.IsBookDayMetByPageBank(82, pages, 0));
        Assert.False(DailyQuotaRules.IsBookDayMetByPageBank(82, pages, 1));
        Assert.True(DailyQuotaRules.IsBookDayMetByPageBank(82, pages, 2));
        // Crédito restante (40+164-82=122) cubre el jueves.
        Assert.True(DailyQuotaRules.IsBookDayMetByPageBank(82, [40, 0, 164, 0], 3));
        Assert.False(DailyQuotaRules.IsBookDayMetByPageBank(82, [40, 0, 164, 0, 0], 4));
    }

    [Fact]
    public void Book_PageBank_AccumulatesAcrossDaysTowardOneQuota()
    {
        var pages = new[] { 40, 42 };
        Assert.False(DailyQuotaRules.IsBookDayMetByPageBank(82, pages, 0));
        Assert.True(DailyQuotaRules.IsBookDayMetByPageBank(82, pages, 1));
    }

    [Fact]
    public void Daily_RunningGymCourse_RequireOneSession()
    {
        Assert.Equal(1, DailyQuotaRules.GetRequiredPrimary(Models.Enums.MilestoneSourceType.Running));
        Assert.Equal(1, DailyQuotaRules.GetRequiredPrimary(Models.Enums.MilestoneSourceType.Gym));
        Assert.Equal(1, DailyQuotaRules.GetRequiredPrimary(Models.Enums.MilestoneSourceType.Course));
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
