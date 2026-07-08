namespace HobbyXP.Services.Messaging;

public sealed record LevelUpCelebrationInfo(int NewLevel, int TotalXp);

public interface ILevelUpMessenger
{
    event EventHandler<LevelUpCelebrationInfo>? LevelUpPublished;

    void Publish(int newLevel, int totalXp);
}
