namespace HobbyXP.Services.Messaging;

public sealed class LevelUpMessenger : ILevelUpMessenger
{
    public event EventHandler<LevelUpCelebrationInfo>? LevelUpPublished;

    public void Publish(int newLevel, int totalXp) =>
        LevelUpPublished?.Invoke(this, new LevelUpCelebrationInfo(newLevel, totalXp));
}
