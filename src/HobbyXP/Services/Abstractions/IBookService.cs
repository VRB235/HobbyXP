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
        CancellationToken cancellationToken = default);
}
