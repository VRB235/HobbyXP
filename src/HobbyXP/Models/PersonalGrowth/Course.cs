using System.ComponentModel.DataAnnotations.Schema;
using HobbyXP.Helpers;
using HobbyXP.Models.Common;
using HobbyXP.Models.Enums;

namespace HobbyXP.Models.PersonalGrowth;

public class Course : EntityBase
{
    public string Name { get; set; } = string.Empty;

    public string Platform { get; set; } = string.Empty;

    public int TotalSessions { get; set; } = 1;

    public int SessionsCompleted { get; set; }

    public CourseStatus Status { get; set; } = CourseStatus.InProgress;

    public DateTime? CompletedAt { get; set; }

    public int XpEarned { get; set; }

    /// <summary>Ruta relativa al directorio de datos (portada).</summary>
    public string? ImagePath { get; set; }

    public ICollection<CourseSessionLog> SessionLogs { get; set; } = [];

    [NotMapped]
    public string CompletedAtLabel => CompletedAt?.ToLocalTime().ToString("dd/MM/yyyy") ?? "—";

    [NotMapped]
    public string? ImageDisplayPath => HobbyCoverPhotoStorage.ResolveAbsolutePath(ImagePath);

    [NotMapped]
    public bool HasImage => !string.IsNullOrWhiteSpace(ImageDisplayPath);
}
