using HobbyXP.Models.Enums;

namespace HobbyXP.Helpers;

public static class ExerciseTypeLabels
{
    public static string Get(ExerciseType type) => type switch
    {
        ExerciseType.TraditionalWeight => "Peso tradicional",
        ExerciseType.BodyWeight => "Peso corporal",
        ExerciseType.TimeBased => "Por tiempo",
        _ => type.ToString()
    };
}
