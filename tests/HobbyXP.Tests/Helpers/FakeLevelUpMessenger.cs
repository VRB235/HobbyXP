using HobbyXP.Services.Messaging;

namespace HobbyXP.Tests.Helpers;

public sealed class FakeLevelUpMessenger : ILevelUpMessenger
{
    public event EventHandler<LevelUpCelebrationInfo>? LevelUpPublished;

    public List<LevelUpCelebrationInfo> Published { get; } = [];

    public void Publish(int newLevel, int totalXp)
    {
        var info = new LevelUpCelebrationInfo(newLevel, totalXp);
        Published.Add(info);
        LevelUpPublished?.Invoke(this, info);
    }
}
