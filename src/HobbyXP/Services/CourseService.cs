using HobbyXP.Data;
using HobbyXP.Helpers;
using HobbyXP.Models.Enums;
using HobbyXP.Models.PersonalGrowth;
using HobbyXP.Services.Abstractions;
using HobbyXP.Services.Results;
using Microsoft.EntityFrameworkCore;

namespace HobbyXP.Services;

public sealed class CourseService : ICourseService
{
    private readonly IDbContextFactory<HobbyXpDbContext> _dbContextFactory;
    private readonly IXpService _xpService;
    private readonly IAchievementEngineService _achievementEngine;

    public CourseService(
        IDbContextFactory<HobbyXpDbContext> dbContextFactory,
        IXpService xpService,
        IAchievementEngineService achievementEngine)
    {
        _dbContextFactory = dbContextFactory;
        _xpService = xpService;
        _achievementEngine = achievementEngine;
    }

    public async Task<IReadOnlyList<Course>> GetInProgressAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        return await db.Courses
            .AsNoTracking()
            .Where(c => c.Status == CourseStatus.InProgress)
            .OrderByDescending(c => c.UpdatedAt ?? c.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Course>> GetCompletedAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        return await db.Courses
            .AsNoTracking()
            .Where(c => c.Status == CourseStatus.Completed)
            .OrderByDescending(c => c.CompletedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<Course> RegisterAsync(
        string name,
        string platform,
        int totalSessions,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("El nombre es obligatorio.", nameof(name));

        if (totalSessions < 1)
            throw new ArgumentOutOfRangeException(nameof(totalSessions));

        await using var db = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        var course = new Course
        {
            Name = name.Trim(),
            Platform = platform.Trim(),
            TotalSessions = totalSessions,
            SessionsCompleted = 0,
            Status = CourseStatus.InProgress
        };

        db.Courses.Add(course);
        await db.SaveChangesAsync(cancellationToken);
        return course;
    }

    public async Task<OperationResult<Course>> LogSessionsAsync(
        int courseId,
        DateTime sessionDate,
        int sessionsDone,
        CancellationToken cancellationToken = default)
    {
        if (sessionsDone <= 0)
            throw new ArgumentOutOfRangeException(nameof(sessionsDone));

        await using var db = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        var course = await db.Courses.FindAsync([courseId], cancellationToken)
            ?? throw new InvalidOperationException($"No existe el curso con Id {courseId}.");

        if (course.Status == CourseStatus.Completed)
            return OperationResult<Course>.Empty(course);

        var remaining = course.TotalSessions - course.SessionsCompleted;
        var applied = Math.Min(sessionsDone, remaining);
        if (applied == 0)
            return OperationResult<Course>.Empty(course);

        db.CourseSessionLogs.Add(new CourseSessionLog
        {
            CourseId = course.Id,
            SessionDate = DateTimeHelper.ToUtcFromLocalDate(sessionDate),
            SessionsDone = applied
        });

        course.SessionsCompleted += applied;
        course.UpdatedAt = DateTime.UtcNow;

        var events = new List<AchievementEvent>();

        var sessionXp = await _xpService.AwardXpAsync(
            AchievementActionType.CourseSessionCompleted,
            applied,
            $"Curso: {course.Name} (+{applied} sesiones)",
            MilestoneSourceType.Course,
            nameof(Course),
            course.Id,
            $"Curso: {course.Name}",
            cancellationToken);

        course.XpEarned += sessionXp.AmountAwarded;

        if (sessionXp.Milestone is not null)
        {
            events.Add(new AchievementEvent(
                sessionXp.Milestone.Title,
                sessionXp.Milestone.Description ?? course.Name,
                sessionXp.AmountAwarded,
                MilestoneSourceType.Course));
        }

        if (course.SessionsCompleted >= course.TotalSessions)
        {
            course.Status = CourseStatus.Completed;
            course.CompletedAt = DateTime.UtcNow;

            var completeXp = await _xpService.AwardXpAsync(
                AchievementActionType.CourseCompleted,
                1,
                $"Curso terminado: {course.Name}",
                MilestoneSourceType.Course,
                nameof(Course),
                course.Id,
                $"Curso: {course.Name}",
                cancellationToken);

            course.XpEarned += completeXp.AmountAwarded;

            if (completeXp.Milestone is not null)
            {
                events.Add(new AchievementEvent(
                    completeXp.Milestone.Title,
                    completeXp.Milestone.Description ?? course.Name,
                    completeXp.AmountAwarded,
                    MilestoneSourceType.Course));
            }
        }

        await db.SaveChangesAsync(cancellationToken);

        events.AddRange(await _achievementEngine.TryAwardMilestonesForTrackAsync(
            MedalMilestoneTrack.CourseSessions,
            MilestoneSourceType.Course,
            nameof(Course),
            course.Id,
            cancellationToken));

        if (course.Status == CourseStatus.Completed)
        {
            events.AddRange(await _achievementEngine.TryAwardMilestonesForTrackAsync(
                MedalMilestoneTrack.CoursesCompleted,
                MilestoneSourceType.Course,
                nameof(Course),
                course.Id,
                cancellationToken));
        }

        return OperationResult<Course>.WithEvents(course, events.ToArray());
    }
}
