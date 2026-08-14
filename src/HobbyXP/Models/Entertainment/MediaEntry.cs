using HobbyXP.Helpers;
using HobbyXP.Models.Common;
using HobbyXP.Models.Enums;

namespace HobbyXP.Models.Entertainment;

/// <summary>
/// Registro histórico de series y películas terminadas.
/// </summary>
public class MediaEntry : EntityBase
{
    public string Title { get; set; } = string.Empty;

    public MediaType MediaType { get; set; }

    /// <summary>Etiqueta en español para UI.</summary>
    public string MediaTypeLabel => EntertainmentDisplayLabels.GetMediaType(MediaType);

    public DateTime CompletedAt { get; set; } = DateTime.UtcNow;

    public int XpEarned { get; set; }
}
