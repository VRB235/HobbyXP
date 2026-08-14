using HobbyXP.Models.Common;
using HobbyXP.Models.Enums;

namespace HobbyXP.Models.Core;

/// <summary>
/// Evaluación de disciplina semanal por hobby (lunes–domingo local).
/// </summary>
public class WeeklyQuotaEvaluation : EntityBase
{
    public MilestoneSourceType SourceType { get; set; }

    /// <summary>Lunes 00:00 local convertido a UTC.</summary>
    public DateTime WeekStartUtc { get; set; }

    public int RequiredPrimary { get; set; }

    public int ActualPrimary { get; set; }

    /// <summary>Segunda métrica (p. ej. películas). 0 = no aplica.</summary>
    public int RequiredSecondary { get; set; }

    public int ActualSecondary { get; set; }

    public WeeklyQuotaStatus Status { get; set; }

    public int HobbyXpRevoked { get; set; }

    public int GlobalXpRevoked { get; set; }

    public int HobbyLevelBefore { get; set; }

    public int HobbyLevelAfter { get; set; }

    public DateTime? PenalizedAt { get; set; }

    public DateTime? RestoredAt { get; set; }
}
