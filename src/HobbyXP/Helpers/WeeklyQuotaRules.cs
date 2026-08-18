using HobbyXP.Models.Enums;

namespace HobbyXP.Helpers;

/// <summary>
/// Cuotas semanales de disciplina por hobby.
/// Gym: 5 entrenamientos (no “entretenimientos”).
/// Libro: 20 % de las páginas del libro actual (dinámico).
/// </summary>
public static class WeeklyQuotaRules
{
    public const int BookPageQuotaPercent = 20;
    public const int CourseSessionsRequired = 5;
    public const int SeriesCompletedRequired = 1;
    public const int MoviesRequired = 2;

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
            MilestoneSourceType.Media => (SeriesCompletedRequired, MoviesRequired),
            MilestoneSourceType.VideoGame => (1, 0),
            MilestoneSourceType.Book => (0, 0), // dinámico: GetBookRequiredPages
            MilestoneSourceType.Course => (CourseSessionsRequired, 0),
            MilestoneSourceType.Diet => (5, 0),
            _ => (0, 0)
        };

    /// <summary>
    /// Páginas mínimas de la semana: techo del 20 % del libro actual (mínimo 1 si hay páginas).
    /// </summary>
    public static int GetBookRequiredPages(int totalPages)
    {
        if (totalPages <= 0)
            return 0;

        return Math.Max(1, (int)Math.Ceiling(totalPages * (BookPageQuotaPercent / 100.0)));
    }

    public static string GetPrimaryUnitLabel(MilestoneSourceType sourceType) =>
        sourceType switch
        {
            MilestoneSourceType.Running => "entrenamientos",
            MilestoneSourceType.Gym => "entrenamientos",
            MilestoneSourceType.Puzzle => "rompecabezas",
            MilestoneSourceType.Media => "series terminadas",
            MilestoneSourceType.VideoGame => "avances",
            MilestoneSourceType.Book => "páginas",
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

    public static bool IsBookQuotaMet(int requiredPages, int pagesReadThisWeek, bool completedBookThisWeek) =>
        completedBookThisWeek || (requiredPages > 0 && pagesReadThisWeek >= requiredPages);

    public static string FormatRequirement(MilestoneSourceType sourceType) =>
        FormatRequirement(sourceType, GetRequired(sourceType).Primary, GetRequired(sourceType).Secondary);

    public static string FormatRequirement(MilestoneSourceType sourceType, int primary, int secondary)
    {
        if (sourceType == MilestoneSourceType.Book)
        {
            return primary <= 0
                ? $"{BookPageQuotaPercent}% de páginas del libro actual / semana"
                : $"{primary} páginas ({BookPageQuotaPercent}% del libro actual) / semana";
        }

        if (sourceType == MilestoneSourceType.Media)
        {
            if (primary <= 0 && secondary > 0)
                return $"{secondary} {GetSecondaryUnitLabel(sourceType)} / semana";

            return $"{primary} serie terminada y {secondary} {GetSecondaryUnitLabel(sourceType)} / semana";
        }

        if (secondary <= 0)
            return $"{primary} {GetPrimaryUnitLabel(sourceType)} / semana";

        return $"{primary} {GetPrimaryUnitLabel(sourceType)} y {secondary} {GetSecondaryUnitLabel(sourceType)} / semana";
    }
}
