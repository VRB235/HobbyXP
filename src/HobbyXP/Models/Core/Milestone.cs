using HobbyXP.Models.Common;
using HobbyXP.Models.Enums;

namespace HobbyXP.Models.Core;

/// <summary>
/// Hitos recientes mostrados en el dashboard (sección inferior).
/// </summary>
public class Milestone : EntityBase
{
    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    public int PointsEarned { get; set; }

    public MilestoneSourceType SourceType { get; set; }

    public int? SourceEntityId { get; set; }

    public DateTime CompletedAt { get; set; } = DateTime.UtcNow;
}
