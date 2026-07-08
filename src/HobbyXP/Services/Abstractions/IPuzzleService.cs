using HobbyXP.Models.Entertainment;
using HobbyXP.Services.Results;

namespace HobbyXP.Services.Abstractions;

public interface IPuzzleService
{
    Task<IReadOnlyList<Puzzle>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<OperationResult<Puzzle>> RegisterCompletedAsync(
        string name,
        int pieceCount,
        Models.Enums.PuzzleCategory category,
        string? photoPath = null,
        CancellationToken cancellationToken = default);
}
