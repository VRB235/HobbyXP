using HobbyXP.Models.Enums;

namespace HobbyXP.Services.Abstractions;

public interface IModuleDisciplineService
{
    Task<bool> IsPausedAsync(
        MilestoneSourceType sourceType,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyDictionary<MilestoneSourceType, bool>> GetPauseStatesAsync(
        CancellationToken cancellationToken = default);

    Task SetPausedAsync(
        MilestoneSourceType sourceType,
        bool isPaused,
        CancellationToken cancellationToken = default);
}
