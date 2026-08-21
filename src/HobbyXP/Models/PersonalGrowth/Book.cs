using System.ComponentModel.DataAnnotations.Schema;
using HobbyXP.Helpers;
using HobbyXP.Models.Common;
using HobbyXP.Models.Enums;

namespace HobbyXP.Models.PersonalGrowth;

public class Book : EntityBase
{
    public string Title { get; set; } = string.Empty;

    public string Author { get; set; } = string.Empty;

    public int TotalPages { get; set; }

    public int PagesRead { get; set; }

    public BookStatus Status { get; set; } = BookStatus.Reading;

    public DateTime? CompletedAt { get; set; }

    public int XpEarned { get; set; }

    /// <summary>Ruta relativa al directorio de datos (portada).</summary>
    public string? ImagePath { get; set; }

    public ICollection<BookReadingLog> ReadingLogs { get; set; } = [];

    [NotMapped]
    public string CompletedAtLabel => CompletedAt?.ToLocalTime().ToString("dd/MM/yyyy") ?? "—";

    [NotMapped]
    public string? ImageDisplayPath => HobbyCoverPhotoStorage.ResolveAbsolutePath(ImagePath);

    [NotMapped]
    public bool HasImage => !string.IsNullOrWhiteSpace(ImageDisplayPath);
}
