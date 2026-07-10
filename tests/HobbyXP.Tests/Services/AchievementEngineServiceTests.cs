using HobbyXP.Models.Achievements;
using HobbyXP.Models.Enums;
using HobbyXP.Models.PersonalGrowth;
using HobbyXP.Services;
using HobbyXP.Tests.Helpers;
using Microsoft.EntityFrameworkCore;

namespace HobbyXP.Tests.Services;

public sealed class AchievementEngineServiceTests : IDisposable
{
    private readonly TestDbContextFactory _factory;
    private readonly AchievementEngineService _sut;

    public AchievementEngineServiceTests()
    {
        _factory = new TestDbContextFactory();
        _sut = new AchievementEngineService(_factory);
    }

    public void Dispose() => _factory.Dispose();

    [Fact]
    public async Task GetActiveRuleAsync_ReturnsSeededRule()
    {
        var rule = await _sut.GetActiveRuleAsync(AchievementActionType.RunningKilometer);

        Assert.NotNull(rule);
        Assert.Equal(10m, rule!.PointsPerUnit);
        Assert.True(rule.IsActive);
    }

    [Fact]
    public async Task UpdateRuleAsync_PersistsChanges()
    {
        var existing = await _sut.GetActiveRuleAsync(AchievementActionType.PuzzleCompleted);
        Assert.NotNull(existing);

        existing!.DisplayName = "Puzzle épico";
        existing.PointsPerUnit = 75m;
        existing.FlatBonusPoints = 10;

        var updated = await _sut.UpdateRuleAsync(existing);

        Assert.Equal("Puzzle épico", updated.DisplayName);
        Assert.Equal(75m, updated.PointsPerUnit);
        Assert.Equal(10, updated.FlatBonusPoints);

        await using var db = _factory.CreateDbContext();
        var stored = await db.AchievementRules.SingleAsync(r => r.Id == existing.Id);
        Assert.Equal(75m, stored.PointsPerUnit);
    }

    [Fact]
    public async Task TryAwardMedalAsync_WhenThresholdMet_GrantsMedalOnce()
    {
        await using (var db = _factory.CreateDbContext())
        {
            db.Books.Add(new Book
            {
                Title = "Dune",
                Author = "Herbert",
                TotalPages = 400,
                PagesRead = 400,
                Status = BookStatus.Completed,
                CompletedAt = DateTime.UtcNow
            });
            await db.SaveChangesAsync();
        }

        var first = await _sut.TryAwardMedalAsync(
            MedalCode.BookCompleted,
            MilestoneSourceType.Book,
            sourceEntityType: nameof(Book),
            sourceEntityId: 1);

        var second = await _sut.TryAwardMedalAsync(
            MedalCode.BookCompleted,
            MilestoneSourceType.Book,
            sourceEntityType: nameof(Book),
            sourceEntityId: 2);

        Assert.NotNull(first);
        Assert.Equal(MedalCode.BookCompleted, first!.MedalUnlocked);
        Assert.Null(second);
        Assert.True(await _sut.IsMedalEarnedAsync(MedalCode.BookCompleted));

        await using var verifyDb = _factory.CreateDbContext();
        Assert.Single(await verifyDb.EarnedMedals.ToListAsync());
    }

    [Fact]
    public async Task TryAwardMilestonesForTrackAsync_AwardsMultipleThresholdsInOnePass()
    {
        await using (var db = _factory.CreateDbContext())
        {
            for (var i = 0; i < 5; i++)
            {
                db.Puzzles.Add(new Models.Entertainment.Puzzle
                {
                    Name = $"Puzzle {i + 1}",
                    Category = PuzzleCategory.TwoD,
                    PieceCount = 1000,
                    CompletedAt = DateTime.UtcNow
                });
            }

            await db.SaveChangesAsync();
        }

        var events = await _sut.TryAwardMilestonesForTrackAsync(
            MedalMilestoneTrack.PuzzlesCompleted,
            MilestoneSourceType.Puzzle);

        Assert.Contains(events, e => e.MedalUnlocked == MedalCode.PuzzleMaster);
        Assert.True(events.Count >= 2);
    }
}
