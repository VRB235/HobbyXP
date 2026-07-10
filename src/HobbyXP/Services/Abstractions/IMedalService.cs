using HobbyXP.Models.Achievements;
using HobbyXP.Services.Results;

namespace HobbyXP.Services.Abstractions;

public interface IMedalService
{
    Task<IReadOnlyList<MedalShowcaseItem>> GetShowcaseAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<EarnedMedal>> GetEarnedMedalsAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<MedalDefinition>> GetAllDefinitionsAsync(CancellationToken cancellationToken = default);

    Task<MedalDefinition> UpdateDefinitionAsync(
        MedalDefinition definition,
        CancellationToken cancellationToken = default);
}
