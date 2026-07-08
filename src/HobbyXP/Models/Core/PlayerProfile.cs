using HobbyXP.Models.Common;

namespace HobbyXP.Models.Core;

/// <summary>
/// Perfil único del jugador. Persiste nivel y XP total para el dashboard RPG.
/// </summary>
public class PlayerProfile : EntityBase
{
    public int CurrentLevel { get; set; } = 1;

    public int TotalXp { get; set; }

    /// <summary>
    /// XP base requerida para alcanzar el siguiente nivel (escala configurable).
    /// </summary>
    public int BaseXpPerLevel { get; set; } = 1000;

    /// <summary>
    /// Nombre visible del aventurero en sidebar y dashboard.
    /// </summary>
    public string DisplayName { get; set; } = "Aventurero";

    /// <summary>
    /// Ruta local opcional a la imagen de avatar.
    /// </summary>
    public string? AvatarPath { get; set; }

    public ICollection<XpTransaction> Transactions { get; set; } = [];
}
