using HobbyXP.Helpers;
using HobbyXP.Models.Entertainment;
using HobbyXP.Models.Enums;
using HobbyXP.Models.PersonalGrowth;
using HobbyXP.Models.Physical;
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
        Assert.Equal(0, book.DailyRequiredPrimary);
    }

    [Fact]
    public async Task Book_DailyRequiresTwentyPercent_WeeklyRequiresOneCompleted()
    {
        await using (var db = _factory.CreateDbContext())
        {
            var book = new Book { Title = "Dune", Author = "Herbert", TotalPages = 500, Status = BookStatus.Reading };
            db.Books.Add(book);
            await db.SaveChangesAsync();
            db.BookReadingLogs.Add(new BookReadingLog
            {
                BookId = book.Id,
                ReadDate = DateTimeHelper.ToUtcFromLocalDate(DateTime.Today),
                PagesDone = 50
            });
            await db.SaveChangesAsync();
        }

        var progress = await _sut.GetCurrentWeekProgressAsync();
        var bookQuota = progress.Single(p => p.SourceType == MilestoneSourceType.Book);

        Assert.Equal(1, bookQuota.RequiredPrimary);
        Assert.Equal(0, bookQuota.ActualPrimary);
        Assert.False(bookQuota.IsWeeklyMet);
        Assert.Equal(100, bookQuota.DailyRequiredPrimary);
        Assert.Equal(50, bookQuota.DailyActualPrimary);
        Assert.False(bookQuota.IsDailyMet);
        Assert.False(bookQuota.IsMet);
        Assert.Contains("Dune", bookQuota.RequirementLabel, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Book_DailyTwentyPercentRead_MarksCumplida()
    {
        await using (var db = _factory.CreateDbContext())
        {
            var book = new Book { Title = "Dune", Author = "Herbert", TotalPages = 500, Status = BookStatus.Reading };
            db.Books.Add(book);
            await db.SaveChangesAsync();
            db.BookReadingLogs.Add(new BookReadingLog
            {
                BookId = book.Id,
                ReadDate = DateTimeHelper.ToUtcFromLocalDate(DateTime.Today),
                PagesDone = 100
            });
            await db.SaveChangesAsync();
        }

        var progress = await _sut.GetCurrentWeekProgressAsync();
        var bookQuota = progress.Single(p => p.SourceType == MilestoneSourceType.Book);

        Assert.True(bookQuota.IsDailyMet);
        Assert.True(bookQuota.IsMet);
        Assert.False(bookQuota.IsWeeklyMet);
        Assert.Equal(100, bookQuota.DailyActualPrimary);
    }

    [Fact]
    public async Task Book_CompletedThisWeek_MeetsWeeklyQuota()
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

        Assert.True(bookQuota.IsWeeklyMet);
        Assert.Equal(1, bookQuota.ActualPrimary);
        Assert.Contains("libro terminado", bookQuota.RequirementLabel, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Course_WithoutActiveCourse_IsNotApplicable()
    {
        var progress = await _sut.GetCurrentWeekProgressAsync();
        var course = progress.Single(p => p.SourceType == MilestoneSourceType.Course);

        Assert.False(course.IsApplicable);
        Assert.Equal(0, course.RequiredPrimary);
        Assert.Equal(0, course.DailyRequiredPrimary);
    }

    [Fact]
    public async Task Course_DailyOneSession_WeeklyFive()
    {
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
                SessionDate = DateTimeHelper.ToUtcFromLocalDate(DateTime.Today),
                SessionsDone = 1
            });
            await db.SaveChangesAsync();
        }

        var progress = await _sut.GetCurrentWeekProgressAsync();
        var courseQuota = progress.Single(p => p.SourceType == MilestoneSourceType.Course);

        Assert.Equal(5, courseQuota.RequiredPrimary);
        Assert.Equal(1, courseQuota.ActualPrimary);
        Assert.False(courseQuota.IsWeeklyMet);
        Assert.Equal(1, courseQuota.DailyRequiredPrimary);
        Assert.Equal(1, courseQuota.DailyActualPrimary);
        Assert.True(courseQuota.IsDailyMet);
        Assert.True(courseQuota.IsMet);
    }

    [Fact]
    public async Task Course_FiveSessions_MeetsWeeklyQuota()
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

        Assert.True(courseQuota.IsWeeklyMet);
        Assert.Equal(5, courseQuota.ActualPrimary);
    }

    [Fact]
    public async Task Gym_RequiresOneDailyAndFiveWeekly()
    {
        await using (var db = _factory.CreateDbContext())
        {
            db.GymWorkouts.Add(new GymWorkout
            {
                WorkoutDate = DateTimeHelper.ToUtcFromLocalDate(DateTime.Today),
                Notes = "pierna"
            });
            await db.SaveChangesAsync();
        }

        var progress = await _sut.GetCurrentWeekProgressAsync();
        var gym = progress.Single(p => p.SourceType == MilestoneSourceType.Gym);

        Assert.Equal(5, gym.RequiredPrimary);
        Assert.Equal(1, gym.ActualPrimary);
        Assert.Equal(1, gym.DailyRequiredPrimary);
        Assert.Equal(1, gym.DailyActualPrimary);
        Assert.True(gym.IsDailyMet);
        Assert.True(gym.IsMet);
        Assert.False(gym.IsWeeklyMet);
    }

    [Fact]
    public async Task Running_RequiresOneDailyAndFourWeekly()
    {
        await using (var db = _factory.CreateDbContext())
        {
            db.RunningSessions.Add(new RunningSession
            {
                RecordedAt = DateTimeHelper.ToUtcFromLocalDate(DateTime.Today),
                DistanceKm = 5,
                Duration = TimeSpan.FromMinutes(30)
            });
            await db.SaveChangesAsync();
        }

        var progress = await _sut.GetCurrentWeekProgressAsync();
        var running = progress.Single(p => p.SourceType == MilestoneSourceType.Running);

        Assert.Equal(4, running.RequiredPrimary);
        Assert.Equal(1, running.ActualPrimary);
        Assert.Equal(1, running.DailyRequiredPrimary);
        Assert.True(running.IsDailyMet);
        Assert.True(running.IsMet);
        Assert.False(running.IsWeeklyMet);
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
            Assert.False(await db.DailyQuotaEvaluations.AnyAsync(e => e.SourceType == MilestoneSourceType.Book));
            Assert.False(await db.DailyQuotaEvaluations.AnyAsync(e => e.SourceType == MilestoneSourceType.Course));
        }
    }

    [Fact]
    public async Task EvaluateClosedDays_WithoutGymSession_PenalizesGym()
    {
        var start = DateTime.Today.AddDays(-3);
        await using (var db = _factory.CreateDbContext())
        {
            var profile = await db.PlayerProfiles.SingleAsync();
            profile.WeeklyQuotaTrackingStartedAtUtc = DateTimeHelper.ToUtcFromLocalDate(start);
            profile.DailyQuotaTrackingStartedAtUtc = DateTimeHelper.ToUtcFromLocalDate(start);
            db.HobbyProgresses.Add(new Models.Core.HobbyProgress
            {
                PlayerProfileId = profile.Id,
                SourceType = MilestoneSourceType.Gym,
                TotalXp = 1500,
                CurrentLevel = 2,
                SpendableXp = 1500
            });
            await db.SaveChangesAsync();
        }

        await _sut.EvaluateClosedWeeksAsync();

        await using (var db = _factory.CreateDbContext())
        {
            Assert.True(await db.DailyQuotaEvaluations.AnyAsync(
                e => e.SourceType == MilestoneSourceType.Gym &&
                     e.Status == WeeklyQuotaStatus.Penalized));
        }
    }

    [Fact]
    public async Task DailyTracking_StartsToday_DoesNotPenalizePastDaysOnFirstRun()
    {
        await using (var db = _factory.CreateDbContext())
        {
            var profile = await db.PlayerProfiles.SingleAsync();
            profile.WeeklyQuotaTrackingStartedAtUtc = DateTimeHelper.ToUtcFromLocalDate(DateTime.Today.AddDays(-10));
            db.HobbyProgresses.Add(new Models.Core.HobbyProgress
            {
                PlayerProfileId = profile.Id,
                SourceType = MilestoneSourceType.Gym,
                TotalXp = 1500,
                CurrentLevel = 2,
                SpendableXp = 1500
            });
            await db.SaveChangesAsync();
        }

        await _sut.EvaluateClosedWeeksAsync();

        await using (var db = _factory.CreateDbContext())
        {
            Assert.False(await db.DailyQuotaEvaluations.AnyAsync(e => e.SourceType == MilestoneSourceType.Gym));
            var profile = await db.PlayerProfiles.SingleAsync();
            Assert.NotNull(profile.DailyQuotaTrackingStartedAtUtc);
            Assert.Equal(
                DateTimeHelper.ToUtcFromLocalDate(DateTime.Today),
                profile.DailyQuotaTrackingStartedAtUtc);
        }
    }

    [Fact]
    public async Task OpenDay_IsNotPenalized_ShowsMetWhenQuotaDone()
    {
        await using (var db = _factory.CreateDbContext())
        {
            var book = new Book { Title = "Dune", Author = "Herbert", TotalPages = 410, Status = BookStatus.Reading };
            db.Books.Add(book);
            await db.SaveChangesAsync();
            db.BookReadingLogs.Add(new BookReadingLog
            {
                BookId = book.Id,
                ReadDate = DateTimeHelper.ToUtcFromLocalDate(DateTime.Today),
                PagesDone = 82
            });
            await db.SaveChangesAsync();
        }

        await _sut.EvaluateClosedWeeksAsync();
        await _sut.NotifyActivityAsync(MilestoneSourceType.Book, DateTime.Today);

        var progress = await _sut.GetCurrentWeekProgressAsync();
        var bookQuota = progress.Single(p => p.SourceType == MilestoneSourceType.Book);

        Assert.True(bookQuota.IsDailyMet);
        Assert.True(bookQuota.IsMet);
        Assert.False(bookQuota.HasActivePenalty);
        await using (var db = _factory.CreateDbContext())
        {
            Assert.False(await db.DailyQuotaEvaluations.AnyAsync(
                e => e.SourceType == MilestoneSourceType.Book &&
                     e.Status == WeeklyQuotaStatus.Penalized));
        }
    }
}
