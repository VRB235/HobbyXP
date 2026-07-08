using HobbyXP.Models.Common;
using HobbyXP.Models.Enums;

namespace HobbyXP.Models.Achievements;

/// <summary>
/// Catálogo maestro de medallas/trofeos del sistema.
/// </summary>
public class MedalDefinition : EntityBase
{
    public MedalCode Code { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Pista mostrada en tooltip cuando la medalla está bloqueada.
    /// </summary>
    public string UnlockHint { get; set; } = string.Empty;

    public string? IconPath { get; set; }

    public ICollection<EarnedMedal> EarnedInstances { get; set; } = [];
}
