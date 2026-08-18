using HobbyXP.Models.Common;
using HobbyXP.Models.Enums;

namespace HobbyXP.Models.Achievements;

/// <summary>
/// Premio del mundo real autogestionado, canjeable con XP.
/// </summary>
public class Reward : EntityBase
{
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public int CostInPoints { get; set; }

    public RewardStatus Status { get; set; } = RewardStatus.Available;

    public DateTime? RedeemedAt { get; set; }

    /// <summary>XP canjeable realmente descontado al canjear (costo efectivo por nivel).</summary>
    public int? RedeemedCostInPoints { get; set; }

    public string StatusLabel => Status switch
    {
        RewardStatus.Available => "Disponible",
        RewardStatus.Redeemed => "Canjeado",
        _ => Status.ToString()
    };
}
