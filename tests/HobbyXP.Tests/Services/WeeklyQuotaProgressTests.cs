using HobbyXP.Helpers;
using HobbyXP.Models.Entertainment;
using HobbyXP.Models.Enums;
using HobbyXP.Models.PersonalGrowth;
using HobbyXP.Services;
using HobbyXP.Tests.Helpers;
using Microsoft.EntityFrameworkCore;

namespace HobbyXP.Tests.Services;

public sealed class WeeklyQuotaProgressTests : IDisposable
{
    private readonly TestDbContextFactory _factory;
    private readonly WeeklyQuotaService _sut;

    public WeeklyQuotaProgressTests()
    {
        _factory = new TestDbContextFactory();
        var xp = new XpService(_factory, new FakeLevelUpMessenger());
        _sut = new WeeklyQuotaService(_factory, xp);
    }

    public void Dispose() => _factory.Dispose();

    [Fact]
    public async Task Book_WithoutCurrentBook_IsNotApplicable()
    {
        var progress = await _sut.GetCurrentWeekProgressAsync();
        var book = progress.Single(p => p.SourceType == MilestoneSourceType.Book);

        Assert.False(book.IsApplicable);
        Assert.Equal(0, book.RequiredPrimary);
        Assert.Contains("20%", book.RequirementLabel, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Book_RequiresTwentyPercentOfCurrentBookPages()
    {
        var weekStart = WeekDateHelper.GetWeekStartLocal(DateTime.Today);
        await using (var db = _factory.CreateDbContext())
        {
            var book = new Book { Title = "Dune", Author = "Herbert", TotalPages = 500, Status = BookStatus.Reading };
            db.Books.Add(book);
            await db.SaveChangesAsync();
            db.BookReadingLogs.Add(new BookReadingLog
            {
                BookId = book.Id,
                ReadDate = DateTimeHelper.ToUtcFromLocalDate(weekStart),
                PagesDone = 50
            });
            await db.SaveChangesAsync();
        }

        var progress = await _sut.GetCurrentWeekProgressAsync();
        var bookQuota = progress.Single(p => p.SourceType == MilestoneSourceType.Book);

        Assert.Equal(100, bookQuota.RequiredPrimary);
        Assert.Equal(50, bookQuota.ActualPrimary);
        Assert.False(bookQuota.IsMet);
        Assert.Contains("Dune", bookQuota.RequirementLabel, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Book_TwentyPercentRead_MeetsQuota()
    {
        var weekStart = WeekDateHelper.GetWeekStartLocal(DateTime.Today);
        await using (var db = _factory.CreateDbContext())
        {
            var book = new Book { Title = "Dune", Author = "Herbert", TotalPages = 500, Status = BookStatus.Reading };
            db.Books.Add(book);
            await db.SaveChangesAsync();
            db.BookReadingLogs.Add(new BookReadingLog
            {
                BookId = book.Id,
                ReadDate = DateTimeHelper.ToUtcFromLocalDate(weekStart.AddDays(1)),
                PagesDone = 100
            });
            await db.SaveChangesAsync();
        }

        var progress = await _sut.GetCurrentWeekProgressAsync();
        var bookQuota = progress.Single(p => p.SourceType == MilestoneSourceType.Book);

        Assert.True(bookQuota.IsMet);
        Assert.Equal(100, bookQuota.ActualPrimary);
    }

    [Fact]
    public async Task Book_CompletedThisWeek_MeetsQuota()
    {
        var weekStart = WeekDateHelper.GetWeekStartLocal(DateTime.Today);
        await using (var db = _factory.CreateDbContext())
        {
            db.Books.Add(new Book
            {
                Title = "Corto",
                Author = "Autor",
                TotalPages = 500,
                PagesRead = 500,
                Status = BookStatus.Completed,
                CompletedAt = DateTimeHelper.ToUtcFromLocalDate(weekStart.AddDays(2))
            });
            await db.SaveChangesAsync();
        }

        var progress = await _sut.GetCurrentWeekProgressAsync();
        var bookQuota = progress.Single(p => p.SourceType == MilestoneSourceType.Book);

        Assert.True(bookQuota.IsMet);
        Assert.Contains("terminado", bookQuota.RequirementLabel, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Course_WithoutActiveCourse_IsNotApplicable()
    {
        var progress = await _sut.GetCurrentWeekProgressAsync();
        var course = progress.Single(p => p.SourceType == MilestoneSourceType.Course);

        Assert.False(course.IsApplicable);
        Assert.Equal(0, course.RequiredPrimary);
    }

    [Fact]
    public async Task Course_RequiresFiveSessions()
    {
        var weekStart = WeekDateHelper.GetWeekStartLocal(DateTime.Today);
        await using (var db = _factory.CreateDbContext())
        {
            var course = new Course
            {
                Name = "Azure",
                Platform = "Learn",
                TotalSessions = 10,
                Status = CourseStatus.InProgress
            };
            db.Courses.Add(course);
            await db.SaveChangesAsync();
            db.CourseSessionLogs.Add(new CourseSessionLog
            {
                CourseId = course.Id,
                SessionDate = DateTimeHelper.ToUtcFromLocalDate(weekStart),
                SessionsDone = 4
            });
            await db.SaveChangesAsync();
        }

        var progress = await _sut.GetCurrentWeekProgressAsync();
        var courseQuota = progress.Single(p => p.SourceType == MilestoneSourceType.Course);

        Assert.Equal(5, courseQuota.RequiredPrimary);
        Assert.Equal(4, courseQuota.ActualPrimary);
        Assert.False(courseQuota.IsMet);
    }

    [Fact]
    public async Task Course_FiveSessions_MeetsQuota()
    {
        var weekStart = WeekDateHelper.GetWeekStartLocal(DateTime.Today);
        await using (var db = _factory.CreateDbContext())
        {
            var course = new Course
            {
                Name = "Azure",
                Platform = "Learn",
                TotalSessions = 10,
                Status = CourseStatus.InProgress
            };
            db.Courses.Add(course);
            await db.SaveChangesAsync();
            db.CourseSessionLogs.Add(new CourseSessionLog
            {
                CourseId = course.Id,
                SessionDate = DateTimeHelper.ToUtcFromLocalDate(weekStart.AddDays(3)),
                SessionsDone = 5
            });
            await db.SaveChangesAsync();
        }

        var progress = await _sut.GetCurrentWeekProgressAsync();
        var courseQuota = progress.Single(p => p.SourceType == MilestoneSourceType.Course);

        Assert.True(courseQuota.IsMet);
    }

    [Fact]
    public async Task Media_WatchingChaptersWithoutFinishing_DoesNotMeetSeriesQuota()
    {
        var weekStart = WeekDateHelper.GetWeekStartLocal(DateTime.Today);
        await using (var db = _factory.CreateDbContext())
        {
            var series = new MediaSeries
            {
                Title = "Breaking Bad",
                TotalChapters = 10,
                ChaptersWatched = 2,
                Status = MediaSeriesStatus.InProgress
            };
            db.MediaSeries.Add(series);
            await db.SaveChangesAsync();
            db.MediaSeriesChapterLogs.Add(new MediaSeriesChapterLog
            {
                MediaSeriesId = series.Id,
                WatchDate = DateTimeHelper.ToUtcFromLocalDate(weekStart),
                ChaptersDone = 2
            });
            db.MediaEntries.Add(new MediaEntry
            {
                Title = "Película A",
                MediaType = MediaType.Movie,
                CompletedAt = DateTimeHelper.ToUtcFromLocalDate(weekStart)
            });
            db.MediaEntries.Add(new MediaEntry
            {
                Title = "Película B",
                MediaType = MediaType.Movie,
                CompletedAt = DateTimeHelper.ToUtcFromLocalDate(weekStart.AddDays(1))
            });
            await db.SaveChangesAsync();
        }

        var progress = await _sut.GetCurrentWeekProgressAsync();
        var media = progress.Single(p => p.SourceType == MilestoneSourceType.Media);

        Assert.Equal(1, media.RequiredPrimary);
        Assert.Equal(0, media.ActualPrimary);
        Assert.Equal(2, media.ActualSecondary);
        Assert.False(media.IsMet);
        Assert.Contains("serie terminada", media.RequirementLabel, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Media_CompletedSeriesAndTwoMovies_MeetsQuota()
    {
        var weekStart = WeekDateHelper.GetWeekStartLocal(DateTime.Today);
        await using (var db = _factory.CreateDbContext())
        {
            db.MediaEntries.Add(new MediaEntry
            {
                Title = "Breaking Bad",
                MediaType = MediaType.Series,
                CompletedAt = DateTimeHelper.ToUtcFromLocalDate(weekStart.AddDays(4))
            });
            db.MediaEntries.Add(new MediaEntry
            {
                Title = "Película A",
                MediaType = MediaType.Movie,
                CompletedAt = DateTimeHelper.ToUtcFromLocalDate(weekStart)
            });
            db.MediaEntries.Add(new MediaEntry
            {
                Title = "Película B",
                MediaType = MediaType.Movie,
                CompletedAt = DateTimeHelper.ToUtcFromLocalDate(weekStart.AddDays(1))
            });
            await db.SaveChangesAsync();
        }

        var progress = await _sut.GetCurrentWeekProgressAsync();
        var media = progress.Single(p => p.SourceType == MilestoneSourceType.Media);

        Assert.Equal(1, media.ActualPrimary);
        Assert.Equal(2, media.ActualSecondary);
        Assert.True(media.IsMet);
    }

    [Fact]
    public async Task EvaluateClosedWeeks_WithoutBook_DoesNotPenalizeBook()
    {
        await using (var db = _factory.CreateDbContext())
        {
            var profile = await db.PlayerProfiles.SingleAsync();
            profile.WeeklyQuotaTrackingStartedAtUtc = DateTime.SpecifyKind(
                DateTime.Today.AddDays(-21),
                DateTimeKind.Utc);
            await db.SaveChangesAsync();
        }

        await _sut.EvaluateClosedWeeksAsync();

        await using (var db = _factory.CreateDbContext())
        {
            Assert.False(await db.WeeklyQuotaEvaluations.AnyAsync(e => e.SourceType == MilestoneSourceType.Book));
            Assert.False(await db.WeeklyQuotaEvaluations.AnyAsync(e => e.SourceType == MilestoneSourceType.Course));
        }
    }
}
