using HobbyXP.Services.Results;

namespace HobbyXP.Services.Abstractions;

public interface IDashboardService
{
    Task<DashboardSummary> GetSummaryAsync(CancellationToken cancellationToken = default);
}
