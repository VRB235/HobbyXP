using HobbyXP.Data;
using HobbyXP.Models.Entertainment;
using HobbyXP.Models.Enums;
using HobbyXP.Services.Abstractions;
using HobbyXP.Services.Results;
using Microsoft.EntityFrameworkCore;

namespace HobbyXP.Services;

public sealed class MediaService : IMediaService
{
    private readonly IDbContextFactory<HobbyXpDbContext> _dbContextFactory;
    private readonly IXpService _xpService;

    public MediaService(IDbContextFactory<HobbyXpDbContext> dbContextFactory, IXpService xpService)
    {
        _dbContextFactory = dbContextFactory;
        _xpService = xpService;
    }

    public async Task<IReadOnlyList<MediaEntry>> GetHistoryAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        return await db.MediaEntries
            .AsNoTracking()
            .OrderByDescending(m => m.CompletedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<MediaYearlyCounters> GetYearlyCountersAsync(
        int? year = null,
        CancellationToken cancellationToken = default)
    {
        var targetYear = year ?? DateTime.UtcNow.Year;
        await using var db = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        var entries = await db.MediaEntries
            .AsNoTracking()
            .Where(m => m.CompletedAt.Year == targetYear)
            .ToListAsync(cancellationToken);

        var movies = entries.Count(m => m.MediaType == MediaType.Movie);
        var series = entries.Count(m => m.MediaType == MediaType.Series);

        return new MediaYearlyCounters(targetYear, movies, series, movies + series);
    }

    public async Task<OperationResult<MediaEntry>> RegisterCompletedAsync(
        string title,
        MediaType mediaType,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("El título es obligatorio.", nameof(title));

        await using var db = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        var entry = new MediaEntry
        {
            Title = title.Trim(),
            MediaType = mediaType,
            CompletedAt = DateTime.UtcNow
        };

        db.MediaEntries.Add(entry);
        await db.SaveChangesAsync(cancellationToken);

        var xpOutcome = await _xpService.AwardXpAsync(
            AchievementActionType.MediaCompleted,
            units: 1,
            $"Obra terminada: {entry.Title}",
            MilestoneSourceType.Media,
            nameof(MediaEntry),
            entry.Id,
            $"{GetMediaLabel(mediaType)}: {entry.Title}",
            cancellationToken);

        entry.XpEarned = xpOutcome.AmountAwarded;
        await db.SaveChangesAsync(cancellationToken);

        var events = new List<AchievementEvent>();
        if (xpOutcome.Milestone is not null)
        {
            events.Add(new AchievementEvent(
                xpOutcome.Milestone.Title,
                xpOutcome.Milestone.Description ?? entry.Title,
                xpOutcome.AmountAwarded,
                MilestoneSourceType.Media));
        }

        return OperationResult<MediaEntry>.WithEvents(entry, events.ToArray());
    }

    private static string GetMediaLabel(MediaType mediaType) => mediaType switch
    {
        MediaType.Movie => "Película",
        MediaType.Series => "Serie",
        _ => "Obra"
    };
}
