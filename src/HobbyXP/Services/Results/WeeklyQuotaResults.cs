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
    public string ProgressText => RequiredSecondary > 0
        ? $"{ActualPrimary}/{RequiredPrimary} {PrimaryUnitLabel} · {ActualSecondary}/{RequiredSecondary} {SecondaryUnitLabel}"
        : $"{ActualPrimary}/{RequiredPrimary} {PrimaryUnitLabel}";

    public bool HasActivePenalty => !string.IsNullOrWhiteSpace(ActivePenaltyReminder);
}

public sealed record WeeklyQuotaEvaluationSummary(
    int PenalizedCount,
    int RestoredCount,
    IReadOnlyList<string> Messages);
