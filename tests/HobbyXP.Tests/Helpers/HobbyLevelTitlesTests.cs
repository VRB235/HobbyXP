using HobbyXP.Helpers;
using HobbyXP.Models.Enums;

namespace HobbyXP.Tests.Helpers;

public sealed class HobbyLevelTitlesTests
{
    [Theory]
    [InlineData(MilestoneSourceType.Running, 1, "Aprendiz del asfalto")]
    [InlineData(MilestoneSourceType.Gym, 5, "Alquimista del PR")]
    [InlineData(MilestoneSourceType.Book, 12, "Leyenda del colofón")]
    public void GetTitle_ReturnsExpectedNamedRank(MilestoneSourceType source, int level, string expected) =>
        Assert.Equal(expected, HobbyLevelTitles.GetTitle(source, level));

    [Fact]
    public void GetTitle_BeyondCatalog_AppendsAscensoRoman()
    {
        var title = HobbyLevelTitles.GetTitle(MilestoneSourceType.Puzzle, 14);

        Assert.StartsWith("Leyenda del click final · Ascenso", title);
        Assert.Contains("II", title);
    }

    [Fact]
    public void FormatLevelLabel_IncludesNumberAndTitle()
    {
        var label = HobbyLevelTitles.FormatLevelLabel(MilestoneSourceType.VideoGame, 1);

        Assert.Equal("Nv. 1 · Noob con honor", label);
    }
}
