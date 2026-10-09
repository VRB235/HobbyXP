using HobbyXP.Models.Enums;

namespace HobbyXP.Services.Results;

public sealed record MedalShowcaseItem(
    int MedalDefinitionId,
    string Code,
    string Name,
    string Description,
    string UnlockHint,
    string? IconPath,
    bool IsEarned,
    DateTime? EarnedAt,
    MilestoneSourceType SourceType,
    int XpThreshold);

public sealed record MedalShowcaseSection(
    MilestoneSourceType SourceType,
    string DisplayName,
    IReadOnlyList<MedalShowcaseItem> Medals)
{
    public int EarnedCount => Medals.Count(m => m.IsEarned);

    public string ProgressText => $"{EarnedCount}/{Medals.Count} desbloqueadas";
}
