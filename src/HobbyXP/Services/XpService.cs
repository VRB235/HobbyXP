using HobbyXP.Data;
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

        if (profile.TotalXp < amount)
            return false;

        profile.TotalXp -= amount;
        profile.UpdatedAt = DateTime.UtcNow;
        XpLevelCalculator.RecalculateLevel(profile);

        db.XpTransactions.Add(new XpTransaction
        {
            PlayerProfileId = profile.Id,
            Amount = -amount,
            ActionType = AchievementActionType.RewardRedeemed,
            Description = description,
            SourceEntityType = nameof(Reward),
            EarnedAt = DateTime.UtcNow
        });

        await db.SaveChangesAsync(cancellationToken);
        return true;
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
            .Where(t => t.EarnedAt >= startDate && t.Amount > 0)
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
            return new XpAwardOutcome(0, profile.TotalXp, null, false, null);
        }

        var previousLevel = profile.CurrentLevel;
        profile.TotalXp += totalAward;
        profile.UpdatedAt = DateTime.UtcNow;
        XpLevelCalculator.RecalculateLevel(profile);

        db.XpTransactions.Add(new XpTransaction
        {
            PlayerProfileId = profile.Id,
            Amount = totalAward,
            ActionType = actionType,
            Description = description,
            SourceEntityType = sourceEntityType,
            SourceEntityId = sourceEntityId,
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

        await db.SaveChangesAsync(cancellationToken);

        var leveledUp = profile.CurrentLevel > previousLevel;
        if (leveledUp)
            _levelUpMessenger.Publish(profile.CurrentLevel, profile.TotalXp);

        return new XpAwardOutcome(
            totalAward,
            profile.TotalXp,
            leveledUp ? profile.CurrentLevel : null,
            leveledUp,
            milestone);
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
