using HobbyXP.Models.Enums;

namespace HobbyXP.Services.Results;

/// <summary>
/// Evento de logro para alertas visuales, celebraciones o notificaciones en la UI.
/// </summary>
public sealed record AchievementEvent(
    string Title,
    string Description,
    int PointsEarned,
    MilestoneSourceType SourceType,
    int? MedalDefinitionId = null,
    string? MedalIconPath = null,
    bool RequiresCelebration = false)
{
    public bool IsMedalUnlock => MedalDefinitionId.HasValue;
}
