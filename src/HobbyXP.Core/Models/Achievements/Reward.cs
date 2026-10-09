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

    /// <summary>Precio monetario de referencia (lo que cuesta comprarlo en la vida real).</summary>
    public decimal? Price { get; set; }

    /// <summary>Enlace de compra (tienda online, wishlist, etc.).</summary>
    public string? PurchaseUrl { get; set; }

    /// <summary>Ruta relativa de la imagen del premio bajo el directorio de datos.</summary>
    public string? ImagePath { get; set; }

    public RewardStatus Status { get; set; } = RewardStatus.Available;

    public DateTime? RedeemedAt { get; set; }

    /// <summary>XP canjeable realmente descontado al canjear (costo efectivo por nivel).</summary>
    public int? RedeemedCostInPoints { get; set; }

    /// <summary>Módulo al que pertenece el premio (Running, Gimnasio, …). Nulo = sin asignar (General).</summary>
    public MilestoneSourceType? SourceType { get; set; }

    public string StatusLabel => Status switch
    {
        RewardStatus.Available => "Disponible",
        RewardStatus.Redeemed => "Canjeado",
        _ => Status.ToString()
    };
}
