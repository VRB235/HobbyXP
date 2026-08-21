using HobbyXP.Models.Enums;
using HobbyXP.Services;
using HobbyXP.Tests.Helpers;
using Microsoft.EntityFrameworkCore;

namespace HobbyXP.Tests.Services;

public sealed class RewardServiceTests : IDisposable
{
    private readonly TestDbContextFactory _factory;
    private readonly FakeLevelUpMessenger _levelUpMessenger;
    private readonly RewardService _sut;

    public RewardServiceTests()
    {
        _factory = new TestDbContextFactory();
        _levelUpMessenger = new FakeLevelUpMessenger();

        var xpService = new XpService(_factory, _levelUpMessenger);
        var profileService = new PlayerProfileService(_factory, new FakeProfileRefreshMessenger());
        _sut = new RewardService(_factory, xpService, profileService);
    }

    public void Dispose() => _factory.Dispose();

    [Fact]
    public async Task CreateAsync_WhenNameEmpty_Throws()
    {
        await Assert.ThrowsAsync<ArgumentException>(() => _sut.CreateAsync("  ", 100, MilestoneSourceType.Gym));
    }

    [Fact]
    public async Task CreateAsync_WhenCostNotPositive_Throws()
    {
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => _sut.CreateAsync("Premio", 0, MilestoneSourceType.Gym));
    }

    [Fact]
    public async Task CreateAsync_WhenSourceTypeIsNotAHobby_Throws()
    {
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => _sut.CreateAsync("Premio", 100, MilestoneSourceType.Reward));
    }

    [Fact]
    public async Task CreateAsync_PersistsPurchaseDetails()
    {
        var reward = await _sut.CreateAsync(
            "Auriculares",
            300,
            MilestoneSourceType.Gym,
            "Sony WH",
            price: 199.99m,
            purchaseUrl: "https://tienda.example/auriculares");

        Assert.Equal(199.99m, reward.Price);
        Assert.Equal("https://tienda.example/auriculares", reward.PurchaseUrl);

        await using var db = _factory.CreateDbContext();
        var stored = await db.Rewards.SingleAsync();
        Assert.Equal(199.99m, stored.Price);
        Assert.Equal("https://tienda.example/auriculares", stored.PurchaseUrl);
    }

    [Fact]
    public async Task UpdateAsync_UpdatesAllWritableFields()
    {
        var reward = await _sut.CreateAsync("Batido", 200, MilestoneSourceType.Gym);

        var updated = await _sut.UpdateAsync(
            reward.Id,
            "Batido proteico",
            250,
            MilestoneSourceType.Diet,
            "Post entreno",
            12.5m,
            "https://tienda.example/batido");

        Assert.Equal("Batido proteico", updated.Name);
        Assert.Equal(250, updated.CostInPoints);
        Assert.Equal(MilestoneSourceType.Diet, updated.SourceType);
        Assert.Equal("Post entreno", updated.Description);
        Assert.Equal(12.5m, updated.Price);
        Assert.Equal("https://tienda.example/batido", updated.PurchaseUrl);
    }

    [Fact]
    public async Task DeleteAsync_RemovesAvailableReward()
    {
        var reward = await _sut.CreateAsync("Café", 100, MilestoneSourceType.Gym);

        await _sut.DeleteAsync(reward.Id);

        await using var db = _factory.CreateDbContext();
        Assert.Empty(await db.Rewards.ToListAsync());
    }

    [Fact]
    public async Task DeleteAsync_WhenRedeemed_Throws()
    {
        await using (var db = _factory.CreateDbContext())
        {
            var profile = await db.PlayerProfiles.SingleAsync();
            profile.SpendableXp = 500;
            profile.CurrentLevel = 1;

            db.HobbyProgresses.Add(new Models.Core.HobbyProgress
            {
                PlayerProfileId = profile.Id,
                SourceType = MilestoneSourceType.Gym,
                SpendableXp = 500
            });

            await db.SaveChangesAsync();
        }

        var reward = await _sut.CreateAsync("Café", 100, MilestoneSourceType.Gym);
        await _sut.RedeemAsync(reward.Id);

        await Assert.ThrowsAsync<InvalidOperationException>(() => _sut.DeleteAsync(reward.Id));
    }

    [Fact]
    public async Task CreateAsync_PersistsHobbyModule()
    {
        var reward = await _sut.CreateAsync("Zapatillas", 400, MilestoneSourceType.Running);

        Assert.Equal(MilestoneSourceType.Running, reward.SourceType);

        await using var db = _factory.CreateDbContext();
        Assert.Equal(MilestoneSourceType.Running, (await db.Rewards.SingleAsync()).SourceType);
    }

    [Fact]
    public async Task UpdateSourceTypeAsync_MovesRewardToAnotherHobby()
    {
        var reward = await _sut.CreateAsync("Batido", 200, MilestoneSourceType.Gym);

        await _sut.UpdateSourceTypeAsync(reward.Id, MilestoneSourceType.Diet);

        await using var db = _factory.CreateDbContext();
        Assert.Equal(MilestoneSourceType.Diet, (await db.Rewards.SingleAsync()).SourceType);
    }

    [Fact]
    public async Task CanRedeemAsync_WhenBalanceInsufficient_ReturnsFalse()
    {
        var reward = await _sut.CreateAsync("Café", 500, MilestoneSourceType.Gym);

        var canRedeem = await _sut.CanRedeemAsync(reward.Id);

        Assert.False(canRedeem);
    }

    [Fact]
    public async Task RedeemAsync_DeductsSpendableXpAndMarksRewardRedeemed()
    {
        await using (var db = _factory.CreateDbContext())
        {
            var profile = await db.PlayerProfiles.SingleAsync();
            profile.TotalXp = 1000;
            profile.CurrentLevel = 2;
            profile.SpendableXp = 1000;

            db.HobbyProgresses.Add(new Models.Core.HobbyProgress
            {
                PlayerProfileId = profile.Id,
                SourceType = MilestoneSourceType.Gym,
                CurrentLevel = 1,
                TotalXp = 0,
                SpendableXp = 1000
            });

            await db.SaveChangesAsync();
        }

        var reward = await _sut.CreateAsync("Día libre", 300, MilestoneSourceType.Gym, "Descanso merecido");
        Assert.True(await _sut.CanRedeemAsync(reward.Id));

        var result = await _sut.RedeemAsync(reward.Id);

        Assert.NotNull(result.Value);
        Assert.Equal(RewardStatus.Redeemed, result.Value.Status);
        Assert.NotNull(result.Value.RedeemedAt);
        Assert.Single(result.Events);

        await using var verifyDb = _factory.CreateDbContext();
        var profileAfter = await verifyDb.PlayerProfiles.SingleAsync();
        Assert.Equal(1000, profileAfter.TotalXp);
        Assert.Equal(2, profileAfter.CurrentLevel);
        Assert.Equal(400, profileAfter.SpendableXp);

        var gymPool = await verifyDb.HobbyProgresses.SingleAsync(h => h.SourceType == MilestoneSourceType.Gym);
        Assert.Equal(400, gymPool.SpendableXp);

        Assert.Equal(600, result.Value.RedeemedCostInPoints);
        Assert.Equal(-600, await verifyDb.XpTransactions.Select(t => t.Amount).SingleAsync());
        Assert.Single(await verifyDb.Milestones.Where(m => m.SourceType == MilestoneSourceType.Reward).ToListAsync());
    }

    [Fact]
    public async Task CanRedeemAsync_WhenModuleBalanceInsufficient_ReturnsFalse()
    {
        await using (var db = _factory.CreateDbContext())
        {
            var profile = await db.PlayerProfiles.SingleAsync();
            profile.SpendableXp = 5000;

            db.HobbyProgresses.Add(new Models.Core.HobbyProgress
            {
                PlayerProfileId = profile.Id,
                SourceType = MilestoneSourceType.Running,
                SpendableXp = 5000
            });

            await db.SaveChangesAsync();
        }

        var reward = await _sut.CreateAsync("Café", 500, MilestoneSourceType.Gym);

        Assert.False(await _sut.CanRedeemAsync(reward.Id));
    }

    [Fact]
    public async Task EquipAsync_SetsEquippedRewardOnProfile()
    {
        await using (var db = _factory.CreateDbContext())
        {
            var profile = await db.PlayerProfiles.SingleAsync();
            profile.SpendableXp = 500;
            profile.CurrentLevel = 1;

            db.HobbyProgresses.Add(new Models.Core.HobbyProgress
            {
                PlayerProfileId = profile.Id,
                SourceType = MilestoneSourceType.Gym,
                SpendableXp = 500
            });

            await db.SaveChangesAsync();
        }

        var reward = await _sut.CreateAsync("Café", 100, MilestoneSourceType.Gym);
        await _sut.RedeemAsync(reward.Id);
        await _sut.EquipAsync(reward.Id);

        await using var verify = _factory.CreateDbContext();
        var profileAfter = await verify.PlayerProfiles.SingleAsync();
        Assert.Equal(reward.Id, profileAfter.EquippedRewardId);
    }

    [Fact]
    public async Task EquipAsync_WhenNotRedeemed_Throws()
    {
        var reward = await _sut.CreateAsync("Viaje", 200, MilestoneSourceType.Running);
        await Assert.ThrowsAsync<InvalidOperationException>(() => _sut.EquipAsync(reward.Id));
    }

    [Fact]
    public async Task RedeemAsync_WhenInsufficientXp_Throws()
    {
        var reward = await _sut.CreateAsync("Viaje", 2000, MilestoneSourceType.OfficialRace);

        await Assert.ThrowsAsync<InvalidOperationException>(() => _sut.RedeemAsync(reward.Id));
    }
}
