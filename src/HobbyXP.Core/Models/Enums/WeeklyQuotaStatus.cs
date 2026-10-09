namespace HobbyXP.Models.Enums;

public enum WeeklyQuotaStatus
{
    /// <summary>La cuota se cumplió (sin castigo).</summary>
    Met = 0,

    /// <summary>Castigo aplicado: se bajó un nivel del hobby.</summary>
    Penalized = 1,

    /// <summary>Castigo revertido tras registrar actividad atrasada de esa semana.</summary>
    Restored = 2,

    /// <summary>No se pudo bajar de nivel (ya en piso: nivel 1 y 0 XP).</summary>
    SkippedFloor = 3,

    /// <summary>Incumplida, pero había inmunidad de disciplina activa.</summary>
    Waived = 4
}
