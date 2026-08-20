using HobbyXP.Models.Enums;

namespace HobbyXP.Helpers;

/// <summary>
/// Cuotas diarias de disciplina (además de la semanal) para Running, Gym, Libro y Curso.
/// </summary>
public static class DailyQuotaRules
{
    public const int SessionsPerDay = 1;
    public const int BookPageQuotaPercent = WeeklyQuotaRules.BookPageQuotaPercent;

    public static readonly MilestoneSourceType[] TrackedSources =
    [
        MilestoneSourceType.Running,
        MilestoneSourceType.Gym,
        MilestoneSourceType.Book,
        MilestoneSourceType.Course
    ];

    public static bool IsTracked(MilestoneSourceType sourceType) =>
        TrackedSources.Contains(sourceType);

    public static int GetRequiredPrimary(MilestoneSourceType sourceType) =>
        sourceType switch
        {
            MilestoneSourceType.Running => SessionsPerDay,
            MilestoneSourceType.Gym => SessionsPerDay,
            MilestoneSourceType.Course => SessionsPerDay,
            MilestoneSourceType.Book => 0, // dinámico: GetBookRequiredPages
            _ => 0
        };

    public static int GetBookRequiredPages(int totalPages) =>
        WeeklyQuotaRules.GetBookRequiredPages(totalPages);

    public static string GetPrimaryUnitLabel(MilestoneSourceType sourceType) =>
        sourceType switch
        {
            MilestoneSourceType.Running => "entrenamientos",
            MilestoneSourceType.Gym => "entrenamientos",
            MilestoneSourceType.Book => "páginas",
            MilestoneSourceType.Course => "sesiones",
            _ => "unidades"
        };

    public static string FormatRequirement(MilestoneSourceType sourceType, int primary)
    {
        if (sourceType == MilestoneSourceType.Book)
        {
            return primary <= 0
                ? $"{BookPageQuotaPercent}% de páginas del libro actual / día"
                : $"{primary} páginas ({BookPageQuotaPercent}% del libro actual) / día";
        }

        return $"{primary} {GetPrimaryUnitLabel(sourceType)} / día";
    }

    public static bool IsMet(int requiredPrimary, int actualPrimary) =>
        requiredPrimary > 0 && actualPrimary >= requiredPrimary;

    public static bool IsBookQuotaMet(int requiredPages, int pagesReadToday, bool completedBookToday) =>
        completedBookToday || (requiredPages > 0 && pagesReadToday >= requiredPages);
}
