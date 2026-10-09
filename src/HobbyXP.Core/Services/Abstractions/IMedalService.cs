using HobbyXP.Models.Achievements;
using HobbyXP.Models.Enums;
using HobbyXP.Services.Results;

namespace HobbyXP.Services.Abstractions;

public interface IMedalService
{
    Task<IReadOnlyList<MedalShowcaseItem>> GetShowcaseAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<MedalShowcaseSection>> GetShowcaseSectionsAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<EarnedMedal>> GetEarnedMedalsAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<MedalDefinition>> GetAllDefinitionsAsync(CancellationToken cancellationToken = default);

    Task<MedalDefinition> CreateAsync(
        MilestoneSourceType sourceType,
        int xpThreshold,
        string name,
        string description,
        string unlockHint,
        string? iconPath = null,
        CancellationToken cancellationToken = default);

    Task<MedalDefinition> UpdateDefinitionAsync(
        MedalDefinition definition,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(int medalDefinitionId, CancellationToken cancellationToken = default);

    IReadOnlyList<string> SuggestNames(MilestoneSourceType sourceType, int xpThreshold);
}
