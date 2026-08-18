namespace HobbyXP.Helpers;

/// <summary>
/// Beneficios al desbloquear una medalla: saldo, título e inmunidad de disciplina.
/// </summary>
public static class MedalPrivilegeRules
{
    public const int ImmunityDays = 7;

    public static int GetSpendableBonus(int threshold) =>
        Math.Max(50, threshold * 10);

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
