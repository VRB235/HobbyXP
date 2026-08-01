using HobbyXP.Models.Enums;

namespace HobbyXP.Helpers;

public static class MuscleGroupLabels
{
    public static string Get(MuscleGroup group) => group switch
    {
        MuscleGroup.Pecho => "Pecho",
        MuscleGroup.Triceps => "Tríceps",
        MuscleGroup.Biceps => "Bíceps",
        MuscleGroup.Hombros => "Hombros",
        MuscleGroup.Core => "Core",
        MuscleGroup.Espalda => "Espalda",
        MuscleGroup.Cuadriceps => "Cuádriceps",
        MuscleGroup.Gemelos => "Gemelos",
        MuscleGroup.Abductores => "Abductores",
        MuscleGroup.Aductores => "Aductores",
        MuscleGroup.Isquiotibiales => "Isquiotibiales",
        _ => group.ToString()
    };

    public static string GetOrUnassigned(MuscleGroup? group) =>
        group is null ? "Sin grupo" : Get(group.Value);
}
