using HobbyXP.Models.Feedback;
using HobbyXP.Services.Results;

namespace HobbyXP.Services.Abstractions;

public interface ISuggestionService
{
    Task<IReadOnlyList<Suggestion>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<OperationResult<Suggestion>> CreateAsync(
        string title,
        string description,
        Models.Enums.SuggestionKind kind,
        IReadOnlyList<string>? photoPaths = null,
        DateTime? reportedAt = null,
        CancellationToken cancellationToken = default);

    Task<OperationResult<Suggestion>> SetResolvedAsync(
        int suggestionId,
        bool resolved,
        CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(int suggestionId, CancellationToken cancellationToken = default);
}
