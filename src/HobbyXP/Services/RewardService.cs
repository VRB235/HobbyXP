using HobbyXP.Data;
using HobbyXP.Models.Achievements;
using HobbyXP.Models.Enums;
using HobbyXP.Services.Abstractions;
using HobbyXP.Services.Results;
using Microsoft.EntityFrameworkCore;

namespace HobbyXP.Services;

public sealed class RewardService : IRewardService
{
    private readonly IDbContextFactory<HobbyXpDbContext> _dbContextFactory;
    private readonly IXpService _xpService;
    private readonly IPlayerProfileService _playerProfileService;

    public RewardService(
        IDbContextFactory<HobbyXpDbContext> dbContextFactory,
        IXpService xpService,
        IPlayerProfileService playerProfileService)
    {
        _dbContextFactory = dbContextFactory;
        _xpService = xpService;
        _playerProfileService = playerProfileService;
    }

    public async Task<IReadOnlyList<Reward>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        return await db.Rewards
            .AsNoTracking()
            .OrderBy(r => r.Status)
            .ThenByDescending(r => r.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<Reward> CreateAsync(
        string name,
        int costInPoints,
        string? description = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("El nombre es obligatorio.", nameof(name));

        if (costInPoints <= 0)
            throw new ArgumentOutOfRangeException(nameof(costInPoints));

        await using var db = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        var reward = new Reward
        {
            Name = name.Trim(),
            Description = description,
            CostInPoints = costInPoints,
            Status = RewardStatus.Available
        };

        db.Rewards.Add(reward);
        await db.SaveChangesAsync(cancellationToken);
        return reward;
    }

    public async Task<bool> CanRedeemAsync(int rewardId, CancellationToken cancellationToken = default)
    {
        await using var db = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        var reward = await db.Rewards
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == rewardId, cancellationToken);

        if (reward is null || reward.Status != RewardStatus.Available)
            return false;

        var profile = await _playerProfileService.GetProfileAsync(cancellationToken);
        return profile.TotalXp >= reward.CostInPoints;
    }

    public async Task<OperationResult<Reward>> RedeemAsync(
        int rewardId,
        CancellationToken cancellationToken = default)
    {
        await using var db = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        var reward = await db.Rewards.FindAsync([rewardId], cancellationToken)
            ?? throw new InvalidOperationException($"No existe el premio con Id {rewardId}.");

        if (reward.Status != RewardStatus.Available)
            throw new InvalidOperationException("El premio ya fue reclamado.");

        var deducted = await _xpService.TryDeductXpAsync(
            reward.CostInPoints,
            $"Canje de premio: {reward.Name}",
            cancellationToken);

        if (!deducted)
            throw new InvalidOperationException("No tienes suficiente XP para canjear este premio.");

        reward.Status = RewardStatus.Redeemed;
        reward.RedeemedAt = DateTime.UtcNow;
        reward.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);

        var milestone = new Models.Core.Milestone
        {
            Title = $"Premio canjeado: {reward.Name}",
            Description = $"Gastaste {reward.CostInPoints} XP.",
            PointsEarned = -reward.CostInPoints,
            SourceType = MilestoneSourceType.Reward,
            SourceEntityId = reward.Id,
            CompletedAt = DateTime.UtcNow
        };

        db.Milestones.Add(milestone);
        await db.SaveChangesAsync(cancellationToken);

        var achievementEvent = new AchievementEvent(
            milestone.Title,
            milestone.Description ?? reward.Name,
            -reward.CostInPoints,
            MilestoneSourceType.Reward);

        return OperationResult<Reward>.WithEvents(reward, achievementEvent);
    }
}
