using HobbyXP.Data;
using HobbyXP.Helpers;
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
    private readonly IAchievementEngineService _achievementEngine;
    private readonly IWeeklyQuotaService _weeklyQuotaService;

    public MediaService(
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

    public async Task<IReadOnlyList<MediaEntry>> GetHistoryAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        return await db.MediaEntries
            .AsNoTracking()
            .OrderByDescending(m => m.CompletedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<MediaSeries>> GetInProgressSeriesAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        return await db.MediaSeries
            .AsNoTracking()
            .Where(s => s.Status == MediaSeriesStatus.InProgress)
            .OrderByDescending(s => s.UpdatedAt ?? s.CreatedAt)
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
        DateTime? completedAt = null,
        string? imageSourcePath = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("El título es obligatorio.", nameof(title));

        await using var db = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        var entry = new MediaEntry
        {
            Title = title.Trim(),
            MediaType = mediaType,
            CompletedAt = completedAt ?? DateTime.UtcNow
        };

        db.MediaEntries.Add(entry);
        await db.SaveChangesAsync(cancellationToken);

        if (!string.IsNullOrWhiteSpace(imageSourcePath))
        {
            entry.ImagePath = HobbyCoverPhotoStorage.SaveFromSource(
                HobbyCoverPhotoStorage.Folders.MediaEntries,
                entry.Id,
                imageSourcePath);
            await db.SaveChangesAsync(cancellationToken);
        }

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

        var medalEvents = await _achievementEngine.TryAwardMilestonesForTrackAsync(
            MedalMilestoneTrack.MediaCompleted,
            MilestoneSourceType.Media,
            nameof(MediaEntry),
            entry.Id,
            cancellationToken);

        events.AddRange(medalEvents);

        var activityLocal = (completedAt ?? DateTime.UtcNow).ToLocalTime().Date;
        await _weeklyQuotaService.NotifyActivityAsync(MilestoneSourceType.Media, activityLocal, cancellationToken);

        return OperationResult<MediaEntry>.WithEvents(entry, events.ToArray());
    }

    public async Task<MediaSeries> RegisterSeriesAsync(
        string title,
        int totalChapters,
        string? imageSourcePath = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("El título es obligatorio.", nameof(title));

        if (totalChapters < 1)
            throw new ArgumentOutOfRangeException(nameof(totalChapters));

        await using var db = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        var series = new MediaSeries
        {
            Title = title.Trim(),
            TotalChapters = totalChapters,
            ChaptersWatched = 0,
            Status = MediaSeriesStatus.InProgress
        };

        db.MediaSeries.Add(series);
        await db.SaveChangesAsync(cancellationToken);

        if (!string.IsNullOrWhiteSpace(imageSourcePath))
        {
            series.ImagePath = HobbyCoverPhotoStorage.SaveFromSource(
                HobbyCoverPhotoStorage.Folders.MediaSeries,
                series.Id,
                imageSourcePath);
            await db.SaveChangesAsync(cancellationToken);
        }

        return series;
    }

    public async Task<OperationResult<MediaSeries>> LogChaptersAsync(
        int seriesId,
        DateTime watchDate,
        int chaptersDone,
        CancellationToken cancellationToken = default)
    {
        if (chaptersDone <= 0)
            throw new ArgumentOutOfRangeException(nameof(chaptersDone));

        await using var db = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        var series = await db.MediaSeries.FindAsync([seriesId], cancellationToken)
            ?? throw new InvalidOperationException($"No existe la serie con Id {seriesId}.");

        if (series.Status == MediaSeriesStatus.Completed)
            return OperationResult<MediaSeries>.Empty(series);

        var remaining = series.TotalChapters - series.ChaptersWatched;
        var applied = Math.Min(chaptersDone, remaining);
        if (applied == 0)
            return OperationResult<MediaSeries>.Empty(series);

        db.MediaSeriesChapterLogs.Add(new MediaSeriesChapterLog
        {
            MediaSeriesId = series.Id,
            WatchDate = DateTimeHelper.ToUtcFromLocalDate(watchDate),
            ChaptersDone = applied
        });

        series.ChaptersWatched += applied;
        series.UpdatedAt = DateTime.UtcNow;

        var events = new List<AchievementEvent>();

        var chapterXp = await _xpService.AwardXpAsync(
            AchievementActionType.MediaChapterWatched,
            applied,
            $"Serie: {series.Title} (+{applied} capítulos)",
            MilestoneSourceType.Media,
            nameof(MediaSeries),
            series.Id,
            $"Serie: {series.Title}",
            cancellationToken);

        series.XpEarned += chapterXp.AmountAwarded;

        if (chapterXp.Milestone is not null)
        {
            events.Add(new AchievementEvent(
                chapterXp.Milestone.Title,
                chapterXp.Milestone.Description ?? series.Title,
                chapterXp.AmountAwarded,
                MilestoneSourceType.Media));
        }

        if (series.ChaptersWatched >= series.TotalChapters)
        {
            series.Status = MediaSeriesStatus.Completed;
            series.CompletedAt = DateTime.UtcNow;

            var historyEntry = new MediaEntry
            {
                Title = series.Title,
                MediaType = MediaType.Series,
                CompletedAt = series.CompletedAt.Value
            };

            db.MediaEntries.Add(historyEntry);
            await db.SaveChangesAsync(cancellationToken);

            if (!string.IsNullOrWhiteSpace(series.ImagePath))
            {
                var absolute = HobbyCoverPhotoStorage.ResolveAbsolutePath(series.ImagePath);
                if (absolute is not null)
                {
                    historyEntry.ImagePath = HobbyCoverPhotoStorage.SaveFromSource(
                        HobbyCoverPhotoStorage.Folders.MediaEntries,
                        historyEntry.Id,
                        absolute);
                }
            }

            series.CompletedMediaEntryId = historyEntry.Id;

            var completeXp = await _xpService.AwardXpAsync(
                AchievementActionType.MediaCompleted,
                1,
                $"Serie terminada: {series.Title}",
                MilestoneSourceType.Media,
                nameof(MediaEntry),
                historyEntry.Id,
                $"Serie: {series.Title}",
                cancellationToken);

            series.XpEarned += completeXp.AmountAwarded;
            historyEntry.XpEarned = completeXp.AmountAwarded;

            if (completeXp.Milestone is not null)
            {
                events.Add(new AchievementEvent(
                    completeXp.Milestone.Title,
                    completeXp.Milestone.Description ?? series.Title,
                    completeXp.AmountAwarded,
                    MilestoneSourceType.Media));
            }

            events.AddRange(await _achievementEngine.TryAwardMilestonesForTrackAsync(
                MedalMilestoneTrack.MediaCompleted,
                MilestoneSourceType.Media,
                nameof(MediaEntry),
                historyEntry.Id,
                cancellationToken));
        }

        await db.SaveChangesAsync(cancellationToken);

        await _weeklyQuotaService.NotifyActivityAsync(MilestoneSourceType.Media, watchDate.Date, cancellationToken);

        return OperationResult<MediaSeries>.WithEvents(series, events.ToArray());
    }

    public async Task<MediaSeries> UpdateSeriesImageAsync(
        int seriesId,
        string? imageSourcePath = null,
        bool clearImage = false,
        CancellationToken cancellationToken = default)
    {
        await using var db = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        var series = await db.MediaSeries.FindAsync([seriesId], cancellationToken)
            ?? throw new InvalidOperationException($"No existe la serie con Id {seriesId}.");

        const string folder = HobbyCoverPhotoStorage.Folders.MediaSeries;

        if (clearImage)
        {
            HobbyCoverPhotoStorage.DeleteStoredPhoto(folder, series.Id, series.ImagePath);
            series.ImagePath = null;
        }
        else if (!string.IsNullOrWhiteSpace(imageSourcePath))
        {
            HobbyCoverPhotoStorage.DeleteStoredPhoto(folder, series.Id, series.ImagePath);
            series.ImagePath = HobbyCoverPhotoStorage.SaveFromSource(folder, series.Id, imageSourcePath);
        }
        else
        {
            series.ImagePath = HobbyCoverPhotoStorage.EnsureManaged(folder, series.Id, series.ImagePath);
        }

        series.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        return series;
    }

    public async Task<MediaEntry> UpdateEntryAsync(
        int entryId,
        string title,
        MediaType mediaType,
        DateTime completedAt,
        string? imageSourcePath = null,
        bool clearImage = false,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("El título es obligatorio.", nameof(title));

        await using var db = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        var entry = await db.MediaEntries.FindAsync([entryId], cancellationToken)
            ?? throw new InvalidOperationException($"No existe la obra con Id {entryId}.");

        entry.Title = title.Trim();
        entry.MediaType = mediaType;
        entry.CompletedAt = completedAt;
        entry.UpdatedAt = DateTime.UtcNow;

        ApplyCoverImage(HobbyCoverPhotoStorage.Folders.MediaEntries, entry.Id, entry, imageSourcePath, clearImage);

        await db.SaveChangesAsync(cancellationToken);
        return entry;
    }

    public async Task<bool> DeleteAsync(int entryId, CancellationToken cancellationToken = default)
    {
        await using var db = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        var entry = await db.MediaEntries.FindAsync([entryId], cancellationToken);
        if (entry is null)
            return false;

        await _xpService.RevokeXpForSourceAsync(
            MilestoneSourceType.Media,
            nameof(MediaEntry),
            entryId,
            $"Eliminado del historial: {GetMediaLabel(entry.MediaType)} {entry.Title}",
            cancellationToken);

        HobbyCoverPhotoStorage.DeleteEntityFolder(HobbyCoverPhotoStorage.Folders.MediaEntries, entryId);
        db.MediaEntries.Remove(entry);
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }

    private static void ApplyCoverImage(
        string folder,
        int entityId,
        MediaEntry entry,
        string? imageSourcePath,
        bool clearImage)
    {
        if (clearImage)
        {
            HobbyCoverPhotoStorage.DeleteStoredPhoto(folder, entityId, entry.ImagePath);
            entry.ImagePath = null;
            return;
        }

        if (!string.IsNullOrWhiteSpace(imageSourcePath))
        {
            HobbyCoverPhotoStorage.DeleteStoredPhoto(folder, entityId, entry.ImagePath);
            entry.ImagePath = HobbyCoverPhotoStorage.SaveFromSource(folder, entityId, imageSourcePath);
            return;
        }

        entry.ImagePath = HobbyCoverPhotoStorage.EnsureManaged(folder, entityId, entry.ImagePath);
    }

    private static string GetMediaLabel(MediaType mediaType) => mediaType switch
    {
        MediaType.Movie => "Película",
        MediaType.Series => "Serie",
        _ => "Obra"
    };
}
