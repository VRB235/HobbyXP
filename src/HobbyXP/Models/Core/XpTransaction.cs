using HobbyXP.Models.Common;
using HobbyXP.Models.Enums;

namespace HobbyXP.Models.Core;

/// <summary>
/// Libro mayor de XP para auditoría y gráficos diarios.
/// </summary>
public class XpTransaction : EntityBase
{
    public int PlayerProfileId { get; set; }

    public int Amount { get; set; }

    public AchievementActionType ActionType { get; set; }

    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Tipo de entidad origen (ej. RunningSession, VideoGame).
    /// </summary>
    public string? SourceEntityType { get; set; }

    public int? SourceEntityId { get; set; }

    public DateTime EarnedAt { get; set; } = DateTime.UtcNow;

    public PlayerProfile PlayerProfile { get; set; } = null!;
}
