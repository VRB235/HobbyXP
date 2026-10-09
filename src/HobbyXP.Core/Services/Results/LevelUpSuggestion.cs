namespace HobbyXP.Services.Results;

public sealed record LevelUpSuggestion(
    string Category,
    string MinimumRequirement,
    int EstimatedXp,
    string Description);
