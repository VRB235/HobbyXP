namespace HobbyXP.Helpers;

/// <summary>
/// Costo de canje: el precio base se multiplica por el nivel global actual (mínimo ×1).
/// </summary>
public static class RewardCostCalculator
{
    public static int GetEffectiveCost(int baseCostInPoints, int currentLevel)
    {
        if (baseCostInPoints <= 0)
            return 0;

        var level = Math.Max(1, currentLevel);
        try
        {
            return checked(baseCostInPoints * level);
        }
        catch (OverflowException)
        {
            return int.MaxValue;
        }
    }
}
