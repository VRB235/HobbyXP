using HobbyXP.Data;
using HobbyXP.Helpers;
using HobbyXP.Models.Achievements;
using HobbyXP.Models.Enums;
using HobbyXP.Services.Abstractions;
using HobbyXP.Services.Internal;
using HobbyXP.Services.Results;
using Microsoft.EntityFrameworkCore;

namespace HobbyXP.Services;

public sealed class AchievementProgressService : IAchievementProgressService
{
    private readonly IDbContextFactory<HobbyXpDbContext> _dbContextFactory;
    private readonly IMedalService _medalService;

    public AchievementProgressService(
        IDbContextFactory<HobbyXpDbContext> dbContextFactory,
        IMedalService medalService)
    {
        _dbContextFactory = dbContextFactory;
        _medalService = medalService;
    }

    public async Task<NextMedalProgress?> GetNextMedalAsync(
        MilestoneSourceType sourceType,
        CancellationToken cancellationToken = default)
    {
        var tracks = MedalTrackMap.ForSource(sourceType);
        if (tracks.Count == 0)
            return null;

        var candidates = new List<NextMedalProgress>();
        foreach (var track in tracks)
        {
            var next = await GetNextForTrackAsync(track, cancellationToken);
            if (next is not null)
                candidates.Add(next);
        }

        return candidates
            .OrderBy(c => c.Remaining)
            .ThenBy(c => c.Threshold)
            .FirstOrDefault();
    }

    public async Task<NextRewardProgress?> GetNearestRewardAsync(
        MilestoneSourceType sourceType,
        CancellationToken cancellationToken = default)
    {
        if (!HobbyProgressCatalog.IsTrackedHobby(sourceType))
            return null;

        await using var db = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        var profile = await db.PlayerProfiles.AsNoTracking().FirstAsync(cancellationToken);
        var balance = await db.HobbyProgresses
            .AsNoTracking()
            .Where(h => h.SourceType == sourceType)
            .Select(h => h.SpendableXp)
            .FirstOrDefaultAsync(cancellationToken);

        var rewards = await db.Rewards
            .AsNoTracking()
            .Where(r => r.Status == RewardStatus.Available && r.SourceType == sourceType)
            .ToListAsync(cancellationToken);

        return PickNearest(rewards, profile.CurrentLevel, balance);
    }

    public async Task<AchievementHubSnapshot> GetHubSnapshotAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        var profile = await db.PlayerProfiles.AsNoTracking().FirstAsync(cancellationToken);
        var showcase = await _medalService.GetShowcaseAsync(cancellationToken);
        var latest = showcase
            .Where(m => m.IsEarned)
            .OrderByDescending(m => m.EarnedAt)
            .FirstOrDefault();

        var closest = await GetClosestNextAsync(cancellationToken);

        var availableRewards = await db.Rewards
            .AsNoTracking()
            .Where(r => r.Status == RewardStatus.Available && r.SourceType != null)
            .ToListAsync(cancellationToken);

        var balances = await db.HobbyProgresses
            .AsNoTracking()
            .ToDictionaryAsync(h => h.SourceType, h => h.SpendableXp, cancellationToken);

        NextRewardProgress? nearestGlobal = null;
        foreach (var group in availableRewards.GroupBy(r => r.SourceType!.Value))
        {
            var balance = balances.GetValueOrDefault(group.Key, 0);
            var nearest = PickNearest(group.ToList(), profile.CurrentLevel, balance);
            if (nearest is null)
                continue;

            if (nearestGlobal is null
                || nearest.RemainingXp < nearestGlobal.RemainingXp
                || (nearest.RemainingXp == nearestGlobal.RemainingXp
                    && nearest.EffectiveCost < nearestGlobal.EffectiveCost))
            {
                nearestGlobal = nearest;
            }
        }

