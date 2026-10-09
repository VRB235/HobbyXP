using System.Globalization;
using HobbyXP.Models.Enums;
using HobbyXP.Models.Physical;

namespace HobbyXP.Helpers;

/// <summary>
/// Formatea y selecciona la última sesión de un tipo para precargar el alta de running.
/// </summary>
public static class RunningSessionPrefill
{
    public static RunningSession? FindLatest(
        IEnumerable<RunningSession> sessions,
        RunningSessionType type) =>
        sessions
            .Where(s => s.SessionType == type)
            .OrderByDescending(s => s.RecordedAt)
            .ThenByDescending(s => s.Id)
            .FirstOrDefault();

    public static string FormatDistanceKm(decimal distanceKm) =>
        distanceKm.ToString("0.###", CultureInfo.CurrentCulture);

    public static (int Minutes, int Seconds) SplitDuration(TimeSpan duration)
    {
        var minutes = (int)Math.Floor(duration.TotalMinutes);
        if (minutes < 0)
            minutes = 0;

        var seconds = duration.Seconds;
        if (seconds < 0)
            seconds = 0;

        return (minutes, seconds);
    }

    public static string FormatDurationClock(TimeSpan duration)
    {
        var (minutes, seconds) = SplitDuration(duration);
        return $"{minutes}:{seconds:00}";
    }

    /// <summary>
    /// Distancia de serie para el formulario: metros enteros si 1–999 m; si no, km.
    /// </summary>
    public static (string Text, bool IsMeters) FormatSeriesDistanceInput(decimal distanceKm)
    {
        var meters = distanceKm * 1000m;
        if (meters == decimal.Truncate(meters) && meters is >= 1 and < 1000)
            return (((int)meters).ToString(CultureInfo.CurrentCulture), true);

        return (FormatDistanceKm(distanceKm), false);
    }

    public static string FormatSeriesDistanceLabel(decimal distanceKm)
    {
        var (text, isMeters) = FormatSeriesDistanceInput(distanceKm);
        return isMeters ? $"{text} m" : $"{text} km";
    }

    public static string? TryFormatPaceMinPerKm(decimal distanceKm, TimeSpan duration)
    {
        if (distanceKm <= 0 || duration <= TimeSpan.Zero)
            return null;

        var pace = duration.TotalMinutes / (double)distanceKm;
        return $"{pace:0.00} min/km";
    }

    public static string BuildHint(RunningSession session)
    {
        var seriesNote = session.Series.Count > 0
            ? $" · Series: {session.SeriesSummary}"
            : string.Empty;

        return
            $"Última {session.SessionTypeLabel} ({session.RecordedAt:dd/MM/yyyy}): " +
            $"{session.DistanceKm:0.##} km · {FormatDurationClock(session.Duration)} · " +
            $"{session.PaceMinPerKm:0.00} min/km{seriesNote}. Puede editarlos antes de guardar.";
    }
}
