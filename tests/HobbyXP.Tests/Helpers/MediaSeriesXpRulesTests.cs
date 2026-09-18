using HobbyXP.Helpers;

namespace HobbyXP.Tests.Helpers;

public sealed class MediaSeriesXpRulesTests
{
    [Theory]
    [InlineData(20, 1, 5)]
    [InlineData(20, 20, 100)]
    [InlineData(8, 1, 13)] // Round(12.5) = 13
    [InlineData(8, 8, 100)]
    public void CumulativeXp_DistributesSeriesPool(int totalChapters, int watched, int expected)
    {
        Assert.Equal(expected, MediaSeriesXpRules.GetCumulativeXp(watched, totalChapters, 100));
    }

    [Fact]
    public void AwardForProgress_SumsExactlyToPool()
    {
        const int total = 8;
        var awarded = 0;
        for (var i = 0; i < total; i++)
        {
            awarded += MediaSeriesXpRules.GetAwardForProgress(i, i + 1, total, 100);
        }

        Assert.Equal(100, awarded);
    }

    [Fact]
    public void AwardForProgress_TwentyChapters_IsFiveEach()
    {
        Assert.Equal(5, MediaSeriesXpRules.GetAwardForProgress(0, 1, 20, 100));
        Assert.Equal(5, MediaSeriesXpRules.GetAwardForProgress(4, 5, 20, 100));
        Assert.Equal(10, MediaSeriesXpRules.GetAwardForProgress(0, 2, 20, 100));
    }
}
