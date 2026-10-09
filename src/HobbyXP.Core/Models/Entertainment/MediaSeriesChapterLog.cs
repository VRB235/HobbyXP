using HobbyXP.Models.Common;

namespace HobbyXP.Models.Entertainment;

public class MediaSeriesChapterLog : EntityBase
{
    public int MediaSeriesId { get; set; }

    public MediaSeries MediaSeries { get; set; } = null!;

    /// <summary>Fecha del visionado (inicio del día en UTC).</summary>
    public DateTime WatchDate { get; set; }

    public int ChaptersDone { get; set; }
}
