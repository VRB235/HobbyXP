using HobbyXP.Data;
using HobbyXP.Helpers;
using HobbyXP.Models.Enums;
using HobbyXP.Models.Physical;
using HobbyXP.Services.Abstractions;
using HobbyXP.Services.Results;
using Microsoft.EntityFrameworkCore;

namespace HobbyXP.Services;

public sealed class DietService : IDietService
{
    private readonly IDbContextFactory<HobbyXpDbContext> _dbContextFactory;
    private readonly IXpService _xpService;
    private readonly IAchievementEngineService _achievementEngine;
    private readonly IWeeklyQuotaService _weeklyQuotaService;

    public DietService(
        IDbContextFactory<HobbyXpDbContext> dbContextFactory,
        IXpService xpService,
        IAchievementEngineService achievementEngine,
        IWeeklyQuotaService weeklyQuotaService)
    {
        _dbContextFactory = dbContextFactory;
        _xpService = xpService;
        _achievementEngine = achievementEngine;
        _weeklyQuotaService = weeklyQuotaService;
    }

    public async Task<DietDayLog?> GetByLocalDateAsync(
        DateTime localDate,
        CancellationToken cancellationToken = default)
    {
        var dayUtc = DateTimeHelper.ToUtcFromLocalDate(localDate);
        await using var db = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        return await db.DietDayLogs
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.DayDate == dayUtc, cancellationToken);
    }

    public async Task<IReadOnlyList<DietDayLog>> GetHistoryAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        return await db.DietDayLogs
            .AsNoTracking()
            .OrderByDescending(d => d.DayDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<OperationResult<DietDayLog>> SaveDayAsync(
        DietDayDraft draft,
        CancellationToken cancellationToken = default)
    {
        if (!DietDayRules.HasAnyLoggedMeal(draft.Breakfast, draft.Lunch, draft.Dinner, draft.Snack))
            throw new ArgumentException("Marque al menos una comida del día.", nameof(draft));

        await using var db = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        var dayUtc = DateTimeHelper.ToUtcFromLocalDate(draft.LocalDate);

        var log = await db.DietDayLogs.FirstOrDefaultAsync(d => d.DayDate == dayUtc, cancellationToken);
        var isNew = log is null;
        if (log is null)
        {
            log = new DietDayLog { DayDate = dayUtc };
            db.DietDayLogs.Add(log);
        }

        log.BreakfastStatus = draft.Breakfast;
        log.LunchStatus = draft.Lunch;
        log.DinnerStatus = draft.Dinner;
        log.SnackStatus = draft.Snack;
        log.Notes = string.IsNullOrWhiteSpace(draft.Notes) ? null : draft.Notes.Trim();
        log.RecalculateScore();
        if (!isNew)
            log.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(cancellationToken);

        if (!isNew)
        {
            await _xpService.RevokeXpForSourceAsync(
                MilestoneSourceType.Diet,
                nameof(DietDayLog),
                log.Id,
                $"Ajuste de dieta del {draft.LocalDate:dd/MM/yyyy}",
                cancellationToken);
        }

        var events = new List<AchievementEvent>();
        var xpEarned = 0;

        if (log.OnPlanCount > 0)
        {
            var mealXp = await _xpService.AwardXpAsync(
                AchievementActionType.DietMealOnPlan,
                log.OnPlanCount,
                $"{log.OnPlanCount} comida(s) en plan · {draft.LocalDate:dd/MM/yyyy}",
                MilestoneSourceType.Diet,
                nameof(DietDayLog),
                log.Id,
                "Comidas en plan",
                cancellationToken);

            xpEarned += mealXp.AmountAwarded;
            if (mealXp.Milestone is not null)
            {
                events.Add(new AchievementEvent(
                    mealXp.Milestone.Title,
                    mealXp.Milestone.Description ?? mealXp.Milestone.Title,
                    mealXp.AmountAwarded,
                    MilestoneSourceType.Diet));
            }
        }

        if (DietDayRules.IsPerfectDay(log))
        {
            var perfectXp = await _xpService.AwardFlatBonusAsync(
                AchievementActionType.DietPerfectDay,
                await _xpService.CalculatePointsAsync(AchievementActionType.DietPerfectDay, 1, cancellationToken),
                $"Día perfecto de dieta · {draft.LocalDate:dd/MM/yyyy}",
                MilestoneSourceType.Diet,
                nameof(DietDayLog),
                log.Id,
                "¡Día perfecto!",
                cancellationToken);

            xpEarned += perfectXp.AmountAwarded;
            events.Add(new AchievementEvent(
                "¡Día perfecto!",
                "Las 4 comidas quedaron en plan.",
                perfectXp.AmountAwarded,
                MilestoneSourceType.Diet,
                RequiresCelebration: true));

            events.AddRange(await _achievementEngine.TryAwardMilestonesForTrackAsync(
                MedalMilestoneTrack.DietPerfectDays,
                MilestoneSourceType.Diet,
                nameof(DietDayLog),
                log.Id,
                cancellationToken));
        }

        if (DietDayRules.IsGoodDay(log))
        {
            events.AddRange(await _achievementEngine.TryAwardMilestonesForTrackAsync(
                MedalMilestoneTrack.DietGoodDays,
                MilestoneSourceType.Diet,
                nameof(DietDayLog),
                log.Id,
                cancellationToken));
        }

        log.XpEarned = xpEarned;
        log.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);

        await _weeklyQuotaService.NotifyActivityAsync(
            MilestoneSourceType.Diet,
            draft.LocalDate.Date,
            cancellationToken);

        return OperationResult<DietDayLog>.WithEvents(log, events.ToArray());
    }

    public async Task<bool> DeleteDayAsync(int dietDayLogId, CancellationToken cancellationToken = default)
    {
        await using var db = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        var log = await db.DietDayLogs.FindAsync([dietDayLogId], cancellationToken);
        if (log is null)
            return false;

        await _xpService.RevokeXpForSourceAsync(
            MilestoneSourceType.Diet,
            nameof(DietDayLog),
            dietDayLogId,
            $"Eliminado del historial: dieta del {log.DayDate:dd/MM/yyyy}",
            cancellationToken);

        db.DietDayLogs.Remove(log);
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }
}
