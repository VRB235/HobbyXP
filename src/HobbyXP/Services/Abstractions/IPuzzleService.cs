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
        IReadOnlyList<string>? photoPaths = null,
        DateTime? completedAt = null,
        CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(int puzzleId, CancellationToken cancellationToken = default);
}
