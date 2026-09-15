using HobbyXP.Models.Enums;
using HobbyXP.Models.Physical;
using HobbyXP.Services;
using HobbyXP.Tests.Helpers;
using Microsoft.EntityFrameworkCore;

namespace HobbyXP.Tests.Services;

public sealed class RunningServiceTests : IDisposable
{
    private readonly TestDbContextFactory _factory;
    private readonly RunningService _sut;

    public RunningServiceTests()
    {
        _factory = new TestDbContextFactory();
        var xp = new XpService(_factory, new FakeLevelUpMessenger());
        var achievements = new AchievementEngineService(_factory);
        var quota = new WeeklyQuotaService(_factory, xp);
        _sut = new RunningService(_factory, xp, achievements, quota);
    }

    public void Dispose() => _factory.Dispose();

    [Fact]
    public async Task GetLatestSessionByTypeAsync_ReturnsNewestMatchingSessionWithSeries()
    {
        await using (var db = _factory.CreateDbContext())
        {
            db.RunningSessions.Add(CreateSession(
                RunningSessionType.Umbral,
                new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc),
                6m,
                seriesCount: 3));
            db.RunningSessions.Add(CreateSession(
                RunningSessionType.TiradaLarga,
                new DateTime(2026, 9, 12, 0, 0, 0, DateTimeKind.Utc),
                22m));
            var latestUmbral = CreateSession(
                RunningSessionType.Umbral,
                new DateTime(2026, 9, 10, 0, 0, 0, DateTimeKind.Utc),
                8m,
                seriesCount: 5);
            db.RunningSessions.Add(latestUmbral);
            await db.SaveChangesAsync();
        }

        var found = await _sut.GetLatestSessionByTypeAsync(RunningSessionType.Umbral);

        Assert.NotNull(found);
        Assert.Equal(RunningSessionType.Umbral, found.SessionType);
        Assert.Equal(8m, found.DistanceKm);
        Assert.Equal(5, found.Series.Count);
        Assert.Equal(1, found.Series.Min(s => s.SortOrder));
        Assert.Equal(5, found.Series.Max(s => s.SortOrder));
    }

    [Fact]
    public async Task GetLatestSessionByTypeAsync_SameDate_PrefersHigherId()
    {
        var day = new DateTime(2026, 9, 10, 0, 0, 0, DateTimeKind.Utc);
        int firstId;
        int secondId;
        await using (var db = _factory.CreateDbContext())
        {
            var first = CreateSession(RunningSessionType.Regenerativa, day, 5m);
            db.RunningSessions.Add(first);
            await db.SaveChangesAsync();
            firstId = first.Id;

            var second = CreateSession(RunningSessionType.Regenerativa, day, 7m);
            db.RunningSessions.Add(second);
            await db.SaveChangesAsync();
            secondId = second.Id;
        }

        var found = await _sut.GetLatestSessionByTypeAsync(RunningSessionType.Regenerativa);

        Assert.NotNull(found);
        Assert.True(secondId > firstId);
        Assert.Equal(secondId, found.Id);
        Assert.Equal(7m, found.DistanceKm);
    }

    [Fact]
    public async Task GetLatestSessionByTypeAsync_NoMatch_ReturnsNull()
    {
        await using (var db = _factory.CreateDbContext())
        {
            db.RunningSessions.Add(CreateSession(
                RunningSessionType.Regenerativa,
                DateTime.UtcNow,
                4m));
            await db.SaveChangesAsync();
        }

        var found = await _sut.GetLatestSessionByTypeAsync(RunningSessionType.TiradaLarga);

        Assert.Null(found);
    }

    private static RunningSession CreateSession(
        RunningSessionType type,
        DateTime recordedAt,
        decimal distanceKm,
        int seriesCount = 0)
    {
        var duration = TimeSpan.FromMinutes(30);
        var session = new RunningSession
        {
            DistanceKm = distanceKm,
            Duration = duration,
            PaceMinPerKm = (double)(duration.TotalMinutes / (double)distanceKm),
            SessionType = type,
            RecordedAt = recordedAt
        };

        for (var i = 1; i <= seriesCount; i++)
        {
            session.Series.Add(new RunningSessionSeries
            {
                SortOrder = i,
                DistanceKm = 1m,
                Duration = TimeSpan.FromMinutes(4)
            });
        }

        return session;
    }
}
