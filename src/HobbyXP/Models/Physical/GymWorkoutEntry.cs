using HobbyXP.Models.Common;
using HobbyXP.Models.Enums;

namespace HobbyXP.Models.Physical;

/// <summary>
/// Fila dinámica de entrenamiento. Campos nullable según ExerciseType:
/// - TraditionalWeight: Sets, Repetitions, WeightKg
/// - BodyWeight: Sets, Repetitions (WeightKg = null o 0)
/// - TimeBased: Sets, Duration (Repetitions y WeightKg = null)
/// </summary>
public class GymWorkoutEntry : EntityBase
{
    public int GymWorkoutId { get; set; }

    public int ExerciseId { get; set; }

    public ExerciseType ExerciseType { get; set; }

    public int Sets { get; set; }

    /// <summary>
    /// Repeticiones por serie. Null cuando el ejercicio es por tiempo.
    /// </summary>
    public int? Repetitions { get; set; }

    /// <summary>
    /// Peso en kg. Null para peso corporal y ejercicios por tiempo.
    /// </summary>
    public decimal? WeightKg { get; set; }

    /// <summary>
    /// Tiempo por serie (mm:ss). Null para peso tradicional y corporal.
    /// </summary>
    public TimeSpan? Duration { get; set; }

    public int SortOrder { get; set; }

    public bool IsPersonalRecord { get; set; }

    public GymWorkout GymWorkout { get; set; } = null!;

    public Exercise Exercise { get; set; } = null!;
}
