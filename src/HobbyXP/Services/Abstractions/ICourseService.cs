using HobbyXP.Models.PersonalGrowth;
using HobbyXP.Services.Results;

namespace HobbyXP.Services.Abstractions;

public interface ICourseService
{
    Task<IReadOnlyList<Course>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<OperationResult<Course>> RegisterCompletedAsync(
        string name,
        string platform,
        DateTime? completedAt = null,
        CancellationToken cancellationToken = default);
}
