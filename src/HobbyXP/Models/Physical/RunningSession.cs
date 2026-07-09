using System.ComponentModel.DataAnnotations.Schema;
using HobbyXP.Models.Common;

namespace HobbyXP.Models.Physical;

/// <summary>
/// Sesión de running. CarreraId es opcional (NULL en SQLite cuando no aplica).
/// </summary>
public class RunningSession : EntityBase
{
    public decimal DistanceKm { get; set; }

    /// <summary>
    /// Duración total de la sesión.
    /// </summary>
    public TimeSpan Duration { get; set; }

    /// <summary>
    /// Ritmo calculado en minutos por kilómetro (min/km).
    /// </summary>
    public double PaceMinPerKm { get; set; }

    /// <summary>
    /// Relación opcional con una carrera oficial de preparación.
    /// </summary>
    public int? CarreraId { get; set; }

    public DateTime RecordedAt { get; set; } = DateTime.UtcNow;

    public string? Notes { get; set; }

    public int XpEarned { get; set; }

    public OfficialRace? Carrera { get; set; }

    [NotMapped]
    public string CarreraOficialNombre => Carrera?.Name ?? "(Sin carrera)";
}
