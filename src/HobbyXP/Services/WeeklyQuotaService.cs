using HobbyXP.Data;
using HobbyXP.Helpers;
using HobbyXP.Models.Core;
using HobbyXP.Models.Enums;
using HobbyXP.Models.PersonalGrowth;
using HobbyXP.Services.Abstractions;
using HobbyXP.Services.Results;
using Microsoft.EntityFrameworkCore;

namespace HobbyXP.Services;

public sealed class WeeklyQuotaService : IWeeklyQuotaService
{
    /// <summary>
    /// Máximo de semanas cerradas a revisar hacia atrás (desde el inicio de tracking).
    /// </summary>
    private const int MaxClosedWeeksLookback = 26;

    /// <summary>
    /// Máximo de días cerrados a revisar (misma ventana que las semanas).
    /// </summary>
    private const int MaxClosedDaysLookback = MaxClosedWeeksLookback * 7;

    private readonly IDbContextFactory<HobbyXpDbContext> _dbContextFactory;
    private readonly IXpService _xpService;

    public WeeklyQuotaService(
        IDbContextFactory<HobbyXpDbContext> dbContextFactory,
        IXpService xpService)
    {
        _dbContextFactory = dbContextFactory;
        _xpService = xpService;
    }

    public async Task<WeeklyQuotaEvaluationSummary> EvaluateClosedWeeksAsync(
        CancellationToken cancellationToken = default)
    {
        var todayLocal = DateTime.Today;
        var trackingStartLocal = await EnsureTrackingStartAsync(cancellationToken);
        var lookbackFloor = WeekDateHelper.GetWeekStartLocal(todayLocal).AddDays(-(MaxClosedWeeksLookback * 7));
        var fromLocal = trackingStartLocal > lookbackFloor ? trackingStartLocal : lookbackFloor;

        var messages = new List<string>();
        var penalized = 0;
        var restored = 0;

        foreach (var weekStartLocal in WeekDateHelper.EnumerateClosedWeekStartsLocal(fromLocal, todayLocal))
        {
            foreach (var source in WeeklyQuotaRules.TrackedSources)
            {
                var outcome = await EvaluateWeekAsync(source, weekStartLocal, applyPenaltyIfNeeded: true, cancellationToken);
                if (outcome is null)
                    continue;

                if (outcome.Value.JustPenalized)
                {
                    penalized++;
                    messages.Add(outcome.Value.Message);
                }
                else if (outcome.Value.JustRestored)
                {
                    restored++;
                    messages.Add(outcome.Value.Message);
                }
            }
        }

        var dailyStartLocal = await EnsureDailyTrackingStartAsync(cancellationToken);
        var dayLookbackFloor = todayLocal.AddDays(-MaxClosedDaysLookback);
        var dayFromLocal = dailyStartLocal > dayLookbackFloor ? dailyStartLocal : dayLookbackFloor;

        foreach (var dayLocal in WeekDateHelper.EnumerateClosedDaysLocal(dayFromLocal, todayLocal))
        {
            foreach (var source in DailyQuotaRules.TrackedSources)
            {
                var outcome = await EvaluateDayAsync(source, dayLocal, applyPenaltyIfNeeded: true, cancellationToken);
                if (outcome is null)
                    continue;

                if (outcome.Value.JustPenalized)
                {
                    penalized++;
                    messages.Add(outcome.Value.Message);
                }
                else if (outcome.Value.JustRestored)
                {
                    restored++;
                    messages.Add(outcome.Value.Message);
                }
            }
        }

        return new WeeklyQuotaEvaluationSummary(penalized, restored, messages);
    }

    /// <summary>
    /// Primera ejecución: fija el inicio en la semana actual (no castiga el pasado).
    /// Si ya hay evaluaciones, reutiliza la semana más antigua evaluada.
    /// </summary>
    private async Task<DateTime> EnsureTrackingStartAsync(CancellationToken cancellationToken)
    {
        await using var db = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        var profile = await db.PlayerProfiles.FirstOrDefaultAsync(cancellationToken)
            ?? throw new InvalidOperationException("No existe un perfil de jugador inicializado.");

        if (profile.WeeklyQuotaTrackingStartedAtUtc is not null)
            return profile.WeeklyQuotaTrackingStartedAtUtc.Value.ToLocalTime().Date;

        var earliestWeekly = await db.WeeklyQuotaEvaluations
            .Select(e => (DateTime?)e.WeekStartUtc)
            .MinAsync(cancellationToken);
        var earliestDaily = await db.DailyQuotaEvaluations
            .Select(e => (DateTime?)e.DayUtc)
            .MinAsync(cancellationToken);

        DateTime? earliest = null;
        if (earliestWeekly is not null)
            earliest = earliestWeekly;
        if (earliestDaily is not null && (earliest is null || earliestDaily < earliest))
            earliest = earliestDaily;

        var currentWeekStartUtc = WeekDateHelper.GetWeekStartUtc(DateTime.Today);
        profile.WeeklyQuotaTrackingStartedAtUtc = earliest ?? currentWeekStartUtc;
        profile.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);

