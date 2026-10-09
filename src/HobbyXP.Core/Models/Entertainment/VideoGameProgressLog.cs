using HobbyXP.Models.Common;

namespace HobbyXP.Models.Entertainment;

public class VideoGameProgressLog : EntityBase
{
    public int VideoGameId { get; set; }

    public VideoGame VideoGame { get; set; } = null!;

    /// <summary>Fecha del avance (inicio del día en UTC).</summary>
    public DateTime ProgressDate { get; set; }

    /// <summary>Puntos porcentuales avanzados ese día.</summary>
    public int PercentDelta { get; set; }
}
