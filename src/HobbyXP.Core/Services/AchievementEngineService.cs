using HobbyXP.Data;
using HobbyXP.Helpers;
using HobbyXP.Models.Achievements;
using HobbyXP.Models.Core;
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

    public async Task<bool> IsMedalEarnedAsync(int medalDefinitionId, CancellationToken cancellationToken = default)
    {
        await using var db = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        return await db.EarnedMedals
            .AsNoTracking()
            .AnyAsync(m => m.MedalDefinitionId == medalDefinitionId, cancellationToken);
    }

    public async Task<IReadOnlyList<AchievementEvent>> TryAwardHobbyXpMedalsAsync(
        MilestoneSourceType sourceType,
        CancellationToken cancellationToken = default)
    {
        if (!HobbyProgressCatalog.IsTrackedHobby(sourceType))
            return [];

        await using var db = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        var hobbyXp = await db.HobbyProgresses
            .AsNoTracking()
            .Where(h => h.SourceType == sourceType)
            .Select(h => h.TotalXp)
            .FirstOrDefaultAsync(cancellationToken);

        var definitions = await db.MedalDefinitions
            .Where(m => m.SourceType == sourceType && m.XpThreshold <= hobbyXp)
            .OrderBy(m => m.XpThreshold)
            .ToListAsync(cancellationToken);

        if (definitions.Count == 0)
            return [];

        var earnedDefinitionIds = await db.EarnedMedals
            .Select(m => m.MedalDefinitionId)
            .ToListAsync(cancellationToken);

        var profile = await db.PlayerProfiles.FirstAsync(cancellationToken);
        var events = new List<AchievementEvent>();

        foreach (var definition in definitions)
        {
            if (earnedDefinitionIds.Contains(definition.Id))
                continue;

            db.EarnedMedals.Add(new EarnedMedal
            {
                MedalDefinitionId = definition.Id,
                EarnedAt = DateTime.UtcNow
            });

            earnedDefinitionIds.Add(definition.Id);

            var bonus = MedalPrivilegeRules.GetSpendableBonus(definition.XpThreshold);
            await ApplyMedalPrivilegesAsync(db, profile, definition, sourceType, bonus, cancellationToken);

            var iconPath = string.IsNullOrWhiteSpace(definition.IconPath)
                ? MedalIconPaths.ForHobby(definition.SourceType)
                : definition.IconPath;

            events.Add(new AchievementEvent(
                definition.Name,
                $"{definition.Description} · {MedalPrivilegeRules.FormatSummary(bonus)}",
                bonus,
                sourceType,
                definition.Id,
                iconPath,
                RequiresCelebration: true));
        }

        if (events.Count > 0)
            await db.SaveChangesAsync(cancellationToken);

        return events;
    }

    private static async Task ApplyMedalPrivilegesAsync(
        HobbyXpDbContext db,
        PlayerProfile profile,
        MedalDefinition definition,
        MilestoneSourceType hobbySource,
        int spendableBonus,
        CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;

        profile.SpendableXp += spendableBonus;
        profile.HonorTitle = definition.Name;
        profile.DisciplineImmunityUntilUtc = MedalPrivilegeRules.ExtendImmunity(
            now,
            profile.DisciplineImmunityUntilUtc);
        profile.UpdatedAt = now;

        await db.Entry(profile)
            .Collection(p => p.HobbyProgresses)
            .LoadAsync(cancellationToken);

        var hobby = profile.HobbyProgresses.FirstOrDefault(h => h.SourceType == hobbySource);
        if (hobby is not null)
        {
            hobby.SpendableXp += spendableBonus;
            hobby.UpdatedAt = now;
        }

        db.XpTransactions.Add(new XpTransaction
        {
            PlayerProfileId = profile.Id,
            Amount = spendableBonus,
            ActionType = AchievementActionType.MedalPrivilegeBonus,
            Description = $"Bonus de medalla: {definition.Name}",
            SourceEntityType = nameof(MedalDefinition),
            SourceEntityId = definition.Id,
            SourceType = hobbySource,
            IsGlobal = false,
            EarnedAt = now
        });
    }
}
