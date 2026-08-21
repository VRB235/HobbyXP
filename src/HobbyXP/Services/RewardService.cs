using System.IO;
using HobbyXP.Data;
using HobbyXP.Helpers;
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
        var rewards = await db.Rewards.ToListAsync(cancellationToken);

        var migrated = false;
        foreach (var reward in rewards)
        {
            if (string.IsNullOrWhiteSpace(reward.ImagePath))
                continue;

            var ensured = RewardPhotoStorage.EnsureManaged(reward.Id, reward.ImagePath);
            if (string.Equals(reward.ImagePath, ensured, StringComparison.OrdinalIgnoreCase))
                continue;

            reward.ImagePath = ensured;
            reward.UpdatedAt = DateTime.UtcNow;
            migrated = true;
        }

        if (migrated)
            await db.SaveChangesAsync(cancellationToken);

        return await db.Rewards
            .AsNoTracking()
            .OrderBy(r => r.Status)
            .ThenByDescending(r => r.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<Reward> CreateAsync(
        string name,
        int costInPoints,
        MilestoneSourceType sourceType,
        string? description = null,
        decimal? price = null,
        string? purchaseUrl = null,
        string? imageSourcePath = null,
        CancellationToken cancellationToken = default)
    {
        ValidateWritableFields(name, costInPoints, sourceType, price, purchaseUrl);

        await using var db = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        var reward = new Reward
        {
            Name = name.Trim(),
            Description = NormalizeOptionalText(description),
            CostInPoints = costInPoints,
            Price = price,
            PurchaseUrl = NormalizeOptionalText(purchaseUrl),
            SourceType = sourceType,
            Status = RewardStatus.Available
        };

        db.Rewards.Add(reward);
        await db.SaveChangesAsync(cancellationToken);

        if (!string.IsNullOrWhiteSpace(imageSourcePath))
        {
            reward.ImagePath = RewardPhotoStorage.SaveFromSource(reward.Id, imageSourcePath);
            reward.UpdatedAt = DateTime.UtcNow;
            await db.SaveChangesAsync(cancellationToken);
        }

        return reward;
    }

    public async Task<Reward> UpdateAsync(
        int rewardId,
        string name,
        int costInPoints,
        MilestoneSourceType sourceType,
        string? description = null,
        decimal? price = null,
        string? purchaseUrl = null,
        string? imageSourcePath = null,
        bool clearImage = false,
        CancellationToken cancellationToken = default)
    {
        ValidateWritableFields(name, costInPoints, sourceType, price, purchaseUrl);

        await using var db = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        var reward = await db.Rewards.FindAsync([rewardId], cancellationToken)
            ?? throw new InvalidOperationException($"No existe el premio con Id {rewardId}.");

        reward.Name = name.Trim();
        reward.Description = NormalizeOptionalText(description);
        reward.CostInPoints = costInPoints;
        reward.Price = price;
        reward.PurchaseUrl = NormalizeOptionalText(purchaseUrl);
        reward.SourceType = sourceType;
        reward.UpdatedAt = DateTime.UtcNow;

        if (clearImage)
        {
            RewardPhotoStorage.DeleteStoredPhoto(reward.Id, reward.ImagePath);
            reward.ImagePath = null;
        }
        else if (!string.IsNullOrWhiteSpace(imageSourcePath))
        {
            RewardPhotoStorage.DeleteStoredPhoto(reward.Id, reward.ImagePath);
            reward.ImagePath = RewardPhotoStorage.SaveFromSource(reward.Id, imageSourcePath);
        }

        await db.SaveChangesAsync(cancellationToken);
        return reward;
    }

    public async Task UpdateSourceTypeAsync(
        int rewardId,
        MilestoneSourceType sourceType,
        CancellationToken cancellationToken = default)
    {
        EnsureTrackedHobby(sourceType);

        await using var db = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        var reward = await db.Rewards.FindAsync([rewardId], cancellationToken)
            ?? throw new InvalidOperationException($"No existe el premio con Id {rewardId}.");

        reward.SourceType = sourceType;
        reward.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(int rewardId, CancellationToken cancellationToken = default)
    {
        await using var db = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        var reward = await db.Rewards.FindAsync([rewardId], cancellationToken)
            ?? throw new InvalidOperationException($"No existe el premio con Id {rewardId}.");

        if (reward.Status == RewardStatus.Redeemed)
            throw new InvalidOperationException("No se puede eliminar un premio ya canjeado. Desequipelo si aplica.");

        var profile = await db.PlayerProfiles.FirstAsync(cancellationToken);
        if (profile.EquippedRewardId == rewardId)
            profile.EquippedRewardId = null;

        RewardPhotoStorage.DeleteRewardFolder(rewardId);
        db.Rewards.Remove(reward);
        await db.SaveChangesAsync(cancellationToken);
    }

    private static void ValidateWritableFields(
        string name,
        int costInPoints,
        MilestoneSourceType sourceType,
        decimal? price,
        string? purchaseUrl)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("El nombre es obligatorio.", nameof(name));

        if (costInPoints <= 0)
            throw new ArgumentOutOfRangeException(nameof(costInPoints));

        if (price is < 0)
            throw new ArgumentOutOfRangeException(nameof(price));

        if (purchaseUrl is { Length: > 2000 })
            throw new ArgumentException("El enlace de compra es demasiado largo.", nameof(purchaseUrl));

        EnsureTrackedHobby(sourceType);
    }

    private static string? NormalizeOptionalText(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static void EnsureTrackedHobby(MilestoneSourceType sourceType)
    {
        if (!HobbyProgressCatalog.IsTrackedHobby(sourceType))
            throw new ArgumentOutOfRangeException(nameof(sourceType), "Indique un módulo válido.");
    }

    public async Task<bool> CanRedeemAsync(int rewardId, CancellationToken cancellationToken = default)
    {
        await using var db = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        var reward = await db.Rewards
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == rewardId, cancellationToken);

        if (reward is null || reward.Status != RewardStatus.Available)
            return false;

        if (reward.SourceType is not { } module || !HobbyProgressCatalog.IsTrackedHobby(module))
            return false;

        var profile = await _playerProfileService.GetProfileAsync(cancellationToken);
        var cost = RewardCostCalculator.GetEffectiveCost(reward.CostInPoints, profile.CurrentLevel);
        var moduleBalance = await _xpService.GetHobbySpendableXpAsync(module, cancellationToken);
        return moduleBalance >= cost;
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

        if (reward.SourceType is not { } module || !HobbyProgressCatalog.IsTrackedHobby(module))
            throw new InvalidOperationException("El premio debe pertenecer a un módulo válido.");

        var profile = await _playerProfileService.GetProfileAsync(cancellationToken);
        var effectiveCost = RewardCostCalculator.GetEffectiveCost(reward.CostInPoints, profile.CurrentLevel);

        var deducted = await _xpService.TryDeductXpAsync(
            effectiveCost,
            module,
            $"Canje de premio: {reward.Name} ({effectiveCost:N0} XP · {HobbyProgressCatalog.GetDisplayName(module)})",
            cancellationToken);

        if (!deducted)
            throw new InvalidOperationException(
                $"No tienes suficiente XP de {HobbyProgressCatalog.GetDisplayName(module)} para canjear este premio.");

        reward.Status = RewardStatus.Redeemed;
        reward.RedeemedAt = DateTime.UtcNow;
        reward.RedeemedCostInPoints = effectiveCost;
        reward.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);

        var milestone = new Models.Core.Milestone
        {
            Title = $"Premio canjeado: {reward.Name}",
            Description = $"Gastaste {effectiveCost:N0} XP (base {reward.CostInPoints:N0} × nivel {profile.CurrentLevel}).",
            PointsEarned = -effectiveCost,
            SourceType = MilestoneSourceType.Reward,
            SourceEntityId = reward.Id,
            CompletedAt = DateTime.UtcNow
        };

        db.Milestones.Add(milestone);
        await db.SaveChangesAsync(cancellationToken);

        var achievementEvent = new AchievementEvent(
            milestone.Title,
            milestone.Description ?? reward.Name,
            -effectiveCost,
            MilestoneSourceType.Reward);

        return OperationResult<Reward>.WithEvents(reward, achievementEvent);
    }

    public async Task EquipAsync(int rewardId, CancellationToken cancellationToken = default)
    {
        await using var db = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        var reward = await db.Rewards.AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == rewardId, cancellationToken)
            ?? throw new InvalidOperationException($"No existe el premio con Id {rewardId}.");

        if (reward.Status != RewardStatus.Redeemed)
            throw new InvalidOperationException("Solo se pueden equipar premios ya canjeados.");

        var profile = await db.PlayerProfiles.FirstAsync(cancellationToken);
        profile.EquippedRewardId = reward.Id;
        profile.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task UnequipAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        var profile = await db.PlayerProfiles.FirstAsync(cancellationToken);
        profile.EquippedRewardId = null;
        profile.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
    }
}
