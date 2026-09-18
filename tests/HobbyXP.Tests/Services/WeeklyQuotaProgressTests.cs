using HobbyXP.Helpers;
using HobbyXP.Models.Enums;
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
    public async Task CurrentWeekProgress_OnlyTracksRunningGymAndDiet()
    {
        var progress = await _sut.GetCurrentWeekProgressAsync();
        var sources = progress.Select(p => p.SourceType).ToHashSet();

        Assert.Equal(
            new HashSet<MilestoneSourceType>
            {
                MilestoneSourceType.Running,
                MilestoneSourceType.Gym,
                MilestoneSourceType.Diet
            },
            sources);
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
    public async Task EvaluateClosedWeeks_DoesNotPenalizeEntertainmentOrGrowthHobbies()
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
            var untracked =
                new[]
                {
                    MilestoneSourceType.Puzzle,
                    MilestoneSourceType.Media,
                    MilestoneSourceType.VideoGame,
                    MilestoneSourceType.Book,
                    MilestoneSourceType.Course
                };

            Assert.False(await db.WeeklyQuotaEvaluations.AnyAsync(e => untracked.Contains(e.SourceType)));
            Assert.False(await db.DailyQuotaEvaluations.AnyAsync(e => untracked.Contains(e.SourceType)));
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
    public async Task NotifyActivity_OnUntrackedHobby_DoesNotCreateEvaluations()
    {
        await _sut.NotifyActivityAsync(MilestoneSourceType.Book, DateTime.Today);
        await _sut.NotifyActivityAsync(MilestoneSourceType.Media, DateTime.Today);
        await _sut.NotifyActivityAsync(MilestoneSourceType.Puzzle, DateTime.Today);

        await using var db = _factory.CreateDbContext();
        Assert.False(await db.WeeklyQuotaEvaluations.AnyAsync());
        Assert.False(await db.DailyQuotaEvaluations.AnyAsync());
    }
}
