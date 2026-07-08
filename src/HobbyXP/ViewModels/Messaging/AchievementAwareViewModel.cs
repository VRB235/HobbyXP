using HobbyXP.Services.Results;
using HobbyXP.ViewModels.Common;

namespace HobbyXP.ViewModels.Messaging;

public abstract class AchievementAwareViewModel : LoadableViewModelBase
{
    private readonly IAchievementMessenger _achievementMessenger;

    protected AchievementAwareViewModel(IAchievementMessenger achievementMessenger)
    {
        _achievementMessenger = achievementMessenger;
    }

    protected void PublishAchievements(IEnumerable<AchievementEvent> events) =>
        _achievementMessenger.PublishRange(events);
}
