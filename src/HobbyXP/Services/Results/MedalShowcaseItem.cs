using HobbyXP.Models.Enums;

namespace HobbyXP.Services.Results;

public sealed record MedalShowcaseItem(
    int MedalDefinitionId,
    MedalCode Code,
    string Name,
    string Description,
    string UnlockHint,
    string? IconPath,
    bool IsEarned,
    DateTime? EarnedAt);
