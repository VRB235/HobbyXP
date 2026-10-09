using HobbyXP.Models.Common;
using HobbyXP.Models.Enums;

namespace HobbyXP.Models.Achievements;

/// <summary>
/// Regla editable del motor de logros (ej. 1 km = 10 pts).
/// </summary>
public class AchievementRule : EntityBase
{
    public AchievementActionType ActionType { get; set; }

    public string DisplayName { get; set; } = string.Empty;

    public string UnitLabel { get; set; } = string.Empty;

    /// <summary>
    /// Puntos otorgados por unidad (km, página, porcentaje, etc.).
    /// </summary>
    public decimal PointsPerUnit { get; set; }

    /// <summary>
    /// Bono fijo adicional al completar la acción (opcional).
    /// </summary>
    public int? FlatBonusPoints { get; set; }

    public bool IsActive { get; set; } = true;
}
