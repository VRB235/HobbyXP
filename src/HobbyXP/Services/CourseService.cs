using HobbyXP.Data;
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

    public CourseService(IDbContextFactory<HobbyXpDbContext> dbContextFactory, IXpService xpService)
    {
        _dbContextFactory = dbContextFactory;
        _xpService = xpService;
    }

    public async Task<IReadOnlyList<Course>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        return await db.Courses
            .AsNoTracking()
            .OrderByDescending(c => c.CompletedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<OperationResult<Course>> RegisterCompletedAsync(
        string name,
        string platform,
        DateTime? completedAt = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("El nombre es obligatorio.", nameof(name));

        await using var db = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        var course = new Course
        {
            Name = name.Trim(),
            Platform = platform.Trim(),
            CompletedAt = completedAt ?? DateTime.UtcNow
        };

        db.Courses.Add(course);
        await db.SaveChangesAsync(cancellationToken);

        var xpOutcome = await _xpService.AwardXpAsync(
            AchievementActionType.CourseCompleted,
            units: 1,
            $"Curso completado: {course.Name}",
            MilestoneSourceType.Course,
            nameof(Course),
            course.Id,
            $"Curso: {course.Name}",
            cancellationToken);

        course.XpEarned = xpOutcome.AmountAwarded;
        await db.SaveChangesAsync(cancellationToken);

        var events = new List<AchievementEvent>();
        if (xpOutcome.Milestone is not null)
        {
            events.Add(new AchievementEvent(
                xpOutcome.Milestone.Title,
                xpOutcome.Milestone.Description ?? course.Name,
                xpOutcome.AmountAwarded,
                MilestoneSourceType.Course));
        }

        return OperationResult<Course>.WithEvents(course, events.ToArray());
    }
}
