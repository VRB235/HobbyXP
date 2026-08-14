using HobbyXP.Models.Enums;
using HobbyXP.Models.Physical;

namespace HobbyXP.Helpers;

/// <summary>
/// Contrato de adherencia: día bueno ≥ 3 comidas en plan; perfecto = 4/4.
/// Las comidas sin marcar no suman.
/// </summary>
public static class DietDayRules
{
    public const int MealsPerDay = 4;
    public const int GoodDayThreshold = 3;

    public static int OnPlanCount(
        DietMealStatus breakfast,
        DietMealStatus lunch,
        DietMealStatus dinner,
        DietMealStatus snack) =>
        (breakfast == DietMealStatus.OnPlan ? 1 : 0)
        + (lunch == DietMealStatus.OnPlan ? 1 : 0)
        + (dinner == DietMealStatus.OnPlan ? 1 : 0)
        + (snack == DietMealStatus.OnPlan ? 1 : 0);

    public static int OnPlanCount(DietDayLog log) =>
        OnPlanCount(log.BreakfastStatus, log.LunchStatus, log.DinnerStatus, log.SnackStatus);

    public static bool IsGoodDay(int onPlanCount) => onPlanCount >= GoodDayThreshold;

    public static bool IsGoodDay(DietDayLog log) => IsGoodDay(OnPlanCount(log));

    public static bool IsPerfectDay(int onPlanCount) => onPlanCount == MealsPerDay;

    public static bool IsPerfectDay(DietDayLog log) => IsPerfectDay(OnPlanCount(log));

    public static bool HasAnyLoggedMeal(
        DietMealStatus breakfast,
        DietMealStatus lunch,
        DietMealStatus dinner,
        DietMealStatus snack) =>
        breakfast != DietMealStatus.Unlogged
        || lunch != DietMealStatus.Unlogged
        || dinner != DietMealStatus.Unlogged
        || snack != DietMealStatus.Unlogged;
}
