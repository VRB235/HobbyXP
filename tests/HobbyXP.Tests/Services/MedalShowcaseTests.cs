using HobbyXP.Models.Achievements;
using HobbyXP.Models.Enums;
using HobbyXP.Services;
using HobbyXP.Services.Results;
using HobbyXP.Tests.Helpers;
using HobbyXP.ViewModels.Achievements;
using Microsoft.EntityFrameworkCore;

namespace HobbyXP.Tests.Services;

public sealed class MedalShowcaseTests : IDisposable
{
    private readonly TestDbContextFactory _factory;
    private readonly MedalService _sut;

    public MedalShowcaseTests()
    {
        _factory = new TestDbContextFactory();
        _sut = new MedalService(_factory);
    }

    public void Dispose() => _factory.Dispose();

    [Fact]
    public async Task GetShowcaseSections_GroupsByHobby_AndPutsEarnedFirst()
    {
        await using (var db = _factory.CreateDbContext())
        {
            var gymLater = await db.MedalDefinitions.SingleAsync(m => m.Code == MedalCode.GymWorkouts10);
            db.EarnedMedals.Add(new EarnedMedal
            {
                MedalDefinitionId = gymLater.Id,
                EarnedAt = DateTime.UtcNow
            });
            await db.SaveChangesAsync();
        }

        var sections = await _sut.GetShowcaseSectionsAsync();

        Assert.Equal(
        [
            MilestoneSourceType.Running,
            MilestoneSourceType.Gym,
            MilestoneSourceType.OfficialRace,
            MilestoneSourceType.Puzzle,
            MilestoneSourceType.Media,
            MilestoneSourceType.VideoGame,
            MilestoneSourceType.Book,
            MilestoneSourceType.Course,
            MilestoneSourceType.Diet
        ], sections.Select(s => s.SourceType).ToArray());

        var gym = sections.Single(s => s.SourceType == MilestoneSourceType.Gym);
        Assert.Equal(MedalCode.GymWorkouts10, gym.Medals[0].Code);
        Assert.True(gym.Medals[0].IsEarned);
        Assert.Contains(gym.Medals.Skip(1), m => m.Code == MedalCode.ProgressiveOverload && !m.IsEarned);

        var running = sections.Single(s => s.SourceType == MilestoneSourceType.Running);
        Assert.False(running.Medals[0].IsEarned);
    }

    [Fact]
    public void SectionViewModel_ExpandsOnlyWhenAnyMedalIsEarned()
    {
        var earned = new MedalShowcaseSection(
            MilestoneSourceType.Gym,
            "Gimnasio",
            [Item(MedalCode.GymWorkouts10, isEarned: true)]);
        var locked = new MedalShowcaseSection(
            MilestoneSourceType.Running,
            "Running",
            [Item(MedalCode.RunningKm100, isEarned: false)]);

        Assert.True(new MedalShowcaseSectionViewModel(earned).IsExpanded);
        Assert.False(new MedalShowcaseSectionViewModel(locked).IsExpanded);
        Assert.Equal("Gimnasio  ·  1/1 desbloqueadas", new MedalShowcaseSectionViewModel(earned).ToString());
    }

    private static MedalShowcaseItem Item(MedalCode code, bool isEarned) =>
        new(
            MedalDefinitionId: 1,
            Code: code,
            Name: code.ToString(),
            Description: string.Empty,
            UnlockHint: string.Empty,
            IconPath: null,
            IsEarned: isEarned,
            EarnedAt: isEarned ? DateTime.UtcNow : null,
            SourceType: MilestoneSourceType.Gym);
}
