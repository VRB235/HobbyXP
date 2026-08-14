using HobbyXP.Data;
using HobbyXP.Helpers;
using HobbyXP.Models.Core;
using HobbyXP.Models.Enums;
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

        var earliestEvaluated = await db.WeeklyQuotaEvaluations
            .Select(e => (DateTime?)e.WeekStartUtc)
            .MinAsync(cancellationToken);

        var currentWeekStartUtc = WeekDateHelper.GetWeekStartUtc(DateTime.Today);
        profile.WeeklyQuotaTrackingStartedAtUtc = earliestEvaluated ?? currentWeekStartUtc;
        profile.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);

        return profile.WeeklyQuotaTrackingStartedAtUtc.Value.ToLocalTime().Date;
    }

    public async Task NotifyActivityAsync(
        MilestoneSourceType sourceType,
        DateTime activityLocalDate,
        CancellationToken cancellationToken = default)
    {
        if (!WeeklyQuotaRules.TrackedSources.Contains(sourceType))
            return;

        var weekStartLocal = WeekDateHelper.GetWeekStartLocal(activityLocalDate);
        var trackingStartLocal = await EnsureTrackingStartAsync(cancellationToken);
        if (weekStartLocal < trackingStartLocal)
            return; // fuera del periodo de disciplina

        var isClosed = WeekDateHelper.IsClosedWeek(weekStartLocal, DateTime.Today);
        await EvaluateWeekAsync(sourceType, weekStartLocal, applyPenaltyIfNeeded: isClosed, cancellationToken);
    }

    public async Task<IReadOnlyList<WeeklyQuotaProgress>> GetCurrentWeekProgressAsync(
        CancellationToken cancellationToken = default)
    {
        var weekStartLocal = WeekDateHelper.GetWeekStartLocal(DateTime.Today);
        var counts = await CountActivityAsync(weekStartLocal, cancellationToken);
        await using var db = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        var result = new List<WeeklyQuotaProgress>();
        foreach (var source in WeeklyQuotaRules.TrackedSources)
        {
            var (requiredPrimary, requiredSecondary) = WeeklyQuotaRules.GetRequired(source);
            var (actualPrimary, actualSecondary) = counts[source];
            var weekStartUtc = WeekDateHelper.GetWeekStartUtc(weekStartLocal);
            var lastClosed = await db.WeeklyQuotaEvaluations
                .AsNoTracking()
                .Where(e => e.SourceType == source && e.WeekStartUtc < weekStartUtc)
                .OrderByDescending(e => e.WeekStartUtc)
                .Select(e => (WeeklyQuotaStatus?)e.Status)
                .FirstOrDefaultAsync(cancellationToken);

            var activePenalties = await db.WeeklyQuotaEvaluations
                .AsNoTracking()
                .Where(e => e.SourceType == source &&
                            e.Status == WeeklyQuotaStatus.Penalized &&
                            e.HobbyXpRevoked > 0)
                .OrderByDescending(e => e.PenalizedAt ?? e.WeekStartUtc)
                .ToListAsync(cancellationToken);

            var reminder = activePenalties.Count == 0
                ? null
                : string.Join(Environment.NewLine, activePenalties.Select(WeeklyQuotaPenaltyMessages.FormatReminder));

            result.Add(new WeeklyQuotaProgress(
                source,
                HobbyProgressCatalog.GetDisplayName(source),
                WeeklyQuotaRules.FormatRequirement(source),
                requiredPrimary,
                actualPrimary,
                WeeklyQuotaRules.GetPrimaryUnitLabel(source),
                requiredSecondary,
                actualSecondary,
                WeeklyQuotaRules.GetSecondaryUnitLabel(source),
                WeeklyQuotaRules.IsMet(requiredPrimary, actualPrimary, requiredSecondary, actualSecondary),
                lastClosed,
                reminder));
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
        var penalties = await db.WeeklyQuotaEvaluations
            .AsNoTracking()
            .Where(e => e.SourceType == sourceType &&
                        e.Status == WeeklyQuotaStatus.Penalized &&
                        e.HobbyXpRevoked > 0)
            .OrderByDescending(e => e.PenalizedAt ?? e.WeekStartUtc)
            .ToListAsync(cancellationToken);

        return penalties.Select(WeeklyQuotaPenaltyMessages.FormatReminder).ToList();
    }

    private async Task<EvaluationTick?> EvaluateWeekAsync(
        MilestoneSourceType sourceType,
        DateTime weekStartLocal,
        bool applyPenaltyIfNeeded,
        CancellationToken cancellationToken)
    {
        var (requiredPrimary, requiredSecondary) = WeeklyQuotaRules.GetRequired(sourceType);
        if (requiredPrimary <= 0)
            return null;

        if (!await ShouldEvaluateSourceWeekAsync(sourceType, weekStartLocal, cancellationToken))
            return null;

        var weekStartUtc = DateTimeHelper.ToUtcFromLocalDate(weekStartLocal);
        var counts = await CountActivityForSourceAsync(sourceType, weekStartLocal, cancellationToken);
        var met = WeeklyQuotaRules.IsMet(requiredPrimary, counts.Primary, requiredSecondary, counts.Secondary);

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
            return await ApplyPenaltyAsync(evaluation, cancellationToken);
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
                return await RestorePenaltyAsync(evaluation, cancellationToken);
            }

            evaluation.Status = WeeklyQuotaStatus.Met;
            await db.SaveChangesAsync(cancellationToken);
            return null;
        }

        // No cumplida
        if (evaluation.Status is WeeklyQuotaStatus.Penalized or WeeklyQuotaStatus.SkippedFloor)
        {
            await db.SaveChangesAsync(cancellationToken);
            return null;
        }

        if (evaluation.Status == WeeklyQuotaStatus.Restored)
        {
            // Volvió a incumplir tras restauración (borró actividad): no re-castigar en v1.
            await db.SaveChangesAsync(cancellationToken);
            return null;
        }

        // Status Met (o nuevo) pero ya no cumple
        if (!applyPenaltyIfNeeded)
        {
            await db.SaveChangesAsync(cancellationToken);
            return null;
        }

        await db.SaveChangesAsync(cancellationToken);
        return await ApplyPenaltyAsync(evaluation, cancellationToken);
    }

    private async Task<EvaluationTick> ApplyPenaltyAsync(
        WeeklyQuotaEvaluation evaluation,
        CancellationToken cancellationToken)
    {
        var weekLabel = evaluation.WeekStartUtc.ToLocalTime().Date.ToString("dd/MM/yyyy");
        var description =
            $"Castigo semanal ({HobbyProgressCatalog.GetDisplayName(evaluation.SourceType)}) · semana del {weekLabel}";

        var outcome = await _xpService.ApplyHobbyLevelDownPenaltyAsync(
            evaluation.SourceType,
            description,
            evaluation.Id,
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

    private async Task<EvaluationTick> RestorePenaltyAsync(
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
                await CountBookActivityAsync(db, startUtc, endUtc, cancellationToken),
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

    private static async Task<int> CountSeriesActivityAsync(
        HobbyXpDbContext db,
        DateTime startUtc,
        DateTime endUtc,
        CancellationToken cancellationToken)
    {
        var chapters = await db.MediaSeriesChapterLogs
            .Where(l => l.WatchDate >= startUtc && l.WatchDate < endUtc)
            .SumAsync(l => l.ChaptersDone, cancellationToken);

        if (chapters > 0)
            return 1;

        var completedSeries = await db.MediaEntries.CountAsync(
            m => m.MediaType == MediaType.Series &&
                 m.CompletedAt >= startUtc &&
                 m.CompletedAt < endUtc,
            cancellationToken);

        return completedSeries > 0 ? 1 : 0;
    }

    private static async Task<int> CountBookActivityAsync(
        HobbyXpDbContext db,
        DateTime startUtc,
        DateTime endUtc,
        CancellationToken cancellationToken)
    {
        var pages = await db.BookReadingLogs
            .Where(l => l.ReadDate >= startUtc && l.ReadDate < endUtc)
            .SumAsync(l => l.PagesDone, cancellationToken);

        if (pages > 0)
            return 1;

        var completed = await db.Books.CountAsync(
            b => b.CompletedAt != null &&
                 b.CompletedAt >= startUtc &&
                 b.CompletedAt < endUtc,
            cancellationToken);

        return completed > 0 ? 1 : 0;
    }

    private readonly record struct EvaluationTick(bool JustPenalized, bool JustRestored, string Message);
}
