using HobbyXP.Models.Enums;

namespace HobbyXP.Helpers;

public static class DietMealLabels
{
    public static string Slot(DietMealSlot slot) => slot switch
    {
        DietMealSlot.Breakfast => "Desayuno",
        DietMealSlot.Lunch => "Almuerzo",
        DietMealSlot.Dinner => "Cena",
        DietMealSlot.Snack => "Snack",
        _ => slot.ToString()
    };

    public static string Status(DietMealStatus status) => status switch
    {
        DietMealStatus.OnPlan => "En plan",
        DietMealStatus.OffPlan => "Fuera de plan",
        DietMealStatus.Unlogged => "—",
        _ => status.ToString()
    };

    public static string DayKind(int onPlanCount)
    {
        if (DietDayRules.IsPerfectDay(onPlanCount))
            return "Día perfecto";
        if (DietDayRules.IsGoodDay(onPlanCount))
            return "Día bueno";
        return "No cuenta para la cuota";
    }

    public static string Score(int onPlanCount) =>
        $"{onPlanCount}/{DietDayRules.MealsPerDay}";
}
