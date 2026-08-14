using HobbyXP.Models.Common;

namespace HobbyXP.Models.PersonalGrowth;

public class BookReadingLog : EntityBase
{
    public int BookId { get; set; }

    public Book Book { get; set; } = null!;

    /// <summary>Fecha de la lectura (inicio del día en UTC).</summary>
    public DateTime ReadDate { get; set; }

    public int PagesDone { get; set; }
}
