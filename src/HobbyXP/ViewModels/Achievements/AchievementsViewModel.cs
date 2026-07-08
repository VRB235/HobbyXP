using HobbyXP.ViewModels.Common;
using HobbyXP.ViewModels.Navigation;

namespace HobbyXP.ViewModels.Achievements;

public sealed class AchievementsViewModel : LoadableViewModelBase
{
    private int _selectedTabIndex;

    public AchievementsViewModel(
        MedalShowcaseViewModel medals,
        RulesEditorViewModel rules,
        RewardShopViewModel rewards)
    {
        Medals = medals;
        Rules = rules;
        Rewards = rewards;
    }

    public MedalShowcaseViewModel Medals { get; }

    public RulesEditorViewModel Rules { get; }

    public RewardShopViewModel Rewards { get; }

    public int SelectedTabIndex
    {
        get => _selectedTabIndex;
        set => SetProperty(ref _selectedTabIndex, value);
    }

    protected override async Task LoadCoreAsync()
    {
        await Medals.LoadAsync();
        await Rules.LoadAsync();
        await Rewards.LoadAsync();
    }
}
