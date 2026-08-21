using System.ComponentModel.DataAnnotations.Schema;
using HobbyXP.Helpers;
using HobbyXP.Models.Common;

namespace HobbyXP.Models.Physical;

/// <summary>
/// Carrera oficial registrada en catálogo. Puede completarse y desbloquear medalla de oro.
/// </summary>
public class OfficialRace : EntityBase
{
    public string Name { get; set; } = string.Empty;

    public decimal DistanceKm { get; set; }

    public DateTime? EventDate { get; set; }

    public string? Location { get; set; }

    public string? Description { get; set; }

    /// <summary>Ruta relativa al directorio de datos (copia gestionada por la app).</summary>
    public string? ImagePath { get; set; }

    public bool IsCompleted { get; set; }

    public DateTime? CompletedAt { get; set; }

    public int BonusXpAwarded { get; set; }

    public ICollection<RunningSession> TrainingSessions { get; set; } = [];

    [NotMapped]
    public string EventDateLabel => EventDate?.ToString("dd/MM/yyyy") ?? "—";

    [NotMapped]
    public string StatusLabel => IsCompleted ? "Completada" : "Pendiente";

    [NotMapped]
    public string LocationLabel => string.IsNullOrWhiteSpace(Location) ? "—" : Location;

    [NotMapped]
    public string DistanceLabel => $"{DistanceKm:0.##} km";

    [NotMapped]
    public string? ImageDisplayPath => RacePhotoStorage.ResolveAbsolutePath(ImagePath);

    [NotMapped]
    public bool HasImage => !string.IsNullOrWhiteSpace(ImageDisplayPath);
}
