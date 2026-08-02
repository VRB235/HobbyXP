using HobbyXP.Models.Core;
using HobbyXP.Services.Internal;

namespace HobbyXP.Tests.Services.Internal;

public sealed class XpLevelCalculatorTests
{
    [Theory]
    [InlineData(1, 1000, 0)]
    [InlineData(2, 1000, 1000)]
    [InlineData(3, 1000, 3000)]
    [InlineData(4, 1000, 7000)]
    [InlineData(5, 500, 7500)]
    public void GetXpThresholdForLevel_ReturnsExpected(int level, int baseXp, long expected) =>
        Assert.Equal(expected, XpLevelCalculator.GetXpThresholdForLevel(level, baseXp));

    [Theory]
    [InlineData(1, 1000, 1000)]
    [InlineData(2, 1000, 2000)]
    [InlineData(3, 1000, 4000)]
    [InlineData(4, 500, 4000)]
    public void GetXpRequiredForLevel_DoublesEachLevel(int level, int baseXp, int expected) =>
        Assert.Equal(expected, XpLevelCalculator.GetXpRequiredForLevel(level, baseXp));

    [Fact]
    public void BuildProgress_AtStartOfLevel1_ReturnsZeroPercent()
    {
        var profile = new PlayerProfile { CurrentLevel = 1, TotalXp = 0, BaseXpPerLevel = 1000 };

        var progress = XpLevelCalculator.BuildProgress(profile);

        Assert.Equal(1, progress.CurrentLevel);
        Assert.Equal(0, progress.XpIntoCurrentLevel);
        Assert.Equal(1000, progress.XpRequiredForNextLevel);
        Assert.Equal(0d, progress.ProgressPercentage);
    }

    [Fact]
    public void BuildProgress_HalfwayThroughLevel_ReturnsFiftyPercent()
    {
        var profile = new PlayerProfile { CurrentLevel = 1, TotalXp = 500, BaseXpPerLevel = 1000 };

        var progress = XpLevelCalculator.BuildProgress(profile);

        Assert.Equal(500, progress.XpIntoCurrentLevel);
        Assert.Equal(50d, progress.ProgressPercentage);
    }

    [Fact]
    public void BuildProgress_AtLevel2_UsesDoubledRequirement()
    {
        var profile = new PlayerProfile { CurrentLevel = 2, TotalXp = 2000, BaseXpPerLevel = 1000 };

        var progress = XpLevelCalculator.BuildProgress(profile);

        Assert.Equal(2, progress.CurrentLevel);
        Assert.Equal(1000, progress.XpIntoCurrentLevel);
        Assert.Equal(2000, progress.XpRequiredForNextLevel);
        Assert.Equal(50d, progress.ProgressPercentage);
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(999, 1)]
    [InlineData(1000, 2)]
    [InlineData(2999, 2)]
    [InlineData(3000, 3)]
    [InlineData(7000, 4)]
    [InlineData(5000, 3)]
    public void RecalculateLevel_FromTotalXp_AssignsExpectedLevel(int totalXp, int expectedLevel)
    {
        var profile = new PlayerProfile { CurrentLevel = 1, TotalXp = totalXp, BaseXpPerLevel = 1000 };

        XpLevelCalculator.RecalculateLevel(profile);

        Assert.Equal(expectedLevel, profile.CurrentLevel);
    }

    [Fact]
    public void RecalculateLevel_WhenXpDrops_ReducesLevel()
    {
        var profile = new PlayerProfile { CurrentLevel = 5, TotalXp = 250, BaseXpPerLevel = 1000 };

        XpLevelCalculator.RecalculateLevel(profile);

        Assert.Equal(1, profile.CurrentLevel);
    }

    [Fact]
    public void RecalculateLevel_WithStaleHighLevel_AlignsToTotalXp()
    {
        var profile = new PlayerProfile { CurrentLevel = 6, TotalXp = 5000, BaseXpPerLevel = 1000 };

        XpLevelCalculator.RecalculateLevel(profile);

        Assert.Equal(3, profile.CurrentLevel);
        Assert.Equal(5000, profile.TotalXp);
    }
}
