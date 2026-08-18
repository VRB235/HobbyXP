using HobbyXP.Helpers;
using HobbyXP.Models.Enums;

namespace HobbyXP.Tests.Helpers;

public sealed class RewardShopCatalogTests
{
    [Fact]
    public void Group_OrdersTrackedHobbies_AndPutsUnassignedInGeneral()
    {
        var items = new (string Name, MilestoneSourceType? Source)[]
        {
            ("Batido", MilestoneSourceType.Gym),
            ("Zapatillas", MilestoneSourceType.Running),
            ("Cinta", MilestoneSourceType.Gym),
            ("Café", null),
            ("Sistema", MilestoneSourceType.System)
        };

        var sections = RewardShopCatalog.Group(items, item => item.Source);

        Assert.Equal(["Running", "Gimnasio", RewardShopCatalog.GeneralDisplayName],
            sections.Select(s => s.DisplayName).ToArray());
        Assert.Equal(["Zapatillas"], sections[0].Items.Select(i => i.Name).ToArray());
        Assert.Equal(["Batido", "Cinta"], sections[1].Items.Select(i => i.Name).ToArray());
        Assert.Equal(["Café", "Sistema"], sections[2].Items.Select(i => i.Name).ToArray());
    }

    [Fact]
    public void GetModuleDisplayName_UsesHobbyLabelOrGeneral()
    {
        Assert.Equal("Gimnasio", RewardShopCatalog.GetModuleDisplayName(MilestoneSourceType.Gym));
        Assert.Equal(RewardShopCatalog.GeneralDisplayName, RewardShopCatalog.GetModuleDisplayName(null));
        Assert.Equal(RewardShopCatalog.GeneralDisplayName, RewardShopCatalog.GetModuleDisplayName(MilestoneSourceType.Reward));
    }
}
