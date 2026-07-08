using HobbyXP.Models.Common;
using HobbyXP.Models.Enums;

namespace HobbyXP.Models.Physical;

/// <summary>
/// Catálogo de ejercicios. El tipo determina columnas activas en la UI.
/// </summary>
public class Exercise : EntityBase
{
    public string Name { get; set; } = string.Empty;

    public ExerciseType ExerciseType { get; set; }

    public ICollection<GymWorkoutEntry> WorkoutEntries { get; set; } = [];
}
