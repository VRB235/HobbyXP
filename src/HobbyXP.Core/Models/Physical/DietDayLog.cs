using System.ComponentModel.DataAnnotations.Schema;
using HobbyXP.Helpers;
using HobbyXP.Models.Common;
using HobbyXP.Models.Enums;

namespace HobbyXP.Models.Physical;

/// <summary>
/// Registro de adherencia de un día calendario (una fila por fecha local).
/// </summary>
public class DietDayLog : EntityBase
{
    /// <summary>
    /// Inicio del día local convertido a UTC (<see cref="DateTimeHelper.ToUtcFromLocalDate"/>).
    /// </summary>
    public DateTime DayDate { get; set; }

    public DietMealStatus BreakfastStatus { get; set; } = DietMealStatus.Unlogged;

    public DietMealStatus LunchStatus { get; set; } = DietMealStatus.Unlogged;

    public DietMealStatus DinnerStatus { get; set; } = DietMealStatus.Unlogged;

    public DietMealStatus SnackStatus { get; set; } = DietMealStatus.Unlogged;

    /// <summary>
    /// Denormalizado para consultas de cuota (SQLite no traduce bien el conteo de enums).
    /// </summary>
    public int OnPlanCount { get; set; }

    public string? Notes { get; set; }

    public int XpEarned { get; set; }

    public void RecalculateScore() =>
        OnPlanCount = DietDayRules.OnPlanCount(BreakfastStatus, LunchStatus, DinnerStatus, SnackStatus);

    [NotMapped]
    public string ScoreLabel => DietMealLabels.Score(OnPlanCount);

    [NotMapped]
    public string DayKindLabel => DietMealLabels.DayKind(OnPlanCount);

    [NotMapped]
    public string BreakfastLabel => DietMealLabels.Status(BreakfastStatus);

    [NotMapped]
    public string LunchLabel => DietMealLabels.Status(LunchStatus);

    [NotMapped]
    public string DinnerLabel => DietMealLabels.Status(DinnerStatus);

    [NotMapped]
    public string SnackLabel => DietMealLabels.Status(SnackStatus);
}
