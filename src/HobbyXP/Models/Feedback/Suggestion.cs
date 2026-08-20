using System.ComponentModel.DataAnnotations.Schema;
using HobbyXP.Helpers;
using HobbyXP.Models.Common;
using HobbyXP.Models.Enums;

namespace HobbyXP.Models.Feedback;

public class Suggestion : EntityBase
{
    public string Title { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public SuggestionKind Kind { get; set; } = SuggestionKind.Improvement;

    public SuggestionStatus Status { get; set; } = SuggestionStatus.Open;

    /// <summary>Fecha del reporte (UTC).</summary>
    public DateTime ReportedAt { get; set; } = DateTime.UtcNow;

    public DateTime? ResolvedAt { get; set; }

    /// <summary>Rutas locales opcionales (JSON con rutas relativas al directorio de datos).</summary>
    public string? PhotoPath { get; set; }

    [NotMapped]
    public string KindLabel => SuggestionDisplayLabels.GetKind(Kind);

    [NotMapped]
    public string StatusLabel => SuggestionDisplayLabels.GetStatus(Status);

    [NotMapped]
    public bool IsResolved => Status == SuggestionStatus.Resolved;
}
