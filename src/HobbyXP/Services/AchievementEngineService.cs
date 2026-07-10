using HobbyXP.Data;
using HobbyXP.Models.Achievements;
using HobbyXP.Models.Enums;
using HobbyXP.Models.PersonalGrowth;
using HobbyXP.Services.Abstractions;
using HobbyXP.Services.Results;
using Microsoft.EntityFrameworkCore;

namespace HobbyXP.Services;

public sealed class AchievementEngineService : IAchievementEngineService
{
    private readonly IDbContextFactory<HobbyXpDbContext> _dbContextFactory;

    public AchievementEngineService(IDbContextFactory<HobbyXpDbContext> dbContextFactory)
    {
        _dbContextFactory = dbContextFactory;
    }

    public async Task<AchievementRule?> GetActiveRuleAsync(
        AchievementActionType actionType,
        CancellationToken cancellationToken = default)
    {
        await using var db = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        return await db.AchievementRules
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.IsActive && r.ActionType == actionType, cancellationToken);
    }

    public async Task<IReadOnlyList<AchievementRule>> GetAllRulesAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        return await db.AchievementRules
            .AsNoTracking()
            .OrderBy(r => r.ActionType)
            .ToListAsync(cancellationToken);
    }

    public async Task<AchievementRule> UpdateRuleAsync(
        AchievementRule rule,
        CancellationToken cancellationToken = default)
    {
        await using var db = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        var existing = await db.AchievementRules.FindAsync([rule.Id], cancellationToken)
            ?? throw new InvalidOperationException($"No se encontró la regla con Id {rule.Id}.");

        existing.DisplayName = rule.DisplayName;
        existing.UnitLabel = rule.UnitLabel;
        existing.PointsPerUnit = rule.PointsPerUnit;
        existing.FlatBonusPoints = rule.FlatBonusPoints;
        existing.IsActive = rule.IsActive;
        existing.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(cancellationToken);
        return existing;
    }

    public async Task<bool> IsMedalEarnedAsync(MedalCode code, CancellationToken cancellationToken = default)
    {
        await using var db = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        return await db.EarnedMedals
            .AsNoTracking()
            .AnyAsync(m => m.MedalDefinition!.Code == code, cancellationToken);
    }

    public async Task<AchievementEvent?> TryAwardMedalAsync(
        MedalCode code,
        MilestoneSourceType sourceType,
        string? sourceEntityType = null,
        int? sourceEntityId = null,
        CancellationToken cancellationToken = default)
    {
        var entry = MedalCatalog.Entries.FirstOrDefault(e => e.Code == code);
        if (entry is null)
            return null;

        var events = await TryAwardMilestonesForTrackAsync(
            entry.Track,
            sourceType,
            sourceEntityType,
            sourceEntityId,
            cancellationToken);

        return events.FirstOrDefault(e => e.MedalUnlocked == code);
    }

    public async Task<IReadOnlyList<AchievementEvent>> TryAwardMilestonesForTrackAsync(
        MedalMilestoneTrack track,
        MilestoneSourceType sourceType,
        string? sourceEntityType = null,
        int? sourceEntityId = null,
        CancellationToken cancellationToken = default)
    {
        await using var db = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        var currentCount = await ResolveTrackCountAsync(db, track, cancellationToken);
        var trackCodes = MedalCatalog.ForTrack(track).Select(e => e.Code).ToList();

        var definitions = await db.MedalDefinitions
            .Where(m => trackCodes.Contains(m.Code))
            .ToListAsync(cancellationToken);

        var earnedDefinitionIds = await db.EarnedMedals
            .Select(m => m.MedalDefinitionId)
            .ToListAsync(cancellationToken);

        var events = new List<AchievementEvent>();
        foreach (var spec in MedalCatalog.ForTrack(track).Where(s => s.Threshold <= currentCount).OrderBy(s => s.Threshold))
        {
            var definition = definitions.FirstOrDefault(d => d.Code == spec.Code);
            if (definition is null || earnedDefinitionIds.Contains(definition.Id))
                continue;

            db.EarnedMedals.Add(new EarnedMedal
            {
                MedalDefinitionId = definition.Id,
                SourceEntityType = sourceEntityType,
                SourceEntityId = sourceEntityId,
                EarnedAt = DateTime.UtcNow
            });

            earnedDefinitionIds.Add(definition.Id);
            events.Add(new AchievementEvent(
                definition.Name,
                definition.Description,
                PointsEarned: 0,
                sourceType,
                spec.Code,
                RequiresCelebration: true));
        }

        if (events.Count > 0)
            await db.SaveChangesAsync(cancellationToken);

        return events;
    }

    private static async Task<int> ResolveTrackCountAsync(
        HobbyXpDbContext db,
        MedalMilestoneTrack track,
        CancellationToken cancellationToken) => track switch
    {
        MedalMilestoneTrack.BooksCompleted => await db.Books
            .CountAsync(b => b.Status == BookStatus.Completed, cancellationToken),
        MedalMilestoneTrack.BookPagesRead => await db.Books
            .SumAsync(b => b.PagesRead, cancellationToken),
        MedalMilestoneTrack.MediaCompleted => await db.MediaEntries
            .CountAsync(cancellationToken),
        MedalMilestoneTrack.PuzzlesCompleted => await db.Puzzles
            .CountAsync(cancellationToken),
        MedalMilestoneTrack.CoursesCompleted => await db.Courses
            .CountAsync(c => c.Status == CourseStatus.Completed, cancellationToken),
        MedalMilestoneTrack.CourseSessions => await db.Courses
            .SumAsync(c => c.SessionsCompleted, cancellationToken),
        MedalMilestoneTrack.OfficialRacesCompleted => await db.OfficialRaces
            .CountAsync(r => r.IsCompleted, cancellationToken),
        MedalMilestoneTrack.RunningSessions => await db.RunningSessions
            .CountAsync(cancellationToken),
        MedalMilestoneTrack.RunningKilometers => (int)await db.RunningSessions
            .SumAsync(s => s.DistanceKm, cancellationToken),
        MedalMilestoneTrack.GymWorkouts => await db.GymWorkouts
            .CountAsync(cancellationToken),
        MedalMilestoneTrack.ProgressiveOverloadPrs => await db.GymWorkouts
            .CountAsync(w => w.TriggeredProgressiveOverload, cancellationToken),
        MedalMilestoneTrack.VideoGamesPlatinum => await db.VideoGames
            .CountAsync(g => g.CompletionPercentage >= 100, cancellationToken),
        _ => 0
    };
}
