using System.ComponentModel.DataAnnotations.Schema;
using HobbyXP.Helpers;
using HobbyXP.Models.Common;
using HobbyXP.Models.Enums;

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

    /// <summary>
    /// Opcional en sesiones legacy; las nuevas deben indicarlo.
    /// </summary>
    public RunningSessionType? SessionType { get; set; }

    public DateTime RecordedAt { get; set; } = DateTime.UtcNow;

    public string? Notes { get; set; }

    public int XpEarned { get; set; }

    public OfficialRace? Carrera { get; set; }

    public ICollection<RunningSessionSeries> Series { get; set; } = new List<RunningSessionSeries>();

    [NotMapped]
    public string CarreraOficialNombre => Carrera?.Name ?? "(Sin carrera)";

    [NotMapped]
    public string SessionTypeLabel => RunningSessionTypeLabels.GetOrUnassigned(SessionType);

    /// <summary>Resumen de series para historial (p. ej. "5×1 km" o "4×800 m").</summary>
    [NotMapped]
    public string SeriesSummary
    {
        get
        {
            if (Series is null || Series.Count == 0)
                return "—";

            var ordered = Series.OrderBy(s => s.SortOrder).ToList();
            var first = ordered[0];
            var sameDistance = ordered.All(s => s.DistanceKm == first.DistanceKm);
            var sameDuration = ordered.All(s => s.Duration == first.Duration);

            if (sameDistance && sameDuration)
                return $"{ordered.Count}× {FormatSeriesDistance(first.DistanceKm)} · {first.Duration:mm\\:ss}";

            if (sameDistance)
                return $"{ordered.Count}× {FormatSeriesDistance(first.DistanceKm)}";

            return $"{ordered.Count} series";
        }
    }

    private static string FormatSeriesDistance(decimal distanceKm)
    {
        var meters = distanceKm * 1000m;
        if (meters == decimal.Truncate(meters) && meters is >= 1 and < 1000)
            return $"{meters:0} m";

        return $"{distanceKm:0.###} km";
    }
}
