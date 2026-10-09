using System.ComponentModel.DataAnnotations.Schema;
using HobbyXP.Helpers;
using HobbyXP.Models.Common;
using HobbyXP.Models.Enums;

namespace HobbyXP.Models.Entertainment;

/// <summary>
/// Videojuego con progreso 0-100%. Al llegar a 100% pasa a estado Platinum.
/// </summary>
public class VideoGame : EntityBase
{
    public string Title { get; set; } = string.Empty;

    public VideoGamePlatform Platform { get; set; }

    /// <summary>Etiqueta legible para UI.</summary>
    public string PlatformLabel => EntertainmentDisplayLabels.GetVideoGamePlatform(Platform);

    /// <summary>
    /// Porcentaje de completitud entre 0 y 100.
    /// </summary>
    public int CompletionPercentage { get; set; }

    public VideoGameStatus Status { get; set; } = VideoGameStatus.InProgress;

    public DateTime? StartedAt { get; set; }

    public DateTime? PlatinumUnlockedAt { get; set; }

    public int XpEarned { get; set; }

    public int PlatinumBonusXp { get; set; }

    /// <summary>Ruta relativa al directorio de datos (portada).</summary>
    public string? ImagePath { get; set; }

    public ICollection<VideoGameProgressLog> ProgressLogs { get; set; } = [];

    [NotMapped]
    public string HistoryDateLabel =>
        (PlatinumUnlockedAt ?? StartedAt)?.ToLocalTime().ToString("dd/MM/yyyy") ?? "—";

    [NotMapped]
    public string? ImageDisplayPath => HobbyCoverPhotoStorage.ResolveAbsolutePath(ImagePath);

    [NotMapped]
    public bool HasImage => !string.IsNullOrWhiteSpace(ImageDisplayPath);
}
