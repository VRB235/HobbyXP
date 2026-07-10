using HobbyXP.Models.Core;
using HobbyXP.Services.Results;

namespace HobbyXP.Services.Abstractions;

public interface IPlayerProfileService
{
    Task<PlayerProfile> GetProfileAsync(CancellationToken cancellationToken = default);

    Task<LevelProgressInfo> GetLevelProgressAsync(CancellationToken cancellationToken = default);

    Task<PlayerProfile> UpdateDisplayNameAsync(string displayName, CancellationToken cancellationToken = default);

    Task<PlayerProfile> UpdateAvatarPathAsync(string? avatarPath, CancellationToken cancellationToken = default);

    Task<PlayerProfile> UpdateBaseXpPerLevelAsync(int baseXpPerLevel, CancellationToken cancellationToken = default);
}
