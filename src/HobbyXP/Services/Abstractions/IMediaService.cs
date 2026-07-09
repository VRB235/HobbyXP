using HobbyXP.Models.Entertainment;
using HobbyXP.Models.Enums;
using HobbyXP.Services.Results;

namespace HobbyXP.Services.Abstractions;

public interface IMediaService
{
    Task<IReadOnlyList<MediaEntry>> GetHistoryAsync(CancellationToken cancellationToken = default);

    Task<MediaYearlyCounters> GetYearlyCountersAsync(
        int? year = null,
        CancellationToken cancellationToken = default);

    Task<OperationResult<MediaEntry>> RegisterCompletedAsync(
        string title,
        MediaType mediaType,
        DateTime? completedAt = null,
        CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(int entryId, CancellationToken cancellationToken = default);
}
