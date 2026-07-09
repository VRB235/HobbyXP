using HobbyXP.Data;
using HobbyXP.Models.Enums;
using HobbyXP.Services.Abstractions;
using HobbyXP.Services.Results;
using Microsoft.EntityFrameworkCore;

namespace HobbyXP.Services;

public sealed class DashboardService : IDashboardService
{
    private readonly IDbContextFactory<HobbyXpDbContext> _dbContextFactory;
    private readonly IXpService _xpService;
    private readonly IPlayerProfileService _playerProfileService;
    private readonly IAchievementEngineService _achievementEngineService;

    public DashboardService(
        IDbContextFactory<HobbyXpDbContext> dbContextFactory,
        IXpService xpService,
        IPlayerProfileService playerProfileService,
        IAchievementEngineService achievementEngineService)
    {
        _dbContextFactory = dbContextFactory;
        _xpService = xpService;
        _playerProfileService = playerProfileService;
        _achievementEngineService = achievementEngineService;
    }

    public async Task<DashboardSummary> GetSummaryAsync(CancellationToken cancellationToken = default)
    {
        var levelProgress = await _playerProfileService.GetLevelProgressAsync(cancellationToken);
        var weeklyXp = await _xpService.GetDailyXpForLastDaysAsync(7, cancellationToken);
        var monthlyDistribution = await GetMonthlyHobbyDistributionAsync(cancellationToken);
        var milestones = await GetRecentMilestonesAsync(cancellationToken);
        var rules = await _achievementEngineService.GetAllRulesAsync(cancellationToken);
        var xpRemaining = Math.Max(0, levelProgress.XpRequiredForNextLevel - levelProgress.XpIntoCurrentLevel);
        var suggestions = LevelUpSuggestionBuilder.Build(xpRemaining, monthlyDistribution, rules);

        return new DashboardSummary(levelProgress, weeklyXp, monthlyDistribution, milestones, suggestions);
    }

    private async Task<IReadOnlyList<HobbyDistributionSlice>> GetMonthlyHobbyDistributionAsync(
        CancellationToken cancellationToken)
    {
        await using var db = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        var monthStart = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1, 0, 0, 0, DateTimeKind.Utc);

        var grouped = await db.Milestones
            .AsNoTracking()
            .Where(m => m.CompletedAt >= monthStart)
            .GroupBy(m => m.SourceType)
            .Select(g => new { SourceType = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        var total = grouped.Sum(x => x.Count);
        if (total == 0)
            return Array.Empty<HobbyDistributionSlice>();

        return grouped
            .OrderByDescending(x => x.Count)
            .Select(x => new HobbyDistributionSlice(
                x.SourceType,
                GetCategoryLabel(x.SourceType),
                x.Count,
                Math.Round(x.Count * 100d / total, 1)))
            .ToList();
    }

    private async Task<IReadOnlyList<Models.Core.Milestone>> GetRecentMilestonesAsync(
        CancellationToken cancellationToken)
    {
        await using var db = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        return await db.Milestones
            .AsNoTracking()
            .OrderByDescending(m => m.CompletedAt)
            .Take(20)
            .ToListAsync(cancellationToken);
    }

    private static string GetCategoryLabel(MilestoneSourceType sourceType) => sourceType switch
    {
        MilestoneSourceType.Running => "Running",
        MilestoneSourceType.Gym => "Gimnasio",
        MilestoneSourceType.Puzzle => "Rompecabezas",
        MilestoneSourceType.Media => "Series y películas",
        MilestoneSourceType.VideoGame => "Videojuegos",
        MilestoneSourceType.Book => "Libros",
        MilestoneSourceType.Course => "Cursos",
        MilestoneSourceType.OfficialRace => "Carreras oficiales",
        MilestoneSourceType.Reward => "Recompensas",
        _ => "Sistema"
    };
}
