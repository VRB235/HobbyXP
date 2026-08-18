using HobbyXP.Models.Enums;
using HobbyXP.Services.Results;

namespace HobbyXP.Services.Abstractions;

public interface IAchievementProgressService
{
    Task<NextMedalProgress?> GetNextMedalAsync(
        MilestoneSourceType sourceType,
        CancellationToken cancellationToken = default);

    Task<AchievementHubSnapshot> GetHubSnapshotAsync(CancellationToken cancellationToken = default);

    Task<int> GetUnseenMedalCountAsync(CancellationToken cancellationToken = default);

    Task MarkMedalsSeenAsync(CancellationToken cancellationToken = default);
}
