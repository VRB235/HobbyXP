using HobbyXP.Models.Core;
using HobbyXP.Models.Enums;

namespace HobbyXP.Helpers;

public static class WeeklyQuotaPenaltyMessages
{
    public static string FormatReminder(WeeklyQuotaEvaluation evaluation)
    {
        var displayName = HobbyProgressCatalog.GetDisplayName(evaluation.SourceType);
        var punishedOn = (evaluation.PenalizedAt ?? evaluation.UpdatedAt ?? evaluation.CreatedAt)
            .ToLocalTime()
            .ToString("dd/MM/yyyy");
        var weekStart = evaluation.WeekStartUtc.ToLocalTime().Date.ToString("dd/MM/yyyy");
        var reason = GetMissingWeeklyProgressReason(evaluation.SourceType);

        return
            $"−{evaluation.HobbyXpRevoked:N0} XP por castigo el {punishedOn}: {reason} en {displayName} (semana del {weekStart})";
    }

    public static string FormatReminder(DailyQuotaEvaluation evaluation)
    {
        var displayName = HobbyProgressCatalog.GetDisplayName(evaluation.SourceType);
        var punishedOn = (evaluation.PenalizedAt ?? evaluation.UpdatedAt ?? evaluation.CreatedAt)
            .ToLocalTime()
            .ToString("dd/MM/yyyy");
        var day = evaluation.DayUtc.ToLocalTime().Date.ToString("dd/MM/yyyy");
        var reason = GetMissingDailyProgressReason(evaluation.SourceType);

        return
            $"−{evaluation.HobbyXpRevoked:N0} XP por castigo el {punishedOn}: {reason} en {displayName} (día {day})";
    }

    public static string GetMissingWeeklyProgressReason(MilestoneSourceType sourceType) =>
        sourceType switch
        {
            MilestoneSourceType.Running => "no registraste 4 entrenamientos",
            MilestoneSourceType.Gym => "no registraste 5 entrenamientos",
            MilestoneSourceType.Puzzle => "no registraste rompecabezas",
            MilestoneSourceType.Media => "no terminaste una serie o no registraste 2 películas",
            MilestoneSourceType.VideoGame => "no registraste progreso",
            MilestoneSourceType.Book => "no terminaste 1 libro",
            MilestoneSourceType.Course => "no registraste 5 sesiones",
            MilestoneSourceType.Diet => "no alcanzaste 5 días buenos",
            _ => "no registraste progreso"
        };

    public static string GetMissingDailyProgressReason(MilestoneSourceType sourceType) =>
        sourceType switch
        {
            MilestoneSourceType.Running => "no registraste 1 entrenamiento",
            MilestoneSourceType.Gym => "no registraste 1 entrenamiento",
            MilestoneSourceType.Book => "no leíste el 20% del libro actual",
            MilestoneSourceType.Course => "no registraste 1 sesión",
            _ => "no registraste progreso"
        };

    /// <summary>Compatibilidad con llamadas antiguas al motivo semanal.</summary>
    public static string GetMissingProgressReason(MilestoneSourceType sourceType) =>
        GetMissingWeeklyProgressReason(sourceType);
}
