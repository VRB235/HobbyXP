using HobbyXP.Models.Achievements;
using HobbyXP.Services.Results;

namespace HobbyXP.Services.Abstractions;

public interface IRewardService
{
    Task<IReadOnlyList<Reward>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<Reward> CreateAsync(
        string name,
        int costInPoints,
        string? description = null,
        CancellationToken cancellationToken = default);

    Task<OperationResult<Reward>> RedeemAsync(
        int rewardId,
        CancellationToken cancellationToken = default);

    Task<bool> CanRedeemAsync(int rewardId, CancellationToken cancellationToken = default);

    Task EquipAsync(int rewardId, CancellationToken cancellationToken = default);

    Task UnequipAsync(CancellationToken cancellationToken = default);
}
