using HobbyXP.Models.Core;
using HobbyXP.Services.Results;

namespace HobbyXP.Services.Internal;

/// <summary>
/// Escala geométrica: el tramo de nivel L cuesta BaseXpPerLevel × 2^(L−1).
/// Umbral acumulado para estar en nivel N: BaseXpPerLevel × (2^(N−1) − 1).
/// </summary>
internal static class XpLevelCalculator
{
    /// <summary>
    /// XP total mínima para alcanzar el <paramref name="level"/> indicado.
    /// </summary>
    public static long GetXpThresholdForLevel(int level, int baseXpPerLevel)
    {
        if (level <= 1 || baseXpPerLevel <= 0)
            return 0;

        var exponent = level - 1;
        if (exponent >= 63)
            return long.MaxValue;

        var factor = (1L << exponent) - 1;
        try
        {
            return checked(baseXpPerLevel * factor);
        }
        catch (OverflowException)
        {
            return long.MaxValue;
        }
    }

    /// <summary>
    /// XP necesaria para pasar del <paramref name="level"/> actual al siguiente.
    /// </summary>
    public static int GetXpRequiredForLevel(int level, int baseXpPerLevel)
    {
        if (baseXpPerLevel <= 0)
            return 1;

        var safeLevel = Math.Max(1, level);
        var exponent = safeLevel - 1;
        if (exponent >= 31)
            return int.MaxValue;

        try
        {
            var cost = checked(baseXpPerLevel * (1L << exponent));
            return cost > int.MaxValue ? int.MaxValue : (int)cost;
        }
        catch (OverflowException)
        {
            return int.MaxValue;
        }
    }

    public static LevelProgressInfo BuildProgress(PlayerProfile profile) =>
        BuildProgress(profile.CurrentLevel, profile.TotalXp, profile.BaseXpPerLevel);

    public static LevelProgressInfo BuildProgress(HobbyProgress hobby, int baseXpPerLevel) =>
        BuildProgress(hobby.CurrentLevel, hobby.TotalXp, baseXpPerLevel);

    public static LevelProgressInfo BuildProgress(int currentLevel, int totalXp, int baseXpPerLevel)
    {
        var threshold = GetXpThresholdForLevel(currentLevel, baseXpPerLevel);
        var xpIntoLevel = threshold >= int.MaxValue
            ? 0
            : Math.Max(0, totalXp - (int)threshold);
        var xpRequired = Math.Max(1, GetXpRequiredForLevel(currentLevel, baseXpPerLevel));
        var percentage = Math.Clamp(xpIntoLevel * 100d / xpRequired, 0d, 100d);

        return new LevelProgressInfo(
            currentLevel,
            totalXp,
            xpIntoLevel,
            xpRequired,
            percentage);
    }

    public static void RecalculateLevel(PlayerProfile profile)
    {
        var level = profile.CurrentLevel;
        RecalculateLevel(ref level, profile.TotalXp, profile.BaseXpPerLevel);
        profile.CurrentLevel = level;
    }

    public static void RecalculateLevel(HobbyProgress hobby, int baseXpPerLevel)
    {
        var level = hobby.CurrentLevel;
        RecalculateLevel(ref level, hobby.TotalXp, baseXpPerLevel);
        hobby.CurrentLevel = level;
    }

    public static void RecalculateLevel(ref int currentLevel, int totalXp, int baseXpPerLevel)
    {
        if (currentLevel < 1)
            currentLevel = 1;

        while (currentLevel > 1 &&
               totalXp < GetXpThresholdForLevel(currentLevel, baseXpPerLevel))
        {
            currentLevel--;
        }

        while (true)
        {
            var nextThreshold = GetXpThresholdForLevel(currentLevel + 1, baseXpPerLevel);
            if (nextThreshold > totalXp || nextThreshold > int.MaxValue)
                break;

            currentLevel++;
        }
    }
}
