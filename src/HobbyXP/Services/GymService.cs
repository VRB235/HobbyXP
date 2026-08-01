using HobbyXP.Data;
using HobbyXP.Models.Enums;
using HobbyXP.Models.Physical;
using HobbyXP.Services.Abstractions;
using HobbyXP.Services.Results;
using Microsoft.EntityFrameworkCore;

namespace HobbyXP.Services;

public sealed class GymService : IGymService
{
    private readonly IDbContextFactory<HobbyXpDbContext> _dbContextFactory;
    private readonly IXpService _xpService;
    private readonly IAchievementEngineService _achievementEngine;

    public GymService(
        IDbContextFactory<HobbyXpDbContext> dbContextFactory,
        IXpService xpService,
        IAchievementEngineService achievementEngine)
    {
        _dbContextFactory = dbContextFactory;
        _xpService = xpService;
        _achievementEngine = achievementEngine;
    }

    public async Task<IReadOnlyList<Exercise>> GetExercisesAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        return await db.Exercises
            .AsNoTracking()
            .OrderBy(e => e.MuscleGroup == null)
            .ThenBy(e => e.MuscleGroup)
            .ThenBy(e => e.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<Exercise> CreateOrGetExerciseAsync(
        string name,
        ExerciseType exerciseType,
        MuscleGroup? muscleGroup = null,
        CancellationToken cancellationToken = default)
    {
        var normalized = name.Trim();
        if (string.IsNullOrWhiteSpace(normalized))
            throw new ArgumentException("El nombre del ejercicio es obligatorio.", nameof(name));

        await using var db = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        var existing = await db.Exercises
            .FirstOrDefaultAsync(e => e.Name == normalized, cancellationToken);

        if (existing is not null)
        {
            // Completa grupo en ejercicios legacy cuando el usuario lo aporta al recrear por nombre.
            if (existing.MuscleGroup is null && muscleGroup is not null)
            {
                existing.MuscleGroup = muscleGroup;
                await db.SaveChangesAsync(cancellationToken);
            }

            return existing;
        }

        var exercise = new Exercise
        {
            Name = normalized,
            ExerciseType = exerciseType,
            MuscleGroup = muscleGroup
        };

        db.Exercises.Add(exercise);
        await db.SaveChangesAsync(cancellationToken);
        return exercise;
    }

    public async Task<Exercise?> UpdateExerciseMuscleGroupAsync(
        int exerciseId,
        MuscleGroup? muscleGroup,
        CancellationToken cancellationToken = default)
    {
        await using var db = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        var exercise = await db.Exercises.FirstOrDefaultAsync(e => e.Id == exerciseId, cancellationToken);
        if (exercise is null)
            return null;

        exercise.MuscleGroup = muscleGroup;
        await db.SaveChangesAsync(cancellationToken);
        return exercise;
    }

    public async Task<Exercise?> UpdateExerciseNameAsync(
        int exerciseId,
        string name,
        CancellationToken cancellationToken = default)
    {
        var normalized = name.Trim();
        if (string.IsNullOrWhiteSpace(normalized))
            throw new ArgumentException("El nombre del ejercicio es obligatorio.", nameof(name));
        if (normalized.Length > 150)
            throw new ArgumentException("El nombre del ejercicio no puede superar 150 caracteres.", nameof(name));

        await using var db = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        var exercise = await db.Exercises.FirstOrDefaultAsync(e => e.Id == exerciseId, cancellationToken);
        if (exercise is null)
            return null;

        if (string.Equals(exercise.Name, normalized, StringComparison.Ordinal))
            return exercise;

        var nameTaken = await db.Exercises
            .AnyAsync(e => e.Id != exerciseId && e.Name == normalized, cancellationToken);
        if (nameTaken)
            throw new InvalidOperationException($"Ya existe un ejercicio llamado '{normalized}'.");

        exercise.Name = normalized;
        await db.SaveChangesAsync(cancellationToken);
        return exercise;
    }

    public async Task<OperationResult<GymWorkout>> SaveWorkoutAsync(
        IReadOnlyList<GymWorkoutEntryDraft> entries,
        string? notes = null,
        CancellationToken cancellationToken = default)
    {
        if (entries.Count == 0)
            throw new ArgumentException("Debe registrar al menos un ejercicio.", nameof(entries));

        await using var db = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        var workout = new GymWorkout
        {
            WorkoutDate = DateTime.UtcNow,
            Notes = notes
        };

        var progressiveOverloadDetected = false;

        foreach (var draft in entries.OrderBy(e => e.SortOrder))
        {
            ValidateDraft(draft);

            var exercise = await db.Exercises.FindAsync([draft.ExerciseId], cancellationToken)
                ?? throw new InvalidOperationException($"No existe el ejercicio con Id {draft.ExerciseId}.");

            var isRecord = await IsPersonalRecordAsync(db, draft, cancellationToken);
            if (isRecord)
                progressiveOverloadDetected = true;

            workout.Entries.Add(new GymWorkoutEntry
            {
                ExerciseId = draft.ExerciseId,
                ExerciseType = draft.ExerciseType,
                Sets = draft.Sets,
                Repetitions = draft.Repetitions,
                WeightKg = draft.WeightKg,
                Duration = draft.Duration,
                SortOrder = draft.SortOrder,
                IsPersonalRecord = isRecord
            });
        }

        workout.TriggeredProgressiveOverload = progressiveOverloadDetected;
        db.GymWorkouts.Add(workout);
        await db.SaveChangesAsync(cancellationToken);

        var events = new List<AchievementEvent>();
        var sessionXp = await _xpService.AwardXpAsync(
            AchievementActionType.GymWorkoutSaved,
            units: 1,
            "Sesión de gimnasio guardada",
            MilestoneSourceType.Gym,
            nameof(GymWorkout),
            workout.Id,
            "Sesión de gimnasio",
            cancellationToken);

        workout.XpEarned = sessionXp.AmountAwarded;
        await db.SaveChangesAsync(cancellationToken);

        if (sessionXp.Milestone is not null)
        {
            events.Add(new AchievementEvent(
                sessionXp.Milestone.Title,
                sessionXp.Milestone.Description ?? sessionXp.Milestone.Title,
                sessionXp.AmountAwarded,
                MilestoneSourceType.Gym));
        }

        if (progressiveOverloadDetected)
        {
            var overloadXp = await _xpService.AwardFlatBonusAsync(
                AchievementActionType.ProgressiveOverload,
                await _xpService.CalculatePointsAsync(AchievementActionType.ProgressiveOverload, 1, cancellationToken),
                "Sobrecarga progresiva detectada",
                MilestoneSourceType.Gym,
                nameof(GymWorkout),
                workout.Id,
                "¡Sobrecarga progresiva!",
                cancellationToken);

            workout.XpEarned += overloadXp.AmountAwarded;
            await db.SaveChangesAsync(cancellationToken);

            events.Add(new AchievementEvent(
                "¡Sobrecarga progresiva!",
                "Superaste tu récord histórico en al menos un ejercicio.",
                overloadXp.AmountAwarded,
                MilestoneSourceType.Gym,
                RequiresCelebration: true));

            events.AddRange(await _achievementEngine.TryAwardMilestonesForTrackAsync(
                MedalMilestoneTrack.ProgressiveOverloadPrs,
                MilestoneSourceType.Gym,
                nameof(GymWorkout),
                workout.Id,
                cancellationToken));
        }

        events.AddRange(await _achievementEngine.TryAwardMilestonesForTrackAsync(
            MedalMilestoneTrack.GymWorkouts,
            MilestoneSourceType.Gym,
            nameof(GymWorkout),
            workout.Id,
            cancellationToken));

        return OperationResult<GymWorkout>.WithEvents(workout, events.ToArray());
    }

    public async Task<IReadOnlyList<GymWorkout>> GetWorkoutHistoryAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        return await db.GymWorkouts
            .AsNoTracking()
            .Include(w => w.Entries)
                .ThenInclude(e => e.Exercise)
            .OrderByDescending(w => w.WorkoutDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> DeleteWorkoutAsync(int workoutId, CancellationToken cancellationToken = default)
    {
        await using var db = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        var workout = await db.GymWorkouts.FindAsync([workoutId], cancellationToken);
        if (workout is null)
            return false;

        await _xpService.RevokeXpForSourceAsync(
            MilestoneSourceType.Gym,
            nameof(GymWorkout),
            workoutId,
            $"Eliminado del historial: entrenamiento del {workout.WorkoutDate:dd/MM/yyyy}",
            cancellationToken);

        db.GymWorkouts.Remove(workout);
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }

    private static void ValidateDraft(GymWorkoutEntryDraft draft)
    {
        if (draft.Sets <= 0)
            throw new ArgumentOutOfRangeException(nameof(draft.Sets), "Las series deben ser mayores que cero.");

        switch (draft.ExerciseType)
        {
            case ExerciseType.TraditionalWeight:
                if (!draft.Repetitions.HasValue || draft.Repetitions <= 0)
                    throw new ArgumentException("Las repeticiones son obligatorias para peso tradicional.");
                if (!draft.WeightKg.HasValue || draft.WeightKg <= 0)
                    throw new ArgumentException("El peso es obligatorio para ejercicios de peso tradicional.");
                break;

            case ExerciseType.BodyWeight:
                if (!draft.Repetitions.HasValue || draft.Repetitions <= 0)
                    throw new ArgumentException("Las repeticiones son obligatorias para peso corporal.");
                break;

            case ExerciseType.TimeBased:
                if (!draft.Duration.HasValue || draft.Duration <= TimeSpan.Zero)
                    throw new ArgumentException("La duración es obligatoria para ejercicios por tiempo.");
                break;
        }
    }

    private static async Task<bool> IsPersonalRecordAsync(
        HobbyXpDbContext db,
        GymWorkoutEntryDraft draft,
        CancellationToken cancellationToken)
    {
        var history = await db.GymWorkoutEntries
            .AsNoTracking()
            .Where(e => e.ExerciseId == draft.ExerciseId)
            .ToListAsync(cancellationToken);

        if (history.Count == 0)
            return true;

        return draft.ExerciseType switch
        {
            ExerciseType.TraditionalWeight => draft.WeightKg > history
                .Where(e => e.WeightKg.HasValue)
                .Select(e => e.WeightKg)
                .DefaultIfEmpty(0m)
                .Max(),

            ExerciseType.BodyWeight => draft.Repetitions > history
                .Where(e => e.Repetitions.HasValue)
                .Select(e => e.Repetitions)
                .DefaultIfEmpty(0)
                .Max(),

            ExerciseType.TimeBased => draft.Duration < history
                .Where(e => e.Duration.HasValue)
                .Select(e => e.Duration!.Value)
                .DefaultIfEmpty(TimeSpan.MaxValue)
                .Min(),

            _ => false
        };
    }
}
