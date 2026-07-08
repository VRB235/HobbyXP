using HobbyXP.Models.Common;

namespace HobbyXP.Models.PersonalGrowth;

public class Course : EntityBase
{
    public string Name { get; set; } = string.Empty;

    public string Platform { get; set; } = string.Empty;

    public DateTime CompletedAt { get; set; } = DateTime.UtcNow;

    public int XpEarned { get; set; }
}
