using HobbyXP.Models.Enums;
using HobbyXP.Models.Physical;
using HobbyXP.Services.Results;

namespace HobbyXP.Services.Abstractions;

public interface IGymService
{
    Task<IReadOnlyList<Exercise>> GetExercisesAsync(CancellationToken cancellationToken = default);

    Task<Exercise> CreateOrGetExerciseAsync(
        string name,
        ExerciseType exerciseType,
        MuscleGroup? muscleGroup = null,
        CancellationToken cancellationToken = default);

    Task<Exercise?> UpdateExerciseMuscleGroupAsync(
        int exerciseId,
        MuscleGroup? muscleGroup,
        CancellationToken cancellationToken = default);

    Task<Exercise?> UpdateExerciseNameAsync(
        int exerciseId,
        string name,
        CancellationToken cancellationToken = default);

    Task<OperationResult<GymWorkout>> SaveWorkoutAsync(
        IReadOnlyList<GymWorkoutEntryDraft> entries,
        string? notes = null,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<GymWorkout>> GetWorkoutHistoryAsync(CancellationToken cancellationToken = default);

    Task<bool> DeleteWorkoutAsync(int workoutId, CancellationToken cancellationToken = default);
}

/// <summary>
/// Borrador de fila de gimnasio antes de persistir y validar récords.
/// </summary>
public sealed record GymWorkoutEntryDraft(
    int ExerciseId,
    ExerciseType ExerciseType,
    int Sets,
    int? Repetitions,
    decimal? WeightKg,
    TimeSpan? Duration,
    int SortOrder);
