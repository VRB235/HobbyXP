using HobbyXP.Helpers;

namespace HobbyXP.Tests.Helpers;

public sealed class GlobalLevelTitlesTests
{
    [Theory]
    [InlineData(1, "Novato de la bitácora")]
    [InlineData(6, "Alquimista del progreso")]
    [InlineData(12, "Leyenda del HobbyXP")]
    public void GetTitle_ReturnsExpectedNamedRank(int level, string expected) =>
        Assert.Equal(expected, GlobalLevelTitles.GetTitle(level));

    [Fact]
    public void GetTitle_BeyondCatalog_AppendsAscensoRoman()
    {
        var title = GlobalLevelTitles.GetTitle(14);

        Assert.StartsWith("Leyenda del HobbyXP · Ascenso", title);
        Assert.Contains("II", title);
    }

    [Fact]
    public void FormatLevelLabel_IncludesNumberAndTitle()
    {
        var label = GlobalLevelTitles.FormatLevelLabel(1);

        Assert.Equal("Nv. 1 · Novato de la bitácora", label);
    }
}
