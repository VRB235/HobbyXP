using HobbyXP.Data;
using HobbyXP.Helpers;
using HobbyXP.Models.Enums;
using HobbyXP.Models.PersonalGrowth;
using HobbyXP.Services.Abstractions;
using HobbyXP.Services.Results;
using Microsoft.EntityFrameworkCore;

namespace HobbyXP.Services;

public sealed class BookService : IBookService
{
    private readonly IDbContextFactory<HobbyXpDbContext> _dbContextFactory;
    private readonly IXpService _xpService;
    private readonly IAchievementEngineService _achievementEngine;
    private readonly IWeeklyQuotaService _weeklyQuotaService;

    public BookService(
        IDbContextFactory<HobbyXpDbContext> dbContextFactory,
        IXpService xpService,
        IAchievementEngineService achievementEngine,
        IWeeklyQuotaService weeklyQuotaService)
    {
        _dbContextFactory = dbContextFactory;
        _xpService = xpService;
        _achievementEngine = achievementEngine;
        _weeklyQuotaService = weeklyQuotaService;
    }

    public async Task<IReadOnlyList<Book>> GetReadingAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        return await db.Books
            .AsNoTracking()
            .Where(b => b.Status == BookStatus.Reading)
            .OrderByDescending(b => b.UpdatedAt ?? b.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Book>> GetCompletedAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        return await db.Books
            .AsNoTracking()
            .Where(b => b.Status == BookStatus.Completed)
            .OrderByDescending(b => b.CompletedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<Book> RegisterAsync(
        string title,
        string author,
        int totalPages,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("El título es obligatorio.", nameof(title));

        if (totalPages <= 0)
            throw new ArgumentOutOfRangeException(nameof(totalPages));

        await using var db = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        var book = new Book
        {
            Title = title.Trim(),
            Author = author.Trim(),
            TotalPages = totalPages,
            PagesRead = 0,
            Status = BookStatus.Reading
        };

        db.Books.Add(book);
        await db.SaveChangesAsync(cancellationToken);
        return book;
    }

    public async Task<OperationResult<Book>> UpdatePagesReadAsync(
        int bookId,
        int pagesRead,
        DateTime? readingDate = null,
        CancellationToken cancellationToken = default)
    {
        if (pagesRead < 0)
            throw new ArgumentOutOfRangeException(nameof(pagesRead));

        var activityLocalDate = (readingDate ?? DateTime.Today).Date;

        await using var db = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        var book = await db.Books.FindAsync([bookId], cancellationToken)
            ?? throw new InvalidOperationException($"No existe el libro con Id {bookId}.");

        if (book.Status == BookStatus.Completed)
            return OperationResult<Book>.Empty(book);

        var clampedPages = Math.Min(pagesRead, book.TotalPages);
        var previousPages = book.PagesRead;
        if (clampedPages == previousPages)
            return OperationResult<Book>.Empty(book);

        book.PagesRead = clampedPages;
        book.UpdatedAt = DateTime.UtcNow;

        var events = new List<AchievementEvent>();
        var pageDelta = Math.Max(0, clampedPages - previousPages);

        if (pageDelta > 0)
        {
            db.BookReadingLogs.Add(new BookReadingLog
            {
                BookId = book.Id,
                ReadDate = DateTimeHelper.ToUtcFromLocalDate(activityLocalDate),
                PagesDone = pageDelta
            });
        }

        if (pageDelta > 0 && clampedPages < book.TotalPages)
        {
            var pageXp = await _xpService.AwardXpAsync(
                AchievementActionType.BookPageRead,
                pageDelta,
                $"Lectura: {book.Title} (+{pageDelta} páginas)",
                MilestoneSourceType.Book,
                nameof(Book),
                book.Id,
                $"Lectura: {book.Title}",
                cancellationToken);

            book.XpEarned += pageXp.AmountAwarded;

            if (pageXp.Milestone is not null)
            {
                events.Add(new AchievementEvent(
                    pageXp.Milestone.Title,
                    pageXp.Milestone.Description ?? book.Title,
                    pageXp.AmountAwarded,
                    MilestoneSourceType.Book));
            }
        }

        if (clampedPages >= book.TotalPages)
        {
            book.Status = BookStatus.Completed;
            book.CompletedAt = DateTimeHelper.ToUtcFromLocalDate(activityLocalDate);
            book.PagesRead = book.TotalPages;

            var remainingPages = Math.Max(0, book.TotalPages - previousPages);
            if (remainingPages > 0 && previousPages < book.TotalPages)
            {
                var pageXp = await _xpService.AwardXpAsync(
                    AchievementActionType.BookPageRead,
                    remainingPages,
                    $"Lectura final: {book.Title}",
                    MilestoneSourceType.Book,
                    nameof(Book),
                    book.Id,
                    milestoneTitle: null,
                    cancellationToken);

                book.XpEarned += pageXp.AmountAwarded;
            }

            var completionXp = await _xpService.AwardFlatBonusAsync(
                AchievementActionType.BookCompleted,
                await _xpService.CalculatePointsAsync(AchievementActionType.BookCompleted, 1, cancellationToken),
                $"Libro terminado: {book.Title}",
                MilestoneSourceType.Book,
                nameof(Book),
                book.Id,
                $"Libro terminado: {book.Title}",
                cancellationToken);

            book.XpEarned += completionXp.AmountAwarded;

            events.Add(new AchievementEvent(
                $"Libro terminado: {book.Title}",
                $"Completaste las {book.TotalPages} páginas.",
                completionXp.AmountAwarded,
                MilestoneSourceType.Book,
                RequiresCelebration: true));
        }

        await db.SaveChangesAsync(cancellationToken);

        if (pageDelta > 0 || clampedPages >= book.TotalPages)
        {
            events.AddRange(await _achievementEngine.TryAwardMilestonesForTrackAsync(
                MedalMilestoneTrack.BookPagesRead,
                MilestoneSourceType.Book,
                nameof(Book),
                book.Id,
                cancellationToken));
        }

        if (clampedPages >= book.TotalPages)
        {
            events.AddRange(await _achievementEngine.TryAwardMilestonesForTrackAsync(
                MedalMilestoneTrack.BooksCompleted,
                MilestoneSourceType.Book,
                nameof(Book),
                book.Id,
                cancellationToken));
        }

        if (pageDelta > 0)
            await _weeklyQuotaService.NotifyActivityAsync(MilestoneSourceType.Book, activityLocalDate, cancellationToken);

        return OperationResult<Book>.WithEvents(book, events.ToArray());
    }

    public async Task<Book?> UpdateMetadataAsync(
        int bookId,
        string title,
        string author,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("El título es obligatorio.", nameof(title));

        if (string.IsNullOrWhiteSpace(author))
            throw new ArgumentException("El autor es obligatorio.", nameof(author));

        await using var db = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        var book = await db.Books.FindAsync([bookId], cancellationToken);
        if (book is null)
            return null;

        book.Title = title.Trim();
        book.Author = author.Trim();
        book.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        return book;
    }
}
