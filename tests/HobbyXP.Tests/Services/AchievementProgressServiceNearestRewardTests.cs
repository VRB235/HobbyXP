using HobbyXP.Models.Achievements;
using HobbyXP.Models.Enums;
using HobbyXP.Services;
using HobbyXP.Tests.Helpers;
using Microsoft.EntityFrameworkCore;

namespace HobbyXP.Tests.Services;

public sealed class AchievementProgressServiceNearestRewardTests : IDisposable
{
    private readonly TestDbContextFactory _factory;
    private readonly AchievementProgressService _sut;

    public AchievementProgressServiceNearestRewardTests()
    {
        _factory = new TestDbContextFactory();
        var medalService = new MedalService(_factory);
        _sut = new AchievementProgressService(_factory, medalService);
    }

    public void Dispose() => _factory.Dispose();

    [Fact]
    public async Task GetNearestRewardAsync_ReturnsClosestByRemainingXp()
    {
        await using (var db = _factory.CreateDbContext())
        {
            var profile = await db.PlayerProfiles.SingleAsync();
            profile.CurrentLevel = 1;

            db.HobbyProgresses.Add(new Models.Core.HobbyProgress
            {
                PlayerProfileId = profile.Id,
                SourceType = MilestoneSourceType.Gym,
                SpendableXp = 200
            });

            db.Rewards.AddRange(
                new Reward
                {
                    Name = "Lejos",
                    CostInPoints = 1000,
                    SourceType = MilestoneSourceType.Gym,
                    Status = RewardStatus.Available
                },
                new Reward
                {
                    Name = "Cercano",
                    CostInPoints = 300,
                    SourceType = MilestoneSourceType.Gym,
                    Status = RewardStatus.Available,
                    Price = 40m,
                    PurchaseUrl = "https://ejemplo.test/cercano"
                },
                new Reward
                {
                    Name = "Otro módulo",
                    CostInPoints = 100,
                    SourceType = MilestoneSourceType.Running,
                    Status = RewardStatus.Available
                });

            await db.SaveChangesAsync();
        }

        var nearest = await _sut.GetNearestRewardAsync(MilestoneSourceType.Gym);

        Assert.NotNull(nearest);
        Assert.Equal("Cercano", nearest.Name);
        Assert.Equal(100, nearest.RemainingXp);
        Assert.Equal(40m, nearest.Price);
        Assert.Contains("faltan 100", nearest.BannerText, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GetNearestRewardAsync_WhenAffordable_PrefersCheapestAffordable()
    {
        await using (var db = _factory.CreateDbContext())
        {
            var profile = await db.PlayerProfiles.SingleAsync();
            profile.CurrentLevel = 1;

            db.HobbyProgresses.Add(new Models.Core.HobbyProgress
            {
                PlayerProfileId = profile.Id,
                SourceType = MilestoneSourceType.Running,
                SpendableXp = 500
            });

            db.Rewards.AddRange(
                new Reward
                {
                    Name = "Barato",
                    CostInPoints = 100,
                    SourceType = MilestoneSourceType.Running,
                    Status = RewardStatus.Available
                },
                new Reward
                {
                    Name = "Caro",
                    CostInPoints = 400,
                    SourceType = MilestoneSourceType.Running,
                    Status = RewardStatus.Available
                });

            await db.SaveChangesAsync();
        }

        var nearest = await _sut.GetNearestRewardAsync(MilestoneSourceType.Running);

        Assert.NotNull(nearest);
        Assert.Equal("Barato", nearest.Name);
        Assert.True(nearest.CanAfford);
        Assert.Equal(0, nearest.RemainingXp);
    }
}
