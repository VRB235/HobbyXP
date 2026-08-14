using HobbyXP.Models.Enums;
using HobbyXP.Services.Results;

namespace HobbyXP.Services.Abstractions;

public interface IWeeklyQuotaService
{
    /// <summary>
    /// Evalúa semanas cerradas, aplica castigos pendientes y restaura si ya se cumplió la cuota.
    /// </summary>
    Task<WeeklyQuotaEvaluationSummary> EvaluateClosedWeeksAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Tras registrar actividad (posiblemente atrasada): actualiza conteos y restaura castigo si aplica.
    /// </summary>
    Task NotifyActivityAsync(
        MilestoneSourceType sourceType,
        DateTime activityLocalDate,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<WeeklyQuotaProgress>> GetCurrentWeekProgressAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Castigos activos (no restaurados) del hobby, del más reciente al más antiguo.
    /// </summary>
    Task<IReadOnlyList<string>> GetActivePenaltyRemindersAsync(
        MilestoneSourceType sourceType,
        CancellationToken cancellationToken = default);
}
