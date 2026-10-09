using HobbyXP.Models.Common;

namespace HobbyXP.Models.Achievements;

/// <summary>
/// Medalla desbloqueada por el jugador.
/// </summary>
public class EarnedMedal : EntityBase
{
    public int MedalDefinitionId { get; set; }

    public DateTime EarnedAt { get; set; } = DateTime.UtcNow;

    public string? SourceEntityType { get; set; }

    public int? SourceEntityId { get; set; }

    public MedalDefinition MedalDefinition { get; set; } = null!;
}
