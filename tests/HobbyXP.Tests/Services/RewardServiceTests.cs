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
        await Assert.ThrowsAsync<ArgumentException>(() => _sut.CreateAsync("  ", 100));
    }

    [Fact]
    public async Task CreateAsync_WhenCostNotPositive_Throws()
    {
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => _sut.CreateAsync("Premio", 0));
    }

    [Fact]
    public async Task CanRedeemAsync_WhenBalanceInsufficient_ReturnsFalse()
    {
        var reward = await _sut.CreateAsync("Café", 500);

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
            await db.SaveChangesAsync();
        }

        var reward = await _sut.CreateAsync("Día libre", 300, "Descanso merecido");
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
        Assert.Equal(700, profileAfter.SpendableXp);
        Assert.Equal(-300, await verifyDb.XpTransactions.Select(t => t.Amount).SingleAsync());
        Assert.Single(await verifyDb.Milestones.Where(m => m.SourceType == MilestoneSourceType.Reward).ToListAsync());
    }

    [Fact]
    public async Task RedeemAsync_WhenInsufficientXp_Throws()
    {
        var reward = await _sut.CreateAsync("Viaje", 2000);

        await Assert.ThrowsAsync<InvalidOperationException>(() => _sut.RedeemAsync(reward.Id));
    }
}
