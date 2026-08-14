using HobbyXP.Models.Enums;
using HobbyXP.Models.Physical;
using HobbyXP.Services.Results;

namespace HobbyXP.Services.Abstractions;

public interface IDietService
{
    Task<DietDayLog?> GetByLocalDateAsync(DateTime localDate, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<DietDayLog>> GetHistoryAsync(CancellationToken cancellationToken = default);

    Task<OperationResult<DietDayLog>> SaveDayAsync(
        DietDayDraft draft,
        CancellationToken cancellationToken = default);

    Task<bool> DeleteDayAsync(int dietDayLogId, CancellationToken cancellationToken = default);
}

public sealed record DietDayDraft(
    DateTime LocalDate,
    DietMealStatus Breakfast,
    DietMealStatus Lunch,
    DietMealStatus Dinner,
    DietMealStatus Snack,
    string? Notes);
