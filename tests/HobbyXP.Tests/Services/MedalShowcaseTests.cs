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
        var engine = new AchievementEngineService(_factory);
        _sut = new MedalService(_factory, engine);
    }

    public void Dispose() => _factory.Dispose();

    [Fact]
    public async Task GetShowcaseSections_GroupsByHobby_AndPutsEarnedFirst()
    {
        var gymEarly = await _sut.CreateAsync(
            MilestoneSourceType.Gym,
            50,
            "Hierro Temprano",
            "50 XP de gym.",
            "Entrena hasta 50 XP.");
        await _sut.CreateAsync(
            MilestoneSourceType.Gym,
            200,
            "Forja Personal",
            "200 XP de gym.",
            "Sigue entrenando.");
        await _sut.CreateAsync(
            MilestoneSourceType.Running,
            100,
            "Ritmo de Reloj",
            "100 XP running.",
            "Corre hasta 100 XP.");

        await using (var db = _factory.CreateDbContext())
            await TestHobbyProgress.SetTotalXpAsync(db, MilestoneSourceType.Gym, 80);

        await new AchievementEngineService(_factory).TryAwardHobbyXpMedalsAsync(MilestoneSourceType.Gym);

        var sections = await _sut.GetShowcaseSectionsAsync();

        Assert.Equal(
        [
            MilestoneSourceType.Running,
            MilestoneSourceType.Gym
        ], sections.Select(s => s.SourceType).ToArray());

        var gym = sections.Single(s => s.SourceType == MilestoneSourceType.Gym);
        Assert.Equal(gymEarly.Code, gym.Medals[0].Code);
        Assert.True(gym.Medals[0].IsEarned);
        Assert.Contains(gym.Medals.Skip(1), m => !m.IsEarned && m.XpThreshold == 200);

        var running = sections.Single(s => s.SourceType == MilestoneSourceType.Running);
        Assert.False(running.Medals[0].IsEarned);
    }

    [Fact]
    public void SectionViewModel_ExpandsOnlyWhenAnyMedalIsEarned()
    {
        var earned = new MedalShowcaseSection(
            MilestoneSourceType.Gym,
            "Gimnasio",
            [Item("m_gym", isEarned: true)]);
        var locked = new MedalShowcaseSection(
            MilestoneSourceType.Running,
            "Running",
            [Item("m_run", isEarned: false)]);

        Assert.True(new MedalShowcaseSectionViewModel(earned).IsExpanded);
        Assert.False(new MedalShowcaseSectionViewModel(locked).IsExpanded);
        Assert.Equal("Gimnasio  ·  1/1 desbloqueadas", new MedalShowcaseSectionViewModel(earned).ToString());
    }

    private static MedalShowcaseItem Item(string code, bool isEarned) =>
        new(
            MedalDefinitionId: 1,
            Code: code,
            Name: code,
            Description: string.Empty,
            UnlockHint: string.Empty,
            IconPath: null,
            IsEarned: isEarned,
            EarnedAt: isEarned ? DateTime.UtcNow : null,
            SourceType: MilestoneSourceType.Gym,
            XpThreshold: 100);
}
