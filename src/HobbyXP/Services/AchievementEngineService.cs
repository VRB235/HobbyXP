using HobbyXP.Data;
using HobbyXP.Helpers;
using HobbyXP.Models.Achievements;
using HobbyXP.Models.Core;
using HobbyXP.Models.Enums;
using HobbyXP.Services.Abstractions;
using HobbyXP.Services.Internal;
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

        var currentCount = await MedalTrackCounter.ResolveAsync(db, track, cancellationToken);
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

            var bonus = MedalPrivilegeRules.GetSpendableBonus(spec.Threshold);
            await ApplyMedalPrivilegesAsync(db, definition, bonus, cancellationToken);

            events.Add(new AchievementEvent(
                definition.Name,
                $"{definition.Description} · {MedalPrivilegeRules.FormatSummary(bonus)}",
                bonus,
                sourceType,
                spec.Code,
                RequiresCelebration: true));
        }

        if (events.Count > 0)
            await db.SaveChangesAsync(cancellationToken);

        return events;
    }

    private static async Task ApplyMedalPrivilegesAsync(
        HobbyXpDbContext db,
        MedalDefinition definition,
        int spendableBonus,
        CancellationToken cancellationToken)
    {
        var profile = await db.PlayerProfiles.FirstAsync(cancellationToken);
        var now = DateTime.UtcNow;

        profile.SpendableXp += spendableBonus;
        profile.HonorTitle = definition.Name;
        profile.DisciplineImmunityUntilUtc = MedalPrivilegeRules.ExtendImmunity(
            now,
            profile.DisciplineImmunityUntilUtc);
        profile.UpdatedAt = now;

        db.XpTransactions.Add(new XpTransaction
        {
            PlayerProfileId = profile.Id,
            Amount = spendableBonus,
            ActionType = AchievementActionType.MedalPrivilegeBonus,
            Description = $"Bonus de medalla: {definition.Name}",
            SourceEntityType = nameof(MedalDefinition),
            SourceEntityId = definition.Id,
            SourceType = MilestoneSourceType.System,
            IsGlobal = true,
            EarnedAt = now
        });
    }
}
