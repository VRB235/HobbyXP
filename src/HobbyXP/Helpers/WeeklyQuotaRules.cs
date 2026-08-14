using HobbyXP.Models.Enums;

namespace HobbyXP.Helpers;

/// <summary>
/// Cuotas semanales de disciplina por hobby.
/// Gym: 5 entrenamientos (no “entretenimientos”).
/// </summary>
public static class WeeklyQuotaRules
{
    public static readonly MilestoneSourceType[] TrackedSources =
    [
        MilestoneSourceType.Running,
        MilestoneSourceType.Gym,
        MilestoneSourceType.Puzzle,
        MilestoneSourceType.Media,
        MilestoneSourceType.VideoGame,
        MilestoneSourceType.Book,
        MilestoneSourceType.Course,
        MilestoneSourceType.Diet
    ];

    public static (int Primary, int Secondary) GetRequired(MilestoneSourceType sourceType) =>
        sourceType switch
        {
            MilestoneSourceType.Running => (4, 0),
            MilestoneSourceType.Gym => (5, 0),
            MilestoneSourceType.Puzzle => (1, 0),
            MilestoneSourceType.Media => (1, 2), // series, películas
            MilestoneSourceType.VideoGame => (1, 0),
            MilestoneSourceType.Book => (1, 0),
            MilestoneSourceType.Course => (1, 0),
            MilestoneSourceType.Diet => (5, 0),
            _ => (0, 0)
        };

    public static string GetPrimaryUnitLabel(MilestoneSourceType sourceType) =>
        sourceType switch
        {
            MilestoneSourceType.Running => "entrenamientos",
            MilestoneSourceType.Gym => "entrenamientos",
            MilestoneSourceType.Puzzle => "rompecabezas",
            MilestoneSourceType.Media => "series",
            MilestoneSourceType.VideoGame => "avances",
            MilestoneSourceType.Book => "lecturas",
            MilestoneSourceType.Course => "sesiones",
            MilestoneSourceType.Diet => "días buenos",
            _ => "unidades"
        };

    public static string GetSecondaryUnitLabel(MilestoneSourceType sourceType) =>
        sourceType switch
        {
            MilestoneSourceType.Media => "películas",
            _ => string.Empty
        };

    public static bool IsMet(int requiredPrimary, int actualPrimary, int requiredSecondary, int actualSecondary) =>
        actualPrimary >= requiredPrimary &&
        (requiredSecondary <= 0 || actualSecondary >= requiredSecondary);

    public static string FormatRequirement(MilestoneSourceType sourceType)
    {
        var (primary, secondary) = GetRequired(sourceType);
        if (secondary <= 0)
            return $"{primary} {GetPrimaryUnitLabel(sourceType)} / semana";

        return $"{primary} {GetPrimaryUnitLabel(sourceType)} y {secondary} {GetSecondaryUnitLabel(sourceType)} / semana";
    }
}
