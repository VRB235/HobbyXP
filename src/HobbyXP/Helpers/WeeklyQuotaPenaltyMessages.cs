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
        var reason = GetMissingProgressReason(evaluation.SourceType);

        return
            $"−{evaluation.HobbyXpRevoked:N0} XP por castigo el {punishedOn}: {reason} en {displayName} (semana del {weekStart})";
    }

    public static string GetMissingProgressReason(MilestoneSourceType sourceType) =>
        sourceType switch
        {
            MilestoneSourceType.Running => "no registraste entrenamientos",
            MilestoneSourceType.Gym => "no registraste entrenamientos",
            MilestoneSourceType.Puzzle => "no registraste rompecabezas",
            MilestoneSourceType.Media => "no terminaste una serie o no registraste 2 películas",
            MilestoneSourceType.VideoGame => "no registraste progreso",
            MilestoneSourceType.Book => "no leíste el 20% del libro actual",
            MilestoneSourceType.Course => "no registraste 5 sesiones",
            MilestoneSourceType.Diet => "no alcanzaste 5 días buenos",
            _ => "no registraste progreso"
        };
}
