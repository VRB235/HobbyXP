using HobbyXP.Services.Results;

namespace HobbyXP.ViewModels.Messaging;

public sealed class AchievementMessenger : IAchievementMessenger
{
    public event EventHandler<AchievementEvent>? AchievementPublished;

    public void Publish(AchievementEvent achievementEvent) =>
        AchievementPublished?.Invoke(this, achievementEvent);

    public void PublishRange(IEnumerable<AchievementEvent> events)
    {
        foreach (var achievementEvent in events)
            Publish(achievementEvent);
    }
}
