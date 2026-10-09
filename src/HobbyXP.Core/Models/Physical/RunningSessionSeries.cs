using HobbyXP.Models.Common;

namespace HobbyXP.Models.Physical;

/// <summary>
/// Serie/intervalo de una sesión de running (p. ej. umbral: 5×1000 m).
/// </summary>
public class RunningSessionSeries : EntityBase
{
    public int RunningSessionId { get; set; }

    /// <summary>Orden 1-based de la serie dentro de la sesión.</summary>
    public int SortOrder { get; set; }

    /// <summary>Distancia de la serie en kilómetros (metros se convierten en UI).</summary>
    public decimal DistanceKm { get; set; }

    /// <summary>Tiempo de la serie.</summary>
    public TimeSpan Duration { get; set; }

    public RunningSession RunningSession { get; set; } = null!;
}
