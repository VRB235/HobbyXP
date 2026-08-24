using HobbyXP.Models.Common;
using HobbyXP.Models.Enums;

namespace HobbyXP.Models.Core;

/// <summary>
/// Pausa explícita de la disciplina (cuotas diaria/semanal y castigos) para un hobby.
/// </summary>
public class ModuleDisciplinePause : EntityBase
{
    public MilestoneSourceType SourceType { get; set; }

    public bool IsPaused { get; set; }

    public DateTime? PausedAtUtc { get; set; }
}
