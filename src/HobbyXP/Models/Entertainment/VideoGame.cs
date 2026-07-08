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

    /// <summary>
    /// Porcentaje de completitud entre 0 y 100.
    /// </summary>
    public int CompletionPercentage { get; set; }

    public VideoGameStatus Status { get; set; } = VideoGameStatus.InProgress;

    public DateTime? StartedAt { get; set; }

    public DateTime? PlatinumUnlockedAt { get; set; }

    public int XpEarned { get; set; }

    public int PlatinumBonusXp { get; set; }
}
