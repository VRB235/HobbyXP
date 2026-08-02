using HobbyXP.Data;
using HobbyXP.Helpers;
using HobbyXP.Models.Achievements;
using HobbyXP.Models.Core;
using HobbyXP.Models.Enums;
using HobbyXP.Services.Abstractions;
using HobbyXP.Services.Internal;
using HobbyXP.Services.Messaging;
using HobbyXP.Services.Results;
using Microsoft.EntityFrameworkCore;

namespace HobbyXP.Services;

public sealed class XpService : IXpService
{
    private readonly IDbContextFactory<HobbyXpDbContext> _dbContextFactory;
    private readonly ILevelUpMessenger _levelUpMessenger;

    public XpService(
        IDbContextFactory<HobbyXpDbContext> dbContextFactory,
        ILevelUpMessenger levelUpMessenger)
    {
        _dbContextFactory = dbContextFactory;
        _levelUpMessenger = levelUpMessenger;
    }

    public async Task<LevelProgressInfo> GetLevelProgressAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        var profile = await GetProfileAsync(db, cancellationToken);
        return XpLevelCalculator.BuildProgress(profile);
    }

    public async Task<LevelProgressInfo> GetHobbyProgressAsync(
        MilestoneSourceType sourceType,
        CancellationToken cancellationToken = default)
    {
        if (!HobbyProgressCatalog.IsTrackedHobby(sourceType))
            throw new ArgumentOutOfRangeException(nameof(sourceType), "No es un hobby con pool de XP.");

        await using var db = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        var profile = await GetProfileAsync(db, cancellationToken);
        var hobby = await GetOrCreateHobbyProgressAsync(db, profile, sourceType, cancellationToken);
        return XpLevelCalculator.BuildProgress(hobby, profile.BaseXpPerLevel);
    }

    public async Task<IReadOnlyList<HobbyProgressInfo>> GetAllHobbyProgressAsync(
        CancellationToken cancellationToken = default)
    {
        await using var db = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        var profile = await GetProfileAsync(db, cancellationToken);
        await EnsureAllHobbyRowsAsync(db, profile, cancellationToken);

        return HobbyProgressCatalog.TrackedHobbies
            .Select(source =>
            {
                var hobby = profile.HobbyProgresses.First(h => h.SourceType == source);
                var progress = XpLevelCalculator.BuildProgress(hobby, profile.BaseXpPerLevel);
                return new HobbyProgressInfo(
                    source,
                    HobbyProgressCatalog.GetDisplayName(source),
                    progress,
                    HobbyLevelTitles.GetTitle(source, progress.CurrentLevel));
            })
            .ToList();
    }

    public async Task<int> CalculatePointsAsync(
        AchievementActionType actionType,
        decimal units,
        CancellationToken cancellationToken = default)
    {
        await using var db = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        var rule = await db.AchievementRules
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.IsActive && r.ActionType == actionType, cancellationToken);

        if (rule is null)
            return 0;

        var scaled = (int)Math.Round(rule.PointsPerUnit * units, MidpointRounding.AwayFromZero);
        return scaled + (rule.FlatBonusPoints ?? 0);
    }

    public Task<XpAwardOutcome> AwardXpAsync(
        AchievementActionType actionType,
        decimal units,
        string description,
        MilestoneSourceType milestoneSource,
        string? sourceEntityType = null,
        int? sourceEntityId = null,
        string? milestoneTitle = null,
        CancellationToken cancellationToken = default) =>
        AwardInternalAsync(
            actionType,
            units,
            flatBonus: null,
            description,
            milestoneSource,
            sourceEntityType,
            sourceEntityId,
            milestoneTitle,
            cancellationToken);

    public Task<XpAwardOutcome> AwardFlatBonusAsync(
        AchievementActionType actionType,
        int bonusPoints,
        string description,
        MilestoneSourceType milestoneSource,
        string? sourceEntityType = null,
        int? sourceEntityId = null,
        string? milestoneTitle = null,
        CancellationToken cancellationToken = default) =>
        AwardInternalAsync(
            actionType,
            units: 0m,
            flatBonus: bonusPoints,
            description,
            milestoneSource,
            sourceEntityType,
            sourceEntityId,
            milestoneTitle,
            cancellationToken);

    public async Task<bool> TryDeductXpAsync(
        int amount,
        string description,
        CancellationToken cancellationToken = default)
    {
        if (amount <= 0)
            return false;

        await using var db = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        var profile = await GetProfileAsync(db, cancellationToken);

        if (profile.SpendableXp < amount)
            return false;

        profile.SpendableXp -= amount;
        profile.UpdatedAt = DateTime.UtcNow;

        db.XpTransactions.Add(new XpTransaction
        {
            PlayerProfileId = profile.Id,
            Amount = -amount,
            ActionType = AchievementActionType.RewardRedeemed,
            Description = description,
            SourceEntityType = nameof(Reward),
            SourceType = MilestoneSourceType.Reward,
            IsGlobal = true,
            EarnedAt = DateTime.UtcNow
        });

        await db.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<int> RevokeXpForSourceAsync(
        MilestoneSourceType milestoneSource,
        string sourceEntityType,
        int sourceEntityId,
        string description,
        CancellationToken cancellationToken = default)
    {
        await using var db = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        var profile = await GetProfileAsync(db, cancellationToken);

        var transactions = await db.XpTransactions
            .Where(t => t.SourceEntityType == sourceEntityType &&
                        t.SourceEntityId == sourceEntityId &&
                        t.Amount > 0 &&
                        !t.IsGlobal)
            .ToListAsync(cancellationToken);

        var totalToRevoke = transactions.Sum(t => t.Amount);

        if (totalToRevoke > 0 && HobbyProgressCatalog.IsTrackedHobby(milestoneSource))
        {
            var hobby = await GetOrCreateHobbyProgressAsync(db, profile, milestoneSource, cancellationToken);
            var previousHobbyLevel = hobby.CurrentLevel;

            hobby.TotalXp = Math.Max(0, hobby.TotalXp - totalToRevoke);
            XpLevelCalculator.RecalculateLevel(hobby, profile.BaseXpPerLevel);

            var levelsLost = Math.Max(0, previousHobbyLevel - hobby.CurrentLevel);
            if (levelsLost > 0)
            {
                var globalPenalty = levelsLost * profile.BaseXpPerLevel;
                profile.TotalXp = Math.Max(0, profile.TotalXp - globalPenalty);
                profile.UpdatedAt = DateTime.UtcNow;
                XpLevelCalculator.RecalculateLevel(profile);

                db.XpTransactions.Add(new XpTransaction
                {
                    PlayerProfileId = profile.Id,
                    Amount = -globalPenalty,
                    ActionType = AchievementActionType.HobbyLevelUp,
                    Description = $"Ajuste global por pérdida de nivel en {HobbyProgressCatalog.GetDisplayName(milestoneSource)}",
                    SourceEntityType = sourceEntityType,
                    SourceEntityId = sourceEntityId,
                    SourceType = milestoneSource,
                    IsGlobal = true,
                    EarnedAt = DateTime.UtcNow
                });

                // El bonus global también se había acreditado al saldo canjeable.
                profile.SpendableXp = Math.Max(0, profile.SpendableXp - globalPenalty);
            }

            profile.SpendableXp = Math.Max(0, profile.SpendableXp - totalToRevoke);
            profile.UpdatedAt = DateTime.UtcNow;

            db.XpTransactions.Add(new XpTransaction
            {
                PlayerProfileId = profile.Id,
                Amount = -totalToRevoke,
                ActionType = AchievementActionType.RewardRedeemed,
                Description = description,
                SourceEntityType = sourceEntityType,
                SourceEntityId = sourceEntityId,
                SourceType = milestoneSource,
                IsGlobal = false,
                EarnedAt = DateTime.UtcNow
            });

            db.XpTransactions.RemoveRange(transactions);
        }
        else if (totalToRevoke > 0)
        {
            // Fallback legacy: txs globales sin pool de hobby.
            profile.TotalXp = Math.Max(0, profile.TotalXp - totalToRevoke);
            profile.SpendableXp = Math.Max(0, profile.SpendableXp - totalToRevoke);
            profile.UpdatedAt = DateTime.UtcNow;
            XpLevelCalculator.RecalculateLevel(profile);

            db.XpTransactions.Add(new XpTransaction
            {
                PlayerProfileId = profile.Id,
                Amount = -totalToRevoke,
                ActionType = AchievementActionType.RewardRedeemed,
                Description = description,
                SourceEntityType = sourceEntityType,
                SourceEntityId = sourceEntityId,
                SourceType = milestoneSource,
                IsGlobal = true,
                EarnedAt = DateTime.UtcNow
            });

            db.XpTransactions.RemoveRange(transactions);
        }

        var milestones = await db.Milestones
            .Where(m => m.SourceType == milestoneSource && m.SourceEntityId == sourceEntityId)
            .ToListAsync(cancellationToken);

        if (milestones.Count > 0)
            db.Milestones.RemoveRange(milestones);

        var medals = await db.EarnedMedals
            .Where(m => m.SourceEntityType == sourceEntityType && m.SourceEntityId == sourceEntityId)
            .ToListAsync(cancellationToken);

        if (medals.Count > 0)
            db.EarnedMedals.RemoveRange(medals);

        await db.SaveChangesAsync(cancellationToken);
        return totalToRevoke;
    }

    public async Task<IReadOnlyList<DailyXpPoint>> GetDailyXpForLastDaysAsync(
        int days,
        CancellationToken cancellationToken = default)
    {
        if (days <= 0)
            return Array.Empty<DailyXpPoint>();

        await using var db = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        var startDate = DateTime.UtcNow.Date.AddDays(-(days - 1));

        var grouped = await db.XpTransactions
            .AsNoTracking()
            .Where(t => t.EarnedAt >= startDate && t.Amount > 0 && !t.IsGlobal)
            .GroupBy(t => t.EarnedAt.Date)
            .Select(g => new { Date = g.Key, Total = g.Sum(t => t.Amount) })
            .ToListAsync(cancellationToken);

        var lookup = grouped.ToDictionary(x => DateOnly.FromDateTime(x.Date), x => x.Total);
        var result = new List<DailyXpPoint>(days);

        for (var i = 0; i < days; i++)
        {
            var date = DateOnly.FromDateTime(startDate.AddDays(i));
            lookup.TryGetValue(date, out var total);
            result.Add(new DailyXpPoint(date, total));
        }

        return result;
    }

    private async Task<XpAwardOutcome> AwardInternalAsync(
        AchievementActionType actionType,
        decimal units,
        int? flatBonus,
        string description,
        MilestoneSourceType milestoneSource,
        string? sourceEntityType,
        int? sourceEntityId,
        string? milestoneTitle,
        CancellationToken cancellationToken)
    {
        await using var db = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        var profile = await GetProfileAsync(db, cancellationToken);
        var rule = await db.AchievementRules
            .FirstOrDefaultAsync(r => r.IsActive && r.ActionType == actionType, cancellationToken);

        var scaled = rule is null
            ? 0
            : (int)Math.Round(rule.PointsPerUnit * units, MidpointRounding.AwayFromZero);

        var bonus = flatBonus ?? rule?.FlatBonusPoints ?? 0;
        var totalAward = scaled + bonus;

        if (totalAward <= 0)
        {
            var emptyHobbyXp = HobbyProgressCatalog.IsTrackedHobby(milestoneSource)
                ? (await GetOrCreateHobbyProgressAsync(db, profile, milestoneSource, cancellationToken)).TotalXp
                : profile.TotalXp;
            return new XpAwardOutcome(0, emptyHobbyXp, null, false, null);
        }

        if (!HobbyProgressCatalog.IsTrackedHobby(milestoneSource))
            throw new InvalidOperationException(
                $"No se puede otorgar XP de actividad al source '{milestoneSource}' (sin pool de hobby).");

        var hobby = await GetOrCreateHobbyProgressAsync(db, profile, milestoneSource, cancellationToken);
        var previousHobbyLevel = hobby.CurrentLevel;
        var previousGlobalLevel = profile.CurrentLevel;

        hobby.TotalXp += totalAward;
        profile.SpendableXp += totalAward;
        profile.UpdatedAt = DateTime.UtcNow;
        XpLevelCalculator.RecalculateLevel(hobby, profile.BaseXpPerLevel);

        db.XpTransactions.Add(new XpTransaction
        {
            PlayerProfileId = profile.Id,
            Amount = totalAward,
            ActionType = actionType,
            Description = description,
            SourceEntityType = sourceEntityType,
            SourceEntityId = sourceEntityId,
            SourceType = milestoneSource,
            IsGlobal = false,
            EarnedAt = DateTime.UtcNow
        });

        Milestone? milestone = null;
        if (!string.IsNullOrWhiteSpace(milestoneTitle))
        {
            milestone = new Milestone
            {
                Title = milestoneTitle,
                Description = description,
                PointsEarned = totalAward,
                SourceType = milestoneSource,
                SourceEntityId = sourceEntityId,
                CompletedAt = DateTime.UtcNow
            };
            db.Milestones.Add(milestone);
        }

        var levelsGained = Math.Max(0, hobby.CurrentLevel - previousHobbyLevel);
        var globalBonus = 0;
        if (levelsGained > 0)
        {
            globalBonus = levelsGained * profile.BaseXpPerLevel;
            profile.TotalXp += globalBonus;
            profile.SpendableXp += globalBonus;
            profile.UpdatedAt = DateTime.UtcNow;
            XpLevelCalculator.RecalculateLevel(profile);

            db.XpTransactions.Add(new XpTransaction
            {
                PlayerProfileId = profile.Id,
                Amount = globalBonus,
                ActionType = AchievementActionType.HobbyLevelUp,
                Description =
                    $"Bonus global: {HobbyProgressCatalog.GetDisplayName(milestoneSource)} → {HobbyLevelTitles.FormatLevelLabel(milestoneSource, hobby.CurrentLevel)}",
                SourceEntityType = sourceEntityType,
                SourceEntityId = sourceEntityId,
                SourceType = milestoneSource,
                IsGlobal = true,
                EarnedAt = DateTime.UtcNow
            });
        }

        await db.SaveChangesAsync(cancellationToken);

        var hobbyLeveledUp = hobby.CurrentLevel > previousHobbyLevel;
        var globalLeveledUp = profile.CurrentLevel > previousGlobalLevel;
        if (globalLeveledUp)
            _levelUpMessenger.Publish(profile.CurrentLevel, profile.TotalXp);

        return new XpAwardOutcome(
            totalAward,
            hobby.TotalXp,
            hobbyLeveledUp ? hobby.CurrentLevel : null,
            hobbyLeveledUp,
            milestone,
            globalBonus,
            globalLeveledUp ? profile.CurrentLevel : null,
            globalLeveledUp);
    }

    private static async Task<HobbyProgress> GetOrCreateHobbyProgressAsync(
        HobbyXpDbContext db,
        PlayerProfile profile,
        MilestoneSourceType sourceType,
        CancellationToken cancellationToken)
    {
        await EnsureAllHobbyRowsAsync(db, profile, cancellationToken);
        return profile.HobbyProgresses.First(h => h.SourceType == sourceType);
    }

    private static async Task EnsureAllHobbyRowsAsync(
        HobbyXpDbContext db,
        PlayerProfile profile,
        CancellationToken cancellationToken)
    {
        if (profile.HobbyProgresses.Count == 0)
        {
            await db.Entry(profile)
                .Collection(p => p.HobbyProgresses)
                .LoadAsync(cancellationToken);
        }

        var changed = false;
        foreach (var source in HobbyProgressCatalog.TrackedHobbies)
        {
            if (profile.HobbyProgresses.Any(h => h.SourceType == source))
                continue;

            var hobby = new HobbyProgress
            {
                PlayerProfileId = profile.Id,
                SourceType = source,
                CurrentLevel = 1,
                TotalXp = 0
            };
            profile.HobbyProgresses.Add(hobby);
            db.HobbyProgresses.Add(hobby);
            changed = true;
        }

        if (changed)
            await db.SaveChangesAsync(cancellationToken);
    }

    private static async Task<PlayerProfile> GetProfileAsync(
        HobbyXpDbContext db,
        CancellationToken cancellationToken)
    {
        var profile = await db.PlayerProfiles.FirstOrDefaultAsync(cancellationToken);
        if (profile is null)
            throw new InvalidOperationException("No existe un perfil de jugador inicializado.");

        return profile;
    }
}
