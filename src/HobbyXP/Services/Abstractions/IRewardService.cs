using HobbyXP.Models.Achievements;
using HobbyXP.Models.Enums;
using HobbyXP.Services.Results;

namespace HobbyXP.Services.Abstractions;

public interface IRewardService
{
    Task<IReadOnlyList<Reward>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<Reward> CreateAsync(
        string name,
        int costInPoints,
        MilestoneSourceType sourceType,
        string? description = null,
        decimal? price = null,
        string? purchaseUrl = null,
        string? imageSourcePath = null,
        CancellationToken cancellationToken = default);

    Task<Reward> UpdateAsync(
        int rewardId,
        string name,
        int costInPoints,
        MilestoneSourceType sourceType,
        string? description = null,
        decimal? price = null,
        string? purchaseUrl = null,
        string? imageSourcePath = null,
        bool clearImage = false,
        CancellationToken cancellationToken = default);

    Task UpdateSourceTypeAsync(
        int rewardId,
        MilestoneSourceType sourceType,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(int rewardId, CancellationToken cancellationToken = default);

    Task<OperationResult<Reward>> RedeemAsync(
        int rewardId,
        CancellationToken cancellationToken = default);

    Task<bool> CanRedeemAsync(int rewardId, CancellationToken cancellationToken = default);

    Task EquipAsync(int rewardId, CancellationToken cancellationToken = default);

    Task UnequipAsync(CancellationToken cancellationToken = default);
}