        return profile.WeeklyQuotaTrackingStartedAtUtc.Value.ToLocalTime().Date;
    }

    /// <summary>
    /// La disciplina diaria arranca en el día de activación (hoy), no en el pasado.
    /// Si ya hubo backfill erróneo (castigos diarios previos), se restaura el XP y se limpia.
    /// </summary>
    private async Task<DateTime> EnsureDailyTrackingStartAsync(CancellationToken cancellationToken)
    {
        await using (var db = await _dbContextFactory.CreateDbContextAsync(cancellationToken))
        {
            var profile = await db.PlayerProfiles.FirstOrDefaultAsync(cancellationToken)
                ?? throw new InvalidOperationException("No existe un perfil de jugador inicializado.");

            if (profile.DailyQuotaTrackingStartedAtUtc is not null)
                return profile.DailyQuotaTrackingStartedAtUtc.Value.ToLocalTime().Date;
        }

        List<(int Id, MilestoneSourceType SourceType, int HobbyXp, int GlobalXp)> toRestore;
        await using (var db = await _dbContextFactory.CreateDbContextAsync(cancellationToken))
        {
            var rows = await db.DailyQuotaEvaluations
                .AsNoTracking()
                .Where(e => e.Status == WeeklyQuotaStatus.Penalized && e.HobbyXpRevoked > 0)
                .Select(e => new { e.Id, e.SourceType, e.HobbyXpRevoked, e.GlobalXpRevoked })
                .ToListAsync(cancellationToken);

            toRestore = rows
                .Select(e => (e.Id, e.SourceType, e.HobbyXpRevoked, e.GlobalXpRevoked))
                .ToList();
        }

        foreach (var item in toRestore)
        {
            await _xpService.RestoreHobbyLevelPenaltyAsync(
                item.SourceType,
                item.HobbyXp,
                item.GlobalXp,
                $"Corrección: castigo diario anticipado ({HobbyProgressCatalog.GetDisplayName(item.SourceType)})",
                item.Id,
                nameof(DailyQuotaEvaluation),
                cancellationToken);
        }

        var todayLocal = DateTime.Today;
        var todayUtc = DateTimeHelper.ToUtcFromLocalDate(todayLocal);

        await using (var db = await _dbContextFactory.CreateDbContextAsync(cancellationToken))
        {
            var profile = await db.PlayerProfiles.FirstAsync(cancellationToken);
            if (profile.DailyQuotaTrackingStartedAtUtc is not null)
                return profile.DailyQuotaTrackingStartedAtUtc.Value.ToLocalTime().Date;

            await db.DailyQuotaEvaluations.ExecuteDeleteAsync(cancellationToken);
            profile.DailyQuotaTrackingStartedAtUtc = todayUtc;
            profile.UpdatedAt = DateTime.UtcNow;
            await db.SaveChangesAsync(cancellationToken);
        }

        return todayLocal;
    }

    public async Task NotifyActivityAsync(
        MilestoneSourceType sourceType,
        DateTime activityLocalDate,
        CancellationToken cancellationToken = default)
    {
        if (!WeeklyQuotaRules.TrackedSources.Contains(sourceType))
            return;

        var activityDay = activityLocalDate.Date;
        var weekStartLocal = WeekDateHelper.GetWeekStartLocal(activityDay);
        var trackingStartLocal = await EnsureTrackingStartAsync(cancellationToken);
        if (weekStartLocal < trackingStartLocal)
            return; // fuera del periodo de disciplina

        var today = DateTime.Today;
        var isClosedWeek = WeekDateHelper.IsClosedWeek(weekStartLocal, today);
        await EvaluateWeekAsync(sourceType, weekStartLocal, applyPenaltyIfNeeded: isClosedWeek, cancellationToken);

        if (DailyQuotaRules.IsTracked(sourceType) && activityDay >= await EnsureDailyTrackingStartAsync(cancellationToken))
        {
            var isClosedDay = WeekDateHelper.IsClosedDay(activityDay, today);
            await EvaluateDayAsync(sourceType, activityDay, applyPenaltyIfNeeded: isClosedDay, cancellationToken);
        }
    }

    public async Task<IReadOnlyList<WeeklyQuotaProgress>> GetCurrentWeekProgressAsync(
        CancellationToken cancellationToken = default)
    {
        var todayLocal = DateTime.Today;
        var weekStartLocal = WeekDateHelper.GetWeekStartLocal(todayLocal);
        var counts = await CountActivityAsync(weekStartLocal, cancellationToken);
        await using var db = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        var result = new List<WeeklyQuotaProgress>();
        foreach (var source in WeeklyQuotaRules.TrackedSources)
        {
            var need = await ResolveRequirementAsync(source, weekStartLocal, cancellationToken);
            var (actualPrimary, actualSecondary) = counts[source];
            var weeklyMet = await IsQuotaMetAsync(source, need, actualPrimary, actualSecondary, weekStartLocal, cancellationToken);

            DailyNeed? dailyNeed = null;
            var dailyActual = 0;
            var dailyMet = false;
            var hasDaily = DailyQuotaRules.IsTracked(source);
            if (hasDaily)
            {
                var resolvedDaily = await ResolveDailyNeedAsync(source, todayLocal, cancellationToken);
                dailyNeed = resolvedDaily;
                dailyActual = await CountDailyActivityForSourceAsync(source, todayLocal, cancellationToken);
                dailyMet = await IsDailyQuotaMetAsync(source, resolvedDaily, dailyActual, todayLocal, cancellationToken);
            }

            // Badge «Cumplida»: cuota diaria de hoy si aplica; si no, la semanal.
            var isMet = hasDaily && dailyNeed is { Primary: > 0 }
                ? dailyMet
                : weeklyMet;

            var weekStartUtc = WeekDateHelper.GetWeekStartUtc(weekStartLocal);
            var lastClosed = await db.WeeklyQuotaEvaluations
                .AsNoTracking()
                .Where(e => e.SourceType == source && e.WeekStartUtc < weekStartUtc)
                .OrderByDescending(e => e.WeekStartUtc)
                .Select(e => (WeeklyQuotaStatus?)e.Status)
                .FirstOrDefaultAsync(cancellationToken);

            var weeklyPenalties = await db.WeeklyQuotaEvaluations
                .AsNoTracking()
                .Where(e => e.SourceType == source &&
                            e.Status == WeeklyQuotaStatus.Penalized &&
                            e.HobbyXpRevoked > 0)
                .OrderByDescending(e => e.PenalizedAt ?? e.WeekStartUtc)
                .ToListAsync(cancellationToken);

            var dailyPenalties = await db.DailyQuotaEvaluations
                .AsNoTracking()
                .Where(e => e.SourceType == source &&
                            e.Status == WeeklyQuotaStatus.Penalized &&
                            e.HobbyXpRevoked > 0)
                .OrderByDescending(e => e.PenalizedAt ?? e.DayUtc)
                .ToListAsync(cancellationToken);

            var reminderParts = weeklyPenalties
                .Select(WeeklyQuotaPenaltyMessages.FormatReminder)
                .Concat(dailyPenalties.Select(WeeklyQuotaPenaltyMessages.FormatReminder))
                .ToList();
            var reminder = reminderParts.Count == 0
                ? null
                : string.Join(Environment.NewLine, reminderParts);

            var requirementLabel = hasDaily && dailyNeed is not null && dailyNeed.Value.Primary > 0
                ? $"{dailyNeed.Value.Label} · {need.Label}"
                : need.Label;

            result.Add(new WeeklyQuotaProgress(
                source,
                HobbyProgressCatalog.GetDisplayName(source),
                requirementLabel,
                need.Primary,
                actualPrimary,
                need.PrimaryUnit,
                need.Secondary,
                actualSecondary,
                need.SecondaryUnit,
                isMet,
                lastClosed,
                reminder,
                HasDailyQuota: hasDaily,
                DailyRequirementLabel: dailyNeed?.Label,
                DailyRequiredPrimary: dailyNeed?.Primary ?? 0,
                DailyActualPrimary: dailyActual,
                DailyPrimaryUnitLabel: dailyNeed?.PrimaryUnit,
                IsDailyMet: dailyMet,
                IsWeeklyMet: weeklyMet));
        }

        return result;
    }

    public async Task<IReadOnlyList<string>> GetActivePenaltyRemindersAsync(
        MilestoneSourceType sourceType,
        CancellationToken cancellationToken = default)
    {
        if (!WeeklyQuotaRules.TrackedSources.Contains(sourceType))
            return Array.Empty<string>();

        await using var db = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        var weekly = await db.WeeklyQuotaEvaluations
            .AsNoTracking()
            .Where(e => e.SourceType == sourceType &&
                        e.Status == WeeklyQuotaStatus.Penalized &&
                        e.HobbyXpRevoked > 0)
            .OrderByDescending(e => e.PenalizedAt ?? e.WeekStartUtc)
            .ToListAsync(cancellationToken);

        var daily = await db.DailyQuotaEvaluations
            .AsNoTracking()
            .Where(e => e.SourceType == sourceType &&
                        e.Status == WeeklyQuotaStatus.Penalized &&
                        e.HobbyXpRevoked > 0)
            .OrderByDescending(e => e.PenalizedAt ?? e.DayUtc)
            .ToListAsync(cancellationToken);

        return weekly.Select(WeeklyQuotaPenaltyMessages.FormatReminder)
            .Concat(daily.Select(WeeklyQuotaPenaltyMessages.FormatReminder))
            .ToList();
    }

    private async Task<EvaluationTick?> EvaluateWeekAsync(
        MilestoneSourceType sourceType,
        DateTime weekStartLocal,
        bool applyPenaltyIfNeeded,
        CancellationToken cancellationToken)
    {
        var need = await ResolveRequirementAsync(sourceType, weekStartLocal, cancellationToken);
        if (need.Primary <= 0 && need.Secondary <= 0)
            return null;

        if (!await ShouldEvaluateSourceWeekAsync(sourceType, weekStartLocal, cancellationToken))
            return null;

        var weekStartUtc = DateTimeHelper.ToUtcFromLocalDate(weekStartLocal);
        var counts = await CountActivityForSourceAsync(sourceType, weekStartLocal, cancellationToken);
        var met = await IsQuotaMetAsync(sourceType, need, counts.Primary, counts.Secondary, weekStartLocal, cancellationToken);
        var requiredPrimary = need.Primary;
        var requiredSecondary = need.Secondary;

        await using var db = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        var evaluation = await db.WeeklyQuotaEvaluations
            .FirstOrDefaultAsync(
                e => e.SourceType == sourceType && e.WeekStartUtc == weekStartUtc,
                cancellationToken);

        if (evaluation is null)
        {
            if (met)
            {
                db.WeeklyQuotaEvaluations.Add(new WeeklyQuotaEvaluation
                {
                    SourceType = sourceType,
                    WeekStartUtc = weekStartUtc,
                    RequiredPrimary = requiredPrimary,
                    RequiredSecondary = requiredSecondary,
                    ActualPrimary = counts.Primary,
                    ActualSecondary = counts.Secondary,
                    Status = WeeklyQuotaStatus.Met
                });
                await db.SaveChangesAsync(cancellationToken);
                return null;
            }

            if (!applyPenaltyIfNeeded)
                return null;

            if (await HasActiveImmunityAsync(cancellationToken))
            {
                db.WeeklyQuotaEvaluations.Add(new WeeklyQuotaEvaluation
                {
                    SourceType = sourceType,
                    WeekStartUtc = weekStartUtc,
                    RequiredPrimary = requiredPrimary,
                    RequiredSecondary = requiredSecondary,
                    ActualPrimary = counts.Primary,
                    ActualSecondary = counts.Secondary,
                    Status = WeeklyQuotaStatus.Waived,
                    PenalizedAt = DateTime.UtcNow
                });
                await db.SaveChangesAsync(cancellationToken);
                return new EvaluationTick(
                    JustPenalized: false,
                    JustRestored: false,
                    $"Inmunidad: {HobbyProgressCatalog.GetDisplayName(sourceType)} no recibió castigo.");
            }

            evaluation = new WeeklyQuotaEvaluation
            {
                SourceType = sourceType,
                WeekStartUtc = weekStartUtc,
                RequiredPrimary = requiredPrimary,
                RequiredSecondary = requiredSecondary,
                ActualPrimary = counts.Primary,
                ActualSecondary = counts.Secondary,
                Status = WeeklyQuotaStatus.Met // placeholder hasta ApplyPenalty
            };
            db.WeeklyQuotaEvaluations.Add(evaluation);
            await db.SaveChangesAsync(cancellationToken);
            return await ApplyWeeklyPenaltyAsync(evaluation, cancellationToken);
        }

        evaluation.ActualPrimary = counts.Primary;
        evaluation.ActualSecondary = counts.Secondary;
        evaluation.RequiredPrimary = requiredPrimary;
        evaluation.RequiredSecondary = requiredSecondary;
        evaluation.UpdatedAt = DateTime.UtcNow;

        if (met)
        {
            if (evaluation.Status == WeeklyQuotaStatus.Penalized)
            {
                await db.SaveChangesAsync(cancellationToken);
                return await RestoreWeeklyPenaltyAsync(evaluation, cancellationToken);
            }

            evaluation.Status = WeeklyQuotaStatus.Met;
            await db.SaveChangesAsync(cancellationToken);
            return null;
        }

        if (evaluation.Status is WeeklyQuotaStatus.Penalized or WeeklyQuotaStatus.SkippedFloor or WeeklyQuotaStatus.Waived)
        {
            await db.SaveChangesAsync(cancellationToken);
            return null;
        }

        if (evaluation.Status == WeeklyQuotaStatus.Restored)
        {
            await db.SaveChangesAsync(cancellationToken);
            return null;
        }

        if (!applyPenaltyIfNeeded)
        {
            await db.SaveChangesAsync(cancellationToken);
            return null;
        }

        await db.SaveChangesAsync(cancellationToken);
        return await ApplyWeeklyPenaltyAsync(evaluation, cancellationToken);
    }

    private async Task<EvaluationTick?> EvaluateDayAsync(
        MilestoneSourceType sourceType,
        DateTime dayLocal,
        bool applyPenaltyIfNeeded,
        CancellationToken cancellationToken)
    {
        if (!DailyQuotaRules.IsTracked(sourceType))
            return null;

        var need = await ResolveDailyNeedAsync(sourceType, dayLocal, cancellationToken);
        if (need.Primary <= 0)
            return null;

        var dayUtc = DateTimeHelper.ToUtcFromLocalDate(dayLocal);
        var actual = await CountDailyActivityForSourceAsync(sourceType, dayLocal, cancellationToken);
        var met = await IsDailyQuotaMetAsync(sourceType, need, actual, dayLocal, cancellationToken);

        await using var db = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        var evaluation = await db.DailyQuotaEvaluations
            .FirstOrDefaultAsync(
                e => e.SourceType == sourceType && e.DayUtc == dayUtc,
                cancellationToken);

        if (evaluation is null)
        {
            if (met)
            {
                db.DailyQuotaEvaluations.Add(new DailyQuotaEvaluation
                {
                    SourceType = sourceType,
                    DayUtc = dayUtc,
                    RequiredPrimary = need.Primary,
                    ActualPrimary = actual,
                    Status = WeeklyQuotaStatus.Met
                });
                await db.SaveChangesAsync(cancellationToken);
                return null;
            }

            if (!applyPenaltyIfNeeded)
                return null;

            if (await HasActiveImmunityAsync(cancellationToken))
            {
                db.DailyQuotaEvaluations.Add(new DailyQuotaEvaluation
                {
                    SourceType = sourceType,
                    DayUtc = dayUtc,
                    RequiredPrimary = need.Primary,
                    ActualPrimary = actual,
                    Status = WeeklyQuotaStatus.Waived,
                    PenalizedAt = DateTime.UtcNow
                });
                await db.SaveChangesAsync(cancellationToken);
                return new EvaluationTick(
                    JustPenalized: false,
                    JustRestored: false,
                    $"Inmunidad diaria: {HobbyProgressCatalog.GetDisplayName(sourceType)} no recibió castigo.");
            }

            evaluation = new DailyQuotaEvaluation
            {
                SourceType = sourceType,
                DayUtc = dayUtc,
                RequiredPrimary = need.Primary,
                ActualPrimary = actual,
                Status = WeeklyQuotaStatus.Met
            };
            db.DailyQuotaEvaluations.Add(evaluation);
            await db.SaveChangesAsync(cancellationToken);
            return await ApplyDailyPenaltyAsync(evaluation, cancellationToken);
        }

        evaluation.ActualPrimary = actual;
        evaluation.RequiredPrimary = need.Primary;
        evaluation.UpdatedAt = DateTime.UtcNow;

        if (met)
        {
            if (evaluation.Status == WeeklyQuotaStatus.Penalized)
            {
                await db.SaveChangesAsync(cancellationToken);
                return await RestoreDailyPenaltyAsync(evaluation, cancellationToken);
            }

            evaluation.Status = WeeklyQuotaStatus.Met;
            await db.SaveChangesAsync(cancellationToken);
            return null;
        }

        if (evaluation.Status is WeeklyQuotaStatus.Penalized or WeeklyQuotaStatus.SkippedFloor or WeeklyQuotaStatus.Waived)
        {
            await db.SaveChangesAsync(cancellationToken);
            return null;
        }

        if (evaluation.Status == WeeklyQuotaStatus.Restored)
        {
            await db.SaveChangesAsync(cancellationToken);
            return null;
        }

        if (!applyPenaltyIfNeeded)
        {
            await db.SaveChangesAsync(cancellationToken);
            return null;
        }

        await db.SaveChangesAsync(cancellationToken);
        return await ApplyDailyPenaltyAsync(evaluation, cancellationToken);
    }

    private async Task<bool> HasActiveImmunityAsync(CancellationToken cancellationToken)
    {
        await using var db = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        var until = await db.PlayerProfiles
            .Select(p => p.DisciplineImmunityUntilUtc)
            .FirstOrDefaultAsync(cancellationToken);
        return MedalPrivilegeRules.IsActive(until, DateTime.UtcNow);
    }

    private async Task<EvaluationTick> ApplyWeeklyPenaltyAsync(
        WeeklyQuotaEvaluation evaluation,
        CancellationToken cancellationToken)
    {
        if (await HasActiveImmunityAsync(cancellationToken))
        {
            await using var immuneDb = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
            var trackedImmune = await immuneDb.WeeklyQuotaEvaluations.FindAsync([evaluation.Id], cancellationToken)
                ?? evaluation;
            trackedImmune.Status = WeeklyQuotaStatus.Waived;
            trackedImmune.HobbyXpRevoked = 0;
            trackedImmune.GlobalXpRevoked = 0;
            trackedImmune.PenalizedAt = DateTime.UtcNow;
            trackedImmune.UpdatedAt = DateTime.UtcNow;
            await immuneDb.SaveChangesAsync(cancellationToken);

            return new EvaluationTick(
                JustPenalized: false,
                JustRestored: false,
                $"Inmunidad: {HobbyProgressCatalog.GetDisplayName(evaluation.SourceType)} no recibió castigo.");
        }

        var weekLabel = evaluation.WeekStartUtc.ToLocalTime().Date.ToString("dd/MM/yyyy");
        var description =
            $"Castigo semanal ({HobbyProgressCatalog.GetDisplayName(evaluation.SourceType)}) · semana del {weekLabel}";

        var outcome = await _xpService.ApplyHobbyLevelDownPenaltyAsync(
            evaluation.SourceType,
            description,
            evaluation.Id,
            nameof(WeeklyQuotaEvaluation),
            cancellationToken);

        await using var db = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        var tracked = await db.WeeklyQuotaEvaluations.FindAsync([evaluation.Id], cancellationToken)
            ?? evaluation;

        if (!outcome.Applied)
        {
            tracked.Status = WeeklyQuotaStatus.SkippedFloor;
            tracked.HobbyLevelBefore = outcome.HobbyLevelBefore;
            tracked.HobbyLevelAfter = outcome.HobbyLevelAfter;
            tracked.HobbyXpRevoked = 0;
            tracked.GlobalXpRevoked = 0;
            tracked.PenalizedAt = DateTime.UtcNow;
            tracked.UpdatedAt = DateTime.UtcNow;
            await db.SaveChangesAsync(cancellationToken);

            return new EvaluationTick(
                JustPenalized: false,
                JustRestored: false,
                $"Disciplina {HobbyProgressCatalog.GetDisplayName(evaluation.SourceType)} (semana {weekLabel}): sin XP que castigar.");
        }

        tracked.Status = WeeklyQuotaStatus.Penalized;
        tracked.HobbyXpRevoked = outcome.HobbyXpRevoked;
        tracked.GlobalXpRevoked = outcome.GlobalXpRevoked;
        tracked.HobbyLevelBefore = outcome.HobbyLevelBefore;
        tracked.HobbyLevelAfter = outcome.HobbyLevelAfter;
        tracked.PenalizedAt = DateTime.UtcNow;
        tracked.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);

        return new EvaluationTick(
            JustPenalized: true,
            JustRestored: false,
            $"Castigo {HobbyProgressCatalog.GetDisplayName(evaluation.SourceType)} (semana {weekLabel}): −{outcome.HobbyXpRevoked} XP · nivel {outcome.HobbyLevelBefore}→{outcome.HobbyLevelAfter}");
    }

    private async Task<EvaluationTick> ApplyDailyPenaltyAsync(
        DailyQuotaEvaluation evaluation,
        CancellationToken cancellationToken)
    {
        if (await HasActiveImmunityAsync(cancellationToken))
        {
            await using var immuneDb = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
            var trackedImmune = await immuneDb.DailyQuotaEvaluations.FindAsync([evaluation.Id], cancellationToken)
                ?? evaluation;
            trackedImmune.Status = WeeklyQuotaStatus.Waived;
            trackedImmune.HobbyXpRevoked = 0;
            trackedImmune.GlobalXpRevoked = 0;
            trackedImmune.PenalizedAt = DateTime.UtcNow;
            trackedImmune.UpdatedAt = DateTime.UtcNow;
            await immuneDb.SaveChangesAsync(cancellationToken);

            return new EvaluationTick(
                JustPenalized: false,
                JustRestored: false,
                $"Inmunidad diaria: {HobbyProgressCatalog.GetDisplayName(evaluation.SourceType)} no recibió castigo.");
        }

        var dayLabel = evaluation.DayUtc.ToLocalTime().Date.ToString("dd/MM/yyyy");
        var description =
            $"Castigo diario ({HobbyProgressCatalog.GetDisplayName(evaluation.SourceType)}) · día {dayLabel}";

        var outcome = await _xpService.ApplyHobbyLevelDownPenaltyAsync(
            evaluation.SourceType,
            description,
            evaluation.Id,
            nameof(DailyQuotaEvaluation),
            cancellationToken);

        await using var db = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        var tracked = await db.DailyQuotaEvaluations.FindAsync([evaluation.Id], cancellationToken)
            ?? evaluation;

        if (!outcome.Applied)
        {
            tracked.Status = WeeklyQuotaStatus.SkippedFloor;
            tracked.HobbyLevelBefore = outcome.HobbyLevelBefore;
            tracked.HobbyLevelAfter = outcome.HobbyLevelAfter;
            tracked.HobbyXpRevoked = 0;
            tracked.GlobalXpRevoked = 0;
            tracked.PenalizedAt = DateTime.UtcNow;
            tracked.UpdatedAt = DateTime.UtcNow;
            await db.SaveChangesAsync(cancellationToken);

            return new EvaluationTick(
                JustPenalized: false,
                JustRestored: false,
                $"Disciplina diaria {HobbyProgressCatalog.GetDisplayName(evaluation.SourceType)} ({dayLabel}): sin XP que castigar.");
        }

        tracked.Status = WeeklyQuotaStatus.Penalized;
        tracked.HobbyXpRevoked = outcome.HobbyXpRevoked;
        tracked.GlobalXpRevoked = outcome.GlobalXpRevoked;
        tracked.HobbyLevelBefore = outcome.HobbyLevelBefore;
        tracked.HobbyLevelAfter = outcome.HobbyLevelAfter;
        tracked.PenalizedAt = DateTime.UtcNow;
        tracked.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);

        return new EvaluationTick(
            JustPenalized: true,
            JustRestored: false,
            $"Castigo diario {HobbyProgressCatalog.GetDisplayName(evaluation.SourceType)} ({dayLabel}): −{outcome.HobbyXpRevoked} XP · nivel {outcome.HobbyLevelBefore}→{outcome.HobbyLevelAfter}");
    }

    private async Task<EvaluationTick> RestoreWeeklyPenaltyAsync(
        WeeklyQuotaEvaluation evaluation,
        CancellationToken cancellationToken)
    {
        var weekLabel = evaluation.WeekStartUtc.ToLocalTime().Date.ToString("dd/MM/yyyy");
        var description =
            $"Restauración semanal ({HobbyProgressCatalog.GetDisplayName(evaluation.SourceType)}) · semana del {weekLabel}";

        await _xpService.RestoreHobbyLevelPenaltyAsync(
            evaluation.SourceType,
            evaluation.HobbyXpRevoked,
            evaluation.GlobalXpRevoked,
            description,
            evaluation.Id,
            nameof(WeeklyQuotaEvaluation),
            cancellationToken);

        await using var db = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        var tracked = await db.WeeklyQuotaEvaluations.FindAsync([evaluation.Id], cancellationToken)
            ?? evaluation;

        tracked.Status = WeeklyQuotaStatus.Restored;
        tracked.RestoredAt = DateTime.UtcNow;
        tracked.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);

        return new EvaluationTick(
            JustPenalized: false,
            JustRestored: true,
            $"Restaurado {HobbyProgressCatalog.GetDisplayName(evaluation.SourceType)} (semana {weekLabel}): +{evaluation.HobbyXpRevoked} XP");
    }

    private async Task<EvaluationTick> RestoreDailyPenaltyAsync(
        DailyQuotaEvaluation evaluation,
        CancellationToken cancellationToken)
    {
        var dayLabel = evaluation.DayUtc.ToLocalTime().Date.ToString("dd/MM/yyyy");
        var description =
            $"Restauración diaria ({HobbyProgressCatalog.GetDisplayName(evaluation.SourceType)}) · día {dayLabel}";

        await _xpService.RestoreHobbyLevelPenaltyAsync(
            evaluation.SourceType,
            evaluation.HobbyXpRevoked,
            evaluation.GlobalXpRevoked,
            description,
            evaluation.Id,
            nameof(DailyQuotaEvaluation),
            cancellationToken);

        await using var db = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        var tracked = await db.DailyQuotaEvaluations.FindAsync([evaluation.Id], cancellationToken)
            ?? evaluation;

        tracked.Status = WeeklyQuotaStatus.Restored;
        tracked.RestoredAt = DateTime.UtcNow;
        tracked.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);

        return new EvaluationTick(
            JustPenalized: false,
            JustRestored: true,
            $"Restaurado diario {HobbyProgressCatalog.GetDisplayName(evaluation.SourceType)} ({dayLabel}): +{evaluation.HobbyXpRevoked} XP");
    }

    /// <summary>
    /// Dieta no existía en el tracking global: no se evalúan semanas anteriores al primer log.
    /// Sin logs, no hay castigo (el hobby aún no empezó).
    /// </summary>
    private async Task<bool> ShouldEvaluateSourceWeekAsync(
        MilestoneSourceType sourceType,
        DateTime weekStartLocal,
        CancellationToken cancellationToken)
    {
        if (sourceType != MilestoneSourceType.Diet)
            return true;

        await using var db = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        var firstDayUtc = await db.DietDayLogs
            .Select(d => (DateTime?)d.DayDate)
            .MinAsync(cancellationToken);

        if (firstDayUtc is null)
            return false;

        var firstWeekStartLocal = WeekDateHelper.GetWeekStartLocal(firstDayUtc.Value.ToLocalTime());
        return weekStartLocal >= firstWeekStartLocal;
    }

    private async Task<Dictionary<MilestoneSourceType, (int Primary, int Secondary)>> CountActivityAsync(
        DateTime weekStartLocal,
        CancellationToken cancellationToken)
    {
        var map = new Dictionary<MilestoneSourceType, (int, int)>();
        foreach (var source in WeeklyQuotaRules.TrackedSources)
            map[source] = await CountActivityForSourceAsync(source, weekStartLocal, cancellationToken);
        return map;
    }

    private async Task<(int Primary, int Secondary)> CountActivityForSourceAsync(
        MilestoneSourceType sourceType,
        DateTime weekStartLocal,
        CancellationToken cancellationToken)
    {
        var startUtc = DateTimeHelper.ToUtcFromLocalDate(weekStartLocal);
        var endUtc = WeekDateHelper.GetWeekEndExclusiveUtc(startUtc);

        await using var db = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        return sourceType switch
        {
            MilestoneSourceType.Running => (
                await db.RunningSessions.CountAsync(
                    s => s.RecordedAt >= startUtc && s.RecordedAt < endUtc,
                    cancellationToken),
                0),

            MilestoneSourceType.Gym => (
                await db.GymWorkouts.CountAsync(
                    w => w.WorkoutDate >= startUtc && w.WorkoutDate < endUtc,
                    cancellationToken),
                0),

            MilestoneSourceType.Puzzle => (
                await db.Puzzles.CountAsync(
                    p => p.CompletedAt >= startUtc && p.CompletedAt < endUtc,
                    cancellationToken),
                0),

            MilestoneSourceType.Media => (
                await CountSeriesActivityAsync(db, startUtc, endUtc, cancellationToken),
                await db.MediaEntries.CountAsync(
                    m => m.MediaType == MediaType.Movie &&
                         m.CompletedAt >= startUtc &&
                         m.CompletedAt < endUtc,
                    cancellationToken)),

            MilestoneSourceType.VideoGame => (
                await db.VideoGameProgressLogs.CountAsync(
                    l => l.ProgressDate >= startUtc && l.ProgressDate < endUtc && l.PercentDelta > 0,
                    cancellationToken),
                0),

            MilestoneSourceType.Book => (
                await db.Books.CountAsync(
                    b => b.CompletedAt != null && b.CompletedAt >= startUtc && b.CompletedAt < endUtc,
                    cancellationToken),
                0),

            MilestoneSourceType.Course => (
                await db.CourseSessionLogs
                    .Where(l => l.SessionDate >= startUtc && l.SessionDate < endUtc)
                    .SumAsync(l => l.SessionsDone, cancellationToken),
                0),

            MilestoneSourceType.Diet => (
                await db.DietDayLogs.CountAsync(
                    d => d.DayDate >= startUtc &&
                         d.DayDate < endUtc &&
                         d.OnPlanCount >= DietDayRules.GoodDayThreshold,
                    cancellationToken),
                0),

            _ => (0, 0)
        };
    }

    private async Task<int> CountDailyActivityForSourceAsync(
        MilestoneSourceType sourceType,
        DateTime dayLocal,
        CancellationToken cancellationToken)
    {
        var startUtc = DateTimeHelper.ToUtcFromLocalDate(dayLocal);
        var endUtc = startUtc.AddDays(1);

        await using var db = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        return sourceType switch
        {
            MilestoneSourceType.Running => await db.RunningSessions.CountAsync(
                s => s.RecordedAt >= startUtc && s.RecordedAt < endUtc,
                cancellationToken),

            MilestoneSourceType.Gym => await db.GymWorkouts.CountAsync(
                w => w.WorkoutDate >= startUtc && w.WorkoutDate < endUtc,
                cancellationToken),

            MilestoneSourceType.Course => await db.CourseSessionLogs
                .Where(l => l.SessionDate >= startUtc && l.SessionDate < endUtc)
                .SumAsync(l => l.SessionsDone, cancellationToken),

            MilestoneSourceType.Book => await CountBookPagesForDayAsync(db, startUtc, endUtc, cancellationToken),

            _ => 0
        };
    }

    private static async Task<int> CountSeriesActivityAsync(
        HobbyXpDbContext db,
        DateTime startUtc,
        DateTime endUtc,
        CancellationToken cancellationToken) =>
        await db.MediaEntries.CountAsync(
            m => m.MediaType == MediaType.Series &&
                 m.CompletedAt >= startUtc &&
                 m.CompletedAt < endUtc,
            cancellationToken);

    private static async Task<int> CountBookPagesForDayAsync(
        HobbyXpDbContext db,
        DateTime startUtc,
        DateTime endUtc,
        CancellationToken cancellationToken)
    {
        var current = await FindCurrentReadingBookAsync(db, startUtc, endUtc, cancellationToken);
        var query = db.BookReadingLogs.Where(l => l.ReadDate >= startUtc && l.ReadDate < endUtc);
        if (current is not null)
            query = query.Where(l => l.BookId == current.Id);

        return await query.SumAsync(l => (int?)l.PagesDone, cancellationToken) ?? 0;
    }

    private Task<bool> IsQuotaMetAsync(
        MilestoneSourceType sourceType,
        QuotaNeed need,
        int actualPrimary,
        int actualSecondary,
        DateTime weekStartLocal,
        CancellationToken cancellationToken)
    {
        _ = sourceType;
        _ = weekStartLocal;
        _ = cancellationToken;
        return Task.FromResult(WeeklyQuotaRules.IsMet(need.Primary, actualPrimary, need.Secondary, actualSecondary));
    }

    private async Task<bool> IsDailyQuotaMetAsync(
        MilestoneSourceType sourceType,
        DailyNeed need,
        int actualPrimary,
        DateTime dayLocal,
        CancellationToken cancellationToken)
    {
        if (sourceType == MilestoneSourceType.Book)
        {
            var completed = await AnyBookCompletedOnDayAsync(dayLocal, cancellationToken);
            return DailyQuotaRules.IsBookQuotaMet(need.Primary, actualPrimary, completed);
        }

        return DailyQuotaRules.IsMet(need.Primary, actualPrimary);
    }

    private async Task<QuotaNeed> ResolveRequirementAsync(
        MilestoneSourceType sourceType,
        DateTime weekStartLocal,
        CancellationToken cancellationToken)
    {
        var startUtc = DateTimeHelper.ToUtcFromLocalDate(weekStartLocal);
        var endUtc = WeekDateHelper.GetWeekEndExclusiveUtc(startUtc);
        var (staticPrimary, staticSecondary) = WeeklyQuotaRules.GetRequired(sourceType);

        if (sourceType == MilestoneSourceType.Book)
            return await ResolveBookWeeklyNeedAsync(startUtc, endUtc, cancellationToken);

        if (sourceType == MilestoneSourceType.Course)
        {
            var hasCourse = await HasActiveCourseAsync(startUtc, endUtc, cancellationToken);
            var primary = hasCourse ? WeeklyQuotaRules.CourseSessionsRequired : 0;
            return new QuotaNeed(
                primary,
                0,
                WeeklyQuotaRules.FormatRequirement(sourceType, primary, 0),
                WeeklyQuotaRules.GetPrimaryUnitLabel(sourceType),
                string.Empty);
        }

        if (sourceType == MilestoneSourceType.Media)
        {
            var hasSeries = await HasSeriesObligationAsync(startUtc, endUtc, cancellationToken);
            var primary = hasSeries ? WeeklyQuotaRules.SeriesCompletedRequired : 0;
            return new QuotaNeed(
                primary,
                WeeklyQuotaRules.MoviesRequired,
                WeeklyQuotaRules.FormatRequirement(sourceType, primary, WeeklyQuotaRules.MoviesRequired),
                WeeklyQuotaRules.GetPrimaryUnitLabel(sourceType),
                WeeklyQuotaRules.GetSecondaryUnitLabel(sourceType));
        }

        return new QuotaNeed(
            staticPrimary,
            staticSecondary,
            WeeklyQuotaRules.FormatRequirement(sourceType, staticPrimary, staticSecondary),
            WeeklyQuotaRules.GetPrimaryUnitLabel(sourceType),
            WeeklyQuotaRules.GetSecondaryUnitLabel(sourceType));
    }

    private async Task<DailyNeed> ResolveDailyNeedAsync(
        MilestoneSourceType sourceType,
        DateTime dayLocal,
        CancellationToken cancellationToken)
    {
        var startUtc = DateTimeHelper.ToUtcFromLocalDate(dayLocal);
        var endUtc = startUtc.AddDays(1);

        if (sourceType == MilestoneSourceType.Book)
            return await ResolveBookDailyNeedAsync(startUtc, endUtc, cancellationToken);

        if (sourceType == MilestoneSourceType.Course)
        {
            var hasCourse = await HasActiveCourseAsync(startUtc, endUtc, cancellationToken);
            var primary = hasCourse ? DailyQuotaRules.SessionsPerDay : 0;
            return new DailyNeed(
                primary,
                DailyQuotaRules.FormatRequirement(sourceType, primary),
                DailyQuotaRules.GetPrimaryUnitLabel(sourceType));
        }

        var required = DailyQuotaRules.GetRequiredPrimary(sourceType);
        return new DailyNeed(
            required,
            DailyQuotaRules.FormatRequirement(sourceType, required),
            DailyQuotaRules.GetPrimaryUnitLabel(sourceType));
    }

    private async Task<QuotaNeed> ResolveBookWeeklyNeedAsync(
        DateTime startUtc,
        DateTime endUtc,
        CancellationToken cancellationToken)
    {
        await using var db = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        var hasReading = await db.Books.AnyAsync(
            b => b.Status == BookStatus.Reading && b.CreatedAt < endUtc,
            cancellationToken);
        var completedCount = await db.Books.CountAsync(
            b => b.CompletedAt != null && b.CompletedAt >= startUtc && b.CompletedAt < endUtc,
            cancellationToken);

        if (!hasReading && completedCount == 0)
        {
            return new QuotaNeed(
                0,
                0,
                WeeklyQuotaRules.FormatRequirement(MilestoneSourceType.Book, 0, 0),
                WeeklyQuotaRules.GetPrimaryUnitLabel(MilestoneSourceType.Book),
                string.Empty);
        }

        var required = WeeklyQuotaRules.BooksCompletedRequired;
        return new QuotaNeed(
            required,
            0,
            WeeklyQuotaRules.FormatRequirement(MilestoneSourceType.Book, required, 0),
            WeeklyQuotaRules.GetPrimaryUnitLabel(MilestoneSourceType.Book),
            string.Empty);
    }

    private async Task<DailyNeed> ResolveBookDailyNeedAsync(
        DateTime startUtc,
        DateTime endUtc,
        CancellationToken cancellationToken)
    {
        await using var db = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        var current = await FindCurrentReadingBookAsync(db, startUtc, endUtc, cancellationToken);
        if (current is not null)
        {
            var required = DailyQuotaRules.GetBookRequiredPages(current.TotalPages);
            return new DailyNeed(
                required,
                $"20% de «{current.Title}» ({required} páginas) / día",
                DailyQuotaRules.GetPrimaryUnitLabel(MilestoneSourceType.Book));
        }

        var completed = await db.Books
            .AsNoTracking()
            .Where(b => b.CompletedAt != null && b.CompletedAt >= startUtc && b.CompletedAt < endUtc)
            .OrderByDescending(b => b.CompletedAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (completed is not null)
        {
            var required = DailyQuotaRules.GetBookRequiredPages(completed.TotalPages);
            return new DailyNeed(
                required,
                $"Libro terminado hoy: «{completed.Title}»",
                DailyQuotaRules.GetPrimaryUnitLabel(MilestoneSourceType.Book));
        }

        return new DailyNeed(
            0,
            DailyQuotaRules.FormatRequirement(MilestoneSourceType.Book, 0),
            DailyQuotaRules.GetPrimaryUnitLabel(MilestoneSourceType.Book));
    }

    private async Task<bool> AnyBookCompletedOnDayAsync(
        DateTime dayLocal,
        CancellationToken cancellationToken)
    {
        var startUtc = DateTimeHelper.ToUtcFromLocalDate(dayLocal);
        var endUtc = startUtc.AddDays(1);
        await using var db = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        return await db.Books.AnyAsync(
            b => b.CompletedAt != null && b.CompletedAt >= startUtc && b.CompletedAt < endUtc,
            cancellationToken);
    }

    private static async Task<Book?> FindCurrentReadingBookAsync(
        HobbyXpDbContext db,
        DateTime startUtc,
        DateTime endUtc,
        CancellationToken cancellationToken)
    {
        var inProgress = await db.Books
            .AsNoTracking()
            .Where(b => b.Status == BookStatus.Reading && b.CreatedAt < endUtc)
            .ToListAsync(cancellationToken);

        if (inProgress.Count == 0)
            return null;

        if (inProgress.Count == 1)
            return inProgress[0];

        var pagesByBook = await db.BookReadingLogs
            .AsNoTracking()
            .Where(l => l.ReadDate >= startUtc && l.ReadDate < endUtc)
            .GroupBy(l => l.BookId)
            .Select(g => new { BookId = g.Key, Pages = g.Sum(x => x.PagesDone) })
            .ToListAsync(cancellationToken);

        return inProgress
            .OrderByDescending(b => pagesByBook.FirstOrDefault(p => p.BookId == b.Id)?.Pages ?? 0)
            .ThenByDescending(b => b.UpdatedAt ?? b.CreatedAt)
            .First();
    }

    private async Task<bool> HasActiveCourseAsync(
        DateTime startUtc,
        DateTime endUtc,
        CancellationToken cancellationToken)
    {
        await using var db = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        var inProgress = await db.Courses.AnyAsync(
            c => c.Status == CourseStatus.InProgress && c.CreatedAt < endUtc,
            cancellationToken);
        if (inProgress)
            return true;

        return await db.CourseSessionLogs.AnyAsync(
            l => l.SessionDate >= startUtc && l.SessionDate < endUtc,
            cancellationToken);
    }

    private async Task<bool> HasSeriesObligationAsync(
        DateTime startUtc,
        DateTime endUtc,
        CancellationToken cancellationToken)
    {
        await using var db = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        var inProgress = await db.MediaSeries.AnyAsync(
            s => s.Status == MediaSeriesStatus.InProgress && s.CreatedAt < endUtc,
            cancellationToken);
        if (inProgress)
            return true;

        return await db.MediaEntries.AnyAsync(
            m => m.MediaType == MediaType.Series &&
                 m.CompletedAt >= startUtc &&
                 m.CompletedAt < endUtc,
            cancellationToken);
    }

    private readonly record struct QuotaNeed(
        int Primary,
        int Secondary,
        string Label,
        string PrimaryUnit,
        string SecondaryUnit);

    private readonly record struct DailyNeed(
        int Primary,
        string Label,
        string PrimaryUnit);

    private readonly record struct EvaluationTick(bool JustPenalized, bool JustRestored, string Message);
}
