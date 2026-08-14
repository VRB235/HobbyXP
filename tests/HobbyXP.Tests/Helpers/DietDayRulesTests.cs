using HobbyXP.Helpers;
using HobbyXP.Models.Enums;

namespace HobbyXP.Tests.Helpers;

public sealed class DietDayRulesTests
{
    [Theory]
    [InlineData(DietMealStatus.OnPlan, DietMealStatus.OnPlan, DietMealStatus.OnPlan, DietMealStatus.Unlogged, 3, true, false)]
    [InlineData(DietMealStatus.OnPlan, DietMealStatus.OnPlan, DietMealStatus.OffPlan, DietMealStatus.Unlogged, 2, false, false)]
    [InlineData(DietMealStatus.OnPlan, DietMealStatus.OnPlan, DietMealStatus.OnPlan, DietMealStatus.OnPlan, 4, true, true)]
    [InlineData(DietMealStatus.Unlogged, DietMealStatus.Unlogged, DietMealStatus.Unlogged, DietMealStatus.Unlogged, 0, false, false)]
    [InlineData(DietMealStatus.OffPlan, DietMealStatus.OffPlan, DietMealStatus.OffPlan, DietMealStatus.OffPlan, 0, false, false)]
    [InlineData(DietMealStatus.OnPlan, DietMealStatus.OnPlan, DietMealStatus.OnPlan, DietMealStatus.OffPlan, 3, true, false)]
    public void OnPlanCount_AndDayFlags(
        DietMealStatus breakfast,
        DietMealStatus lunch,
        DietMealStatus dinner,
        DietMealStatus snack,
        int expectedCount,
        bool expectedGood,
        bool expectedPerfect)
    {
        var count = DietDayRules.OnPlanCount(breakfast, lunch, dinner, snack);

        Assert.Equal(expectedCount, count);
        Assert.Equal(expectedGood, DietDayRules.IsGoodDay(count));
        Assert.Equal(expectedPerfect, DietDayRules.IsPerfectDay(count));
    }

    [Fact]
    public void Unlogged_DoesNotCountAsOnPlan()
    {
        var count = DietDayRules.OnPlanCount(
            DietMealStatus.OnPlan,
            DietMealStatus.OnPlan,
            DietMealStatus.OnPlan,
            DietMealStatus.Unlogged);

        Assert.Equal(3, count);
        Assert.True(DietDayRules.IsGoodDay(count));
    }

    [Fact]
    public void HasAnyLoggedMeal_RequiresAtLeastOneMark()
    {
        Assert.False(DietDayRules.HasAnyLoggedMeal(
            DietMealStatus.Unlogged,
            DietMealStatus.Unlogged,
            DietMealStatus.Unlogged,
            DietMealStatus.Unlogged));

        Assert.True(DietDayRules.HasAnyLoggedMeal(
            DietMealStatus.Unlogged,
            DietMealStatus.OffPlan,
            DietMealStatus.Unlogged,
            DietMealStatus.Unlogged));
    }
}
