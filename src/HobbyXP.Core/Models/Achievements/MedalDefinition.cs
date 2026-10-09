using HobbyXP.Models.Common;
using HobbyXP.Models.Enums;

namespace HobbyXP.Models.Achievements;

/// <summary>
/// Medalla definida por el usuario: se gana al alcanzar un umbral de XP en un hobby.
/// </summary>
public class MedalDefinition : EntityBase
{
    /// <summary>
    /// Identificador estable único (slug generado al crear).
    /// </summary>
    public string Code { get; set; } = string.Empty;

    public MilestoneSourceType SourceType { get; set; }

    /// <summary>
    /// XP de progresión del hobby requerido para desbloquearla.
    /// </summary>
    public int XpThreshold { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Pista mostrada en tooltip cuando la medalla está bloqueada.
    /// </summary>
    public string UnlockHint { get; set; } = string.Empty;

    public string? IconPath { get; set; }

    public ICollection<EarnedMedal> EarnedInstances { get; set; } = [];
}
