using HobbyXP.Models.PersonalGrowth;
using HobbyXP.Services.Results;

namespace HobbyXP.Services.Abstractions;

public interface IBookService
{
    Task<IReadOnlyList<Book>> GetReadingAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Book>> GetCompletedAsync(CancellationToken cancellationToken = default);

    Task<Book> RegisterAsync(
        string title,
        string author,
        int totalPages,
        string? imageSourcePath = null,
        CancellationToken cancellationToken = default);

    Task<OperationResult<Book>> UpdatePagesReadAsync(
        int bookId,
        int pagesRead,
        DateTime? readingDate = null,
        CancellationToken cancellationToken = default);

    Task<Book?> UpdateMetadataAsync(
        int bookId,
        string title,
        string author,
        DateTime? completedAt = null,
        string? imageSourcePath = null,
        bool clearImage = false,
        CancellationToken cancellationToken = default);

    Task<Book> UpdateImageAsync(
        int bookId,
        string? imageSourcePath = null,
        bool clearImage = false,
        CancellationToken cancellationToken = default);
}
