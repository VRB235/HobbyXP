using HobbyXP.Models.Enums;

namespace HobbyXP.Helpers;

public static class RunningSessionTypeLabels
{
    public static string Get(RunningSessionType type) => type switch
    {
        RunningSessionType.Regenerativa => "Regenerativa",
        RunningSessionType.Umbral => "Umbral",
        RunningSessionType.TiradaLarga => "Tirada larga",
        _ => type.ToString()
    };

    public static string GetOrUnassigned(RunningSessionType? type) =>
        type is null ? "Sin tipo" : Get(type.Value);
}
