using HobbyXP.Services.Results;

namespace HobbyXP.ViewModels.Messaging;

public interface IAchievementMessenger
{
    event EventHandler<AchievementEvent>? AchievementPublished;

    void Publish(AchievementEvent achievementEvent);

    void PublishRange(IEnumerable<AchievementEvent> events);
}
