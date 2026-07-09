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

    public ICollection<CourseSessionLog> SessionLogs { get; set; } = [];
}
