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
    /// Saldo canjeable en la tienda de premios (independiente del XP de progresión/nivel).
    /// </summary>
    public int SpendableXp { get; set; }

    /// <summary>
    /// True cuando ya se separó el ledger (progresión vs saldo) y se aplicó el reset one-shot si aplicaba.
    /// </summary>
    public bool SpendableLedgerInitialized { get; set; }

    /// <summary>
    /// True cuando la progresión quedó en baseline (nivel 1 / 0 XP) tras el ledger de saldo.
    /// Evita reaplicar el wipe a awards legítimos posteriores; repara BDs donde el backfill
    /// histórico volvió a llenar hobbies tras el prestige.
    /// </summary>
    public bool SpendableProgressBaselineApplied { get; set; }

    /// <summary>
    /// XP del tramo 1→2. Cada nivel siguiente cuesta el doble (escala geométrica configurable).
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

    public ICollection<HobbyProgress> HobbyProgresses { get; set; } = [];
}
