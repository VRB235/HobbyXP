namespace HobbyXP.Helpers;

/// <summary>
/// Beneficios al desbloquear una medalla: saldo, título e inmunidad de disciplina.
/// </summary>
public static class MedalPrivilegeRules
{
    public const int ImmunityDays = 7;

    /// <summary>
    /// Bonus de saldo al desbloquear: escala con el umbral de XP del hobby, acotado.
    /// </summary>
    public static int GetSpendableBonus(int xpThreshold) =>
        Math.Clamp(xpThreshold / 20, 50, 500);

    public static DateTime ExtendImmunity(DateTime utcNow, DateTime? currentUntilUtc)
    {
        var proposed = utcNow.AddDays(ImmunityDays);
        if (currentUntilUtc is DateTime existing && existing > proposed)
            return existing;

        return proposed;
    }

    public static bool IsActive(DateTime? untilUtc, DateTime utcNow) =>
        untilUtc is DateTime until && until > utcNow;

    public static string FormatSummary(int spendableBonus) =>
        $"+{spendableBonus:N0} XP canjeable · título de honor · inmunidad {ImmunityDays} días";
}
