namespace HobbyXP.Models.Physical;

/// <summary>
/// Borrador de serie/intervalo al guardar una sesión de running.
/// </summary>
public sealed record RunningSeriesDraft(int SortOrder, decimal DistanceKm, TimeSpan Duration);
