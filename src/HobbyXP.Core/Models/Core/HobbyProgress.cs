using HobbyXP.Models.Common;
using HobbyXP.Models.Enums;

namespace HobbyXP.Models.Core;

/// <summary>
/// Progreso RPG independiente por hobby (pool de XP separado del global).
/// </summary>
public class HobbyProgress : EntityBase
{
    public int PlayerProfileId { get; set; }

    public MilestoneSourceType SourceType { get; set; }

    public int CurrentLevel { get; set; } = 1;

    public int TotalXp { get; set; }

    /// <summary>
    /// Saldo canjeable ganado en este módulo (solo válido para premios del mismo hobby).
    /// </summary>
    public int SpendableXp { get; set; }

    public PlayerProfile PlayerProfile { get; set; } = null!;
}
