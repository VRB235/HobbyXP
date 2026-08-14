using HobbyXP.Models.Common;
using HobbyXP.Models.Enums;

namespace HobbyXP.Models.Entertainment;

/// <summary>
/// Serie en seguimiento por capítulos (análogo a <c>Course</c> con sesiones).
/// </summary>
public class MediaSeries : EntityBase
{
    public string Title { get; set; } = string.Empty;

    public int TotalChapters { get; set; } = 1;

    public int ChaptersWatched { get; set; }

    public MediaSeriesStatus Status { get; set; } = MediaSeriesStatus.InProgress;

    public DateTime? CompletedAt { get; set; }

    public int XpEarned { get; set; }

    /// <summary>Entrada de historial creada al completar (si aplica).</summary>
    public int? CompletedMediaEntryId { get; set; }

    public ICollection<MediaSeriesChapterLog> ChapterLogs { get; set; } = [];
}
