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
        CancellationToken cancellationToken = default);
}
