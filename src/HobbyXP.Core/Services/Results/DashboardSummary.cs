namespace HobbyXP.Services.Results;

public sealed record DashboardSummary(
    LevelProgressInfo LevelProgress,
    IReadOnlyList<DailyXpPoint> WeeklyXp,
    IReadOnlyList<HobbyDistributionSlice> MonthlyHobbyDistribution,
    IReadOnlyList<LevelUpSuggestion> LevelUpSuggestions);
