using HobbyXP.Models.Entertainment;
using HobbyXP.Models.Enums;
using HobbyXP.Services.Results;

namespace HobbyXP.Services.Abstractions;

public interface IVideoGameService
{
    Task<IReadOnlyList<VideoGame>> GetInProgressAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<VideoGame>> GetPlatinumAsync(CancellationToken cancellationToken = default);

    Task<OperationResult<VideoGame>> RegisterAsync(
        string title,
        VideoGamePlatform platform,
        int initialCompletionPercentage = 0,
        CancellationToken cancellationToken = default);

    Task<OperationResult<VideoGame>> UpdateCompletionAsync(
        int videoGameId,
        int newCompletionPercentage,
        CancellationToken cancellationToken = default);

    Task<OperationResult<VideoGame>> IncrementCompletionAsync(
        int videoGameId,
        int increment = 1,
        CancellationToken cancellationToken = default);
}
