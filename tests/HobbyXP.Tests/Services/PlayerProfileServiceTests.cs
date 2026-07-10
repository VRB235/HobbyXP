using HobbyXP.Services;
using HobbyXP.Tests.Helpers;
using Microsoft.EntityFrameworkCore;

namespace HobbyXP.Tests.Services;

public sealed class PlayerProfileServiceTests : IDisposable
{
    private readonly TestDbContextFactory _factory;
    private readonly FakeProfileRefreshMessenger _refreshMessenger;
    private readonly PlayerProfileService _sut;

    public PlayerProfileServiceTests()
    {
        _factory = new TestDbContextFactory();
        _refreshMessenger = new FakeProfileRefreshMessenger();
        _sut = new PlayerProfileService(_factory, _refreshMessenger);
    }

    public void Dispose() => _factory.Dispose();

    [Fact]
    public async Task GetLevelProgressAsync_ReturnsCalculatorSnapshot()
    {
        await using (var db = _factory.CreateDbContext())
        {
            var profile = await db.PlayerProfiles.SingleAsync();
            profile.TotalXp = 750;
            profile.CurrentLevel = 1;
            profile.BaseXpPerLevel = 1000;
            await db.SaveChangesAsync();
        }

        var progress = await _sut.GetLevelProgressAsync();

        Assert.Equal(1, progress.CurrentLevel);
        Assert.Equal(750, progress.TotalXp);
        Assert.Equal(750, progress.XpIntoCurrentLevel);
        Assert.Equal(75d, progress.ProgressPercentage);
    }

    [Fact]
    public async Task UpdateDisplayNameAsync_TrimsAndPersists()
    {
        var profile = await _sut.UpdateDisplayNameAsync("  Guerrero XP  ");

        Assert.Equal("Guerrero XP", profile.DisplayName);

        await using var db = _factory.CreateDbContext();
        Assert.Equal("Guerrero XP", await db.PlayerProfiles.Select(p => p.DisplayName).SingleAsync());
    }

    [Fact]
    public async Task UpdateDisplayNameAsync_WhenEmpty_Throws()
    {
        await Assert.ThrowsAsync<ArgumentException>(() => _sut.UpdateDisplayNameAsync("   "));
    }

    [Fact]
    public async Task UpdateBaseXpPerLevelAsync_RecalculatesLevelAndNotifies()
    {
        await using (var db = _factory.CreateDbContext())
        {
            var seededProfile = await db.PlayerProfiles.SingleAsync();
            seededProfile.TotalXp = 2500;
            seededProfile.CurrentLevel = 3;
            seededProfile.BaseXpPerLevel = 1000;
            await db.SaveChangesAsync();
        }

        var updatedProfile = await _sut.UpdateBaseXpPerLevelAsync(2000);

        Assert.Equal(2000, updatedProfile.BaseXpPerLevel);
        Assert.Equal(2, updatedProfile.CurrentLevel);
        Assert.Equal(1, _refreshMessenger.RefreshRequestCount);
    }

    [Fact]
    public async Task UpdateBaseXpPerLevelAsync_WhenNotPositive_Throws()
    {
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => _sut.UpdateBaseXpPerLevelAsync(0));
    }
}
