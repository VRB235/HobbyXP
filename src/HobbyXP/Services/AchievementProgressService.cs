using HobbyXP.Data;
using HobbyXP.Helpers;
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
        var featured = await db.Rewards
            .AsNoTracking()
            .Where(r => r.Status == RewardStatus.Available)
            .OrderBy(r => r.CostInPoints)
            .ThenBy(r => r.Name)
            .FirstOrDefaultAsync(cancellationToken);

        var effectiveCost = featured is null
            ? 0
            : RewardCostCalculator.GetEffectiveCost(featured.CostInPoints, profile.CurrentLevel);

        var moduleBalance = 0;
        if (featured?.SourceType is { } module && HobbyProgressCatalog.IsTrackedHobby(module))
        {
            moduleBalance = await db.HobbyProgresses
                .AsNoTracking()
                .Where(h => h.SourceType == module)
                .Select(h => h.SpendableXp)
                .FirstOrDefaultAsync(cancellationToken);
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
            featured is not null &&
            featured.SourceType is not null &&
            moduleBalance >= effectiveCost,
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
}
