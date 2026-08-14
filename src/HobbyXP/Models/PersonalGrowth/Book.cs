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

    public ICollection<BookReadingLog> ReadingLogs { get; set; } = [];
}
