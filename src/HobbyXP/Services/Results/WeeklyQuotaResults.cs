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
    string? ActivePenaltyReminder,
    bool HasDailyQuota = false,
    string? DailyRequirementLabel = null,
    int DailyRequiredPrimary = 0,
    int DailyActualPrimary = 0,
    string? DailyPrimaryUnitLabel = null,
    bool IsDailyMet = false,
    bool IsWeeklyMet = false)
{
    public bool IsApplicable =>
        RequiredPrimary > 0 ||
        RequiredSecondary > 0 ||
        (HasDailyQuota && DailyRequiredPrimary > 0);

    public string ProgressText
    {
        get
        {
            if (!IsApplicable)
                return "Sin actividad activa esta semana";

            if (HasDailyQuota && DailyRequiredPrimary > 0)
            {
                var dailyUnit = DailyPrimaryUnitLabel ?? PrimaryUnitLabel;
                var daily = $"Hoy: {DailyActualPrimary}/{DailyRequiredPrimary} {dailyUnit}";
                var weekly = FormatWeeklyProgress();
                return string.IsNullOrEmpty(weekly) ? daily : $"{daily} · Semana: {weekly}";
            }

            return FormatWeeklyProgress();
        }
    }

    private string FormatWeeklyProgress()
    {
        if (RequiredPrimary <= 0 && RequiredSecondary <= 0)
            return string.Empty;

        if (RequiredPrimary > 0 && RequiredSecondary > 0)
            return $"{ActualPrimary}/{RequiredPrimary} {PrimaryUnitLabel} · {ActualSecondary}/{RequiredSecondary} {SecondaryUnitLabel}";

        if (RequiredSecondary > 0)
            return $"{ActualSecondary}/{RequiredSecondary} {SecondaryUnitLabel}";

        return $"{ActualPrimary}/{RequiredPrimary} {PrimaryUnitLabel}";
    }

    public bool HasActivePenalty => !string.IsNullOrWhiteSpace(ActivePenaltyReminder);
}

public sealed record WeeklyQuotaEvaluationSummary(
    int PenalizedCount,
    int RestoredCount,
    IReadOnlyList<string> Messages);
