using HobbyXP.Data;
using HobbyXP.Models.Achievements;
using HobbyXP.Models.Enums;
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
        await using var db = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        var definition = await db.MedalDefinitions
            .FirstOrDefaultAsync(m => m.Code == code, cancellationToken);

        if (definition is null)
            return null;

        var alreadyEarned = await db.EarnedMedals
            .AnyAsync(m => m.MedalDefinitionId == definition.Id, cancellationToken);

        if (alreadyEarned)
            return null;

        db.EarnedMedals.Add(new EarnedMedal
        {
            MedalDefinitionId = definition.Id,
            SourceEntityType = sourceEntityType,
            SourceEntityId = sourceEntityId,
            EarnedAt = DateTime.UtcNow
        });

        await db.SaveChangesAsync(cancellationToken);

        return new AchievementEvent(
            definition.Name,
            definition.Description,
            PointsEarned: 0,
            sourceType,
            code,
            RequiresCelebration: true);
    }
}
