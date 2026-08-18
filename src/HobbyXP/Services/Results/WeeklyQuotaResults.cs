using HobbyXP.Models.Enums;

namespace HobbyXP.Services.Results;

public sealed record HobbyLevelPenaltyOutcome(
    int HobbyXpRevoked,
    int GlobalXpRevoked,
    int HobbyLevelBefore,
    int HobbyLevelAfter,
    bool Applied);

public sealed record WeeklyQuotaProgress(
    MilestoneSourceType SourceType,
    string DisplayName,
    string RequirementLabel,
    int RequiredPrimary,
    int ActualPrimary,
    string PrimaryUnitLabel,
    int RequiredSecondary,
    int ActualSecondary,
    string SecondaryUnitLabel,
    bool IsMet,
    WeeklyQuotaStatus? LastClosedStatus,
    string? ActivePenaltyReminder)
{
    public bool IsApplicable => RequiredPrimary > 0 || RequiredSecondary > 0;

    public string ProgressText
    {
        get
        {
            if (!IsApplicable)
                return "Sin actividad activa esta semana";

            if (RequiredPrimary > 0 && RequiredSecondary > 0)
                return $"{ActualPrimary}/{RequiredPrimary} {PrimaryUnitLabel} · {ActualSecondary}/{RequiredSecondary} {SecondaryUnitLabel}";

            if (RequiredSecondary > 0)
                return $"{ActualSecondary}/{RequiredSecondary} {SecondaryUnitLabel}";

            return $"{ActualPrimary}/{RequiredPrimary} {PrimaryUnitLabel}";
        }
    }

    public bool HasActivePenalty => !string.IsNullOrWhiteSpace(ActivePenaltyReminder);
}

public sealed record WeeklyQuotaEvaluationSummary(
    int PenalizedCount,
    int RestoredCount,
    IReadOnlyList<string> Messages);
