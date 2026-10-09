using HobbyXP.Models.Common;
using HobbyXP.Models.Enums;

namespace HobbyXP.Models.Core;

/// <summary>
/// Evaluación de disciplina diaria por hobby (día local 00:00 → UTC).
/// </summary>
public class DailyQuotaEvaluation : EntityBase
{
    public MilestoneSourceType SourceType { get; set; }

    /// <summary>Inicio del día local convertido a UTC.</summary>
    public DateTime DayUtc { get; set; }

    public int RequiredPrimary { get; set; }

    public int ActualPrimary { get; set; }

    public WeeklyQuotaStatus Status { get; set; }

    public int HobbyXpRevoked { get; set; }

    public int GlobalXpRevoked { get; set; }

    public int HobbyLevelBefore { get; set; }

    public int HobbyLevelAfter { get; set; }

    public DateTime? PenalizedAt { get; set; }

    public DateTime? RestoredAt { get; set; }
}
