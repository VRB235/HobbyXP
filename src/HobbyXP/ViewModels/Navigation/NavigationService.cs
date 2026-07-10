using HobbyXP.ViewModels.Achievements;
using HobbyXP.ViewModels.Common;
using HobbyXP.ViewModels.Dashboard;
using HobbyXP.ViewModels.Entertainment;
using HobbyXP.ViewModels.PersonalGrowth;
using HobbyXP.ViewModels.Physical;
using HobbyXP.ViewModels.Settings;

namespace HobbyXP.ViewModels.Navigation;

public sealed class NavigationService : INavigationService
{
    private readonly DashboardViewModel _dashboard;
    private readonly PhysicalActivitiesViewModel _physicalActivities;
    private readonly EntertainmentViewModel _entertainment;
    private readonly PersonalGrowthViewModel _personalGrowth;
    private readonly AchievementsViewModel _achievements;
    private readonly SettingsViewModel _settings;

    public NavigationService(
        DashboardViewModel dashboard,
        PhysicalActivitiesViewModel physicalActivities,
        EntertainmentViewModel entertainment,
        PersonalGrowthViewModel personalGrowth,
        AchievementsViewModel achievements,
        SettingsViewModel settings)
    {
        _dashboard = dashboard;
        _physicalActivities = physicalActivities;
        _entertainment = entertainment;
        _personalGrowth = personalGrowth;
        _achievements = achievements;
        _settings = settings;
        CurrentViewModel = _dashboard;
    }

    public object? CurrentViewModel { get; private set; }

    public NavigationSection CurrentSection { get; private set; } = NavigationSection.Dashboard;

    public event EventHandler? CurrentViewModelChanged;

    public async Task NavigateAsync(NavigationSection section)
    {
        if (CurrentSection == section && CurrentViewModel is INavigatableViewModel loaded && loaded is LoadableViewModelBase { IsLoaded: true })
            return;

        CurrentSection = section;
        CurrentViewModel = section switch
        {
            NavigationSection.Dashboard => _dashboard,
            NavigationSection.PhysicalActivities => _physicalActivities,
            NavigationSection.Entertainment => _entertainment,
            NavigationSection.PersonalGrowth => _personalGrowth,
            NavigationSection.Achievements => _achievements,
            NavigationSection.Settings => _settings,
            _ => _dashboard
        };

        CurrentViewModelChanged?.Invoke(this, EventArgs.Empty);

        if (CurrentViewModel is INavigatableViewModel navigatable)
            await navigatable.LoadAsync();
    }

    public void InvalidateAllLoadedSections()
    {
        Invalidate(_dashboard);
        Invalidate(_physicalActivities);
        Invalidate(_entertainment);
        Invalidate(_personalGrowth);
        Invalidate(_achievements);
        Invalidate(_settings);
    }

    private static void Invalidate(LoadableViewModelBase viewModel) => viewModel.InvalidateLoaded();
}