        Reward? featured = null;
        var effectiveCost = 0;
        var moduleBalance = 0;
        if (nearestGlobal is not null)
        {
            featured = availableRewards.First(r => r.Id == nearestGlobal.RewardId);
            effectiveCost = nearestGlobal.EffectiveCost;
            moduleBalance = nearestGlobal.ModuleBalance;
        }

        string? equippedName = null;
        if (profile.EquippedRewardId is int equippedId)
        {
            equippedName = await db.Rewards
                .AsNoTracking()
                .Where(r => r.Id == equippedId)
                .Select(r => r.Name)
                .FirstOrDefaultAsync(cancellationToken);
        }

        return new AchievementHubSnapshot(
            latest,
            closest,
            featured,
            effectiveCost,
            moduleBalance,
            nearestGlobal?.CanAfford ?? false,
            profile.HonorTitle,
            equippedName,
            profile.DisciplineImmunityUntilUtc);
    }

    public async Task<int> GetUnseenMedalCountAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        var profile = await db.PlayerProfiles.AsNoTracking().FirstAsync(cancellationToken);
        var earned = await db.EarnedMedals.CountAsync(cancellationToken);
        return Math.Max(0, earned - profile.LastSeenEarnedMedalCount);
    }

    public async Task MarkMedalsSeenAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        var profile = await db.PlayerProfiles.FirstAsync(cancellationToken);
        profile.LastSeenEarnedMedalCount = await db.EarnedMedals.CountAsync(cancellationToken);
        profile.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
    }

    private async Task<NextMedalProgress?> GetClosestNextAsync(CancellationToken cancellationToken)
    {
        var candidates = new List<NextMedalProgress>();
        foreach (var track in Enum.GetValues<MedalMilestoneTrack>())
        {
            var next = await GetNextForTrackAsync(track, cancellationToken);
            if (next is not null)
                candidates.Add(next);
        }

        return candidates
            .OrderBy(c => c.Remaining)
            .ThenBy(c => c.Threshold)
            .FirstOrDefault();
    }

    private async Task<NextMedalProgress?> GetNextForTrackAsync(
        MedalMilestoneTrack track,
        CancellationToken cancellationToken)
    {
        await using var db = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        var current = await MedalTrackCounter.ResolveAsync(db, track, cancellationToken);
        var earnedCodes = await db.EarnedMedals
            .AsNoTracking()
            .Select(m => m.MedalDefinition!.Code)
            .ToListAsync(cancellationToken);

        var next = MedalCatalog.ForTrack(track)
            .Where(e => !earnedCodes.Contains(e.Code))
            .OrderBy(e => e.Threshold)
            .FirstOrDefault();

        if (next is null)
            return null;

        var icon = await db.MedalDefinitions
            .AsNoTracking()
            .Where(d => d.Code == next.Code)
            .Select(d => d.IconPath)
            .FirstOrDefaultAsync(cancellationToken);

        return new NextMedalProgress(
            next.Code,
            next.Name,
            AchievementDisplayNames.ForMedalTrack(track),
            current,
            next.Threshold,
            string.IsNullOrWhiteSpace(icon) ? next.IconPath : icon);
    }

    private static NextRewardProgress? PickNearest(
        IReadOnlyList<Reward> rewards,
        int currentLevel,
        int moduleBalance)
    {
        if (rewards.Count == 0)
            return null;

        return rewards
            .Select(reward =>
            {
                var effectiveCost = RewardCostCalculator.GetEffectiveCost(reward.CostInPoints, currentLevel);
                return new NextRewardProgress(
                    reward.Id,
                    reward.Name,
                    reward.SourceType!.Value,
                    effectiveCost,
                    moduleBalance,
                    reward.ImagePath,
                    reward.PurchaseUrl,
                    reward.Price);
            })
            .OrderBy(r => r.RemainingXp)
            .ThenBy(r => r.EffectiveCost)
            .ThenBy(r => r.Name, StringComparer.CurrentCultureIgnoreCase)
            .FirstOrDefault();
    }
}
