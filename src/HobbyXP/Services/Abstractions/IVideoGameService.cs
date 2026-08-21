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
        DateTime? startedAt = null,
        string? imageSourcePath = null,
        CancellationToken cancellationToken = default);

    Task<OperationResult<VideoGame>> UpdateCompletionAsync(
        int videoGameId,
        int newCompletionPercentage,
        DateTime? progressDate = null,
        CancellationToken cancellationToken = default);

    Task<OperationResult<VideoGame>> IncrementCompletionAsync(
        int videoGameId,
        int increment = 1,
        CancellationToken cancellationToken = default);

    Task<VideoGame> UpdateMetadataAsync(
        int videoGameId,
        string title,
        VideoGamePlatform platform,
        DateTime? startedAt,
        DateTime? platinumUnlockedAt,
        string? imageSourcePath = null,
        bool clearImage = false,
        CancellationToken cancellationToken = default);

    Task<VideoGame> UpdateImageAsync(
        int videoGameId,
        string? imageSourcePath = null,
        bool clearImage = false,
        CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(int videoGameId, CancellationToken cancellationToken = default);
}
