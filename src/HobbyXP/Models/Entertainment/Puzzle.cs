using HobbyXP.Models.Common;
using HobbyXP.Models.Enums;

namespace HobbyXP.Models.Entertainment;

public class Puzzle : EntityBase
{
    public string Name { get; set; } = string.Empty;

    public int PieceCount { get; set; }

    /// <summary>
    /// Categoría obligatoria: 2D o 3D.
    /// </summary>
    public PuzzleCategory Category { get; set; }

    /// <summary>
    /// Rutas locales opcionales a fotos del rompecabezas armado (JSON con rutas relativas).
    /// </summary>
    public string? PhotoPath { get; set; }

    public DateTime CompletedAt { get; set; } = DateTime.UtcNow;

    public int XpEarned { get; set; }
}
