using System.ComponentModel.DataAnnotations.Schema;
using HobbyXP.Helpers;
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

    /// <summary>
    /// Opcional: ejercicios legacy pueden no tener grupo hasta asignarlo.
    /// </summary>
    public MuscleGroup? MuscleGroup { get; set; }

    public ICollection<GymWorkoutEntry> WorkoutEntries { get; set; } = [];

    [NotMapped]
    public string MuscleGroupLabel => MuscleGroupLabels.GetOrUnassigned(MuscleGroup);

    [NotMapped]
    public string ExerciseTypeLabel => ExerciseTypeLabels.Get(ExerciseType);

    [NotMapped]
    public int MuscleGroupSortOrder => MuscleGroup is null ? int.MaxValue : (int)MuscleGroup.Value;

    /// <summary>
    /// Texto del ComboBox de entrenamiento (grupo · nombre).
    /// </summary>
    [NotMapped]
    public string PickerDisplayName => MuscleGroup is null
        ? Name
        : $"{MuscleGroupLabels.Get(MuscleGroup.Value)} · {Name}";
}
