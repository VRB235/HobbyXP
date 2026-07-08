using HobbyXP.Models.Core;
using HobbyXP.Services.Results;

namespace HobbyXP.Services.Internal;

internal static class XpLevelCalculator
{
    public static int GetXpThresholdForLevel(int level, int baseXpPerLevel) =>
        Math.Max(0, (level - 1) * baseXpPerLevel);

    public static LevelProgressInfo BuildProgress(PlayerProfile profile)
    {
        var xpIntoLevel = profile.TotalXp - GetXpThresholdForLevel(profile.CurrentLevel, profile.BaseXpPerLevel);
        var xpRequired = Math.Max(1, profile.BaseXpPerLevel);
        var percentage = xpRequired == 0
            ? 100d
            : Math.Clamp(xpIntoLevel * 100d / xpRequired, 0d, 100d);

        return new LevelProgressInfo(
            profile.CurrentLevel,
            profile.TotalXp,
            Math.Max(0, xpIntoLevel),
            xpRequired,
            percentage);
    }

    public static void RecalculateLevel(PlayerProfile profile)
    {
        while (profile.CurrentLevel > 1 &&
               profile.TotalXp < GetXpThresholdForLevel(profile.CurrentLevel, profile.BaseXpPerLevel))
        {
            profile.CurrentLevel--;
        }

        while (profile.TotalXp >= GetXpThresholdForLevel(profile.CurrentLevel + 1, profile.BaseXpPerLevel))
        {
            profile.CurrentLevel++;
        }
    }
}
