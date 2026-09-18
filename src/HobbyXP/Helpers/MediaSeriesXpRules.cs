namespace HobbyXP.Helpers;

/// <summary>
/// Reparte el XP total de una serie entre sus capítulos (p. ej. 100 XP / N capítulos).
/// Usa redondeo acumulativo para que el total al completar sea exacto.
/// </summary>
public static class MediaSeriesXpRules
{
    public const int DefaultSeriesPoolXp = 100;

    public static int GetCumulativeXp(int chaptersWatched, int totalChapters, int seriesPoolXp)
    {
        if (seriesPoolXp <= 0 || totalChapters <= 0 || chaptersWatched <= 0)
            return 0;

        if (chaptersWatched >= totalChapters)
            return seriesPoolXp;

        return (int)Math.Round(
            seriesPoolXp * (decimal)chaptersWatched / totalChapters,
            MidpointRounding.AwayFromZero);
    }

    public static int GetAwardForProgress(
        int chaptersBefore,
        int chaptersAfter,
        int totalChapters,
        int seriesPoolXp)
    {
        var before = Math.Clamp(chaptersBefore, 0, totalChapters);
        var after = Math.Clamp(chaptersAfter, 0, totalChapters);
        if (after <= before)
            return 0;

        return GetCumulativeXp(after, totalChapters, seriesPoolXp)
               - GetCumulativeXp(before, totalChapters, seriesPoolXp);
    }
}
