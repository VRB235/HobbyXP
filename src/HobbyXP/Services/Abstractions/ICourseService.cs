using HobbyXP.Models.PersonalGrowth;
using HobbyXP.Services.Results;

namespace HobbyXP.Services.Abstractions;

public interface ICourseService
{
    Task<IReadOnlyList<Course>> GetInProgressAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Course>> GetCompletedAsync(CancellationToken cancellationToken = default);

    Task<Course> RegisterAsync(
        string name,
        string platform,
        int totalSessions,
        string? imageSourcePath = null,
        CancellationToken cancellationToken = default);

    Task<OperationResult<Course>> LogSessionsAsync(
        int courseId,
        DateTime sessionDate,
        int sessionsDone,
        CancellationToken cancellationToken = default);

    Task<Course?> UpdateMetadataAsync(
        int courseId,
        string name,
        string platform,
        int totalSessions,
        DateTime? completedAt = null,
        string? imageSourcePath = null,
        bool clearImage = false,
        CancellationToken cancellationToken = default);

    Task<Course> UpdateImageAsync(
        int courseId,
        string? imageSourcePath = null,
        bool clearImage = false,
        CancellationToken cancellationToken = default);
}
