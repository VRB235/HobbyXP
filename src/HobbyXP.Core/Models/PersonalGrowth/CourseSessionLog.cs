using HobbyXP.Models.Common;

namespace HobbyXP.Models.PersonalGrowth;

public class CourseSessionLog : EntityBase
{
    public int CourseId { get; set; }

    public Course Course { get; set; } = null!;

    /// <summary>Fecha de la sesión (inicio del día en UTC).</summary>
    public DateTime SessionDate { get; set; }

    public int SessionsDone { get; set; }
}
