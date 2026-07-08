using HobbyXP.ViewModels.Common;
using HobbyXP.ViewModels.Navigation;

namespace HobbyXP.ViewModels.Physical;

public sealed class PhysicalActivitiesViewModel : LoadableViewModelBase
{
    private int _selectedTabIndex;

    public PhysicalActivitiesViewModel(RunningViewModel running, GymViewModel gym)
    {
        Running = running;
        Gym = gym;
    }

    public RunningViewModel Running { get; }

    public GymViewModel Gym { get; }

    public int SelectedTabIndex
    {
        get => _selectedTabIndex;
        set => SetProperty(ref _selectedTabIndex, value);
    }

    protected override async Task LoadCoreAsync()
    {
        await Running.LoadAsync();
        await Gym.LoadAsync();
    }
}
