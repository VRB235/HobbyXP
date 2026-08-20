using HobbyXP.ViewModels.Achievements;
using HobbyXP.ViewModels.Dashboard;
using HobbyXP.ViewModels.Entertainment;
using HobbyXP.ViewModels.Messaging;
using HobbyXP.ViewModels.Navigation;
using HobbyXP.ViewModels.PersonalGrowth;
using HobbyXP.ViewModels.Physical;
using HobbyXP.ViewModels.Settings;
using HobbyXP.ViewModels.Suggestions;
using Microsoft.Extensions.DependencyInjection;

namespace HobbyXP.ViewModels;

public static class ViewModelServiceCollectionExtensions
{
    public static IServiceCollection AddHobbyXpViewModels(this IServiceCollection services)
    {
        services.AddSingleton<IAchievementMessenger, AchievementMessenger>();
        services.AddScoped<INavigationService, NavigationService>();

        services.AddScoped<MainViewModel>();
        services.AddScoped<DashboardViewModel>();

        services.AddScoped<RunningViewModel>();
        services.AddScoped<GymViewModel>();
        services.AddScoped<DietViewModel>();
        services.AddScoped<PhysicalActivitiesViewModel>();

        services.AddScoped<PuzzlesViewModel>();
        services.AddScoped<MediaViewModel>();
        services.AddScoped<VideoGamesViewModel>();
        services.AddScoped<EntertainmentViewModel>();

        services.AddScoped<BooksViewModel>();
        services.AddScoped<CoursesViewModel>();
        services.AddScoped<PersonalGrowthViewModel>();

        services.AddScoped<MedalShowcaseViewModel>();
        services.AddScoped<MedalsEditorViewModel>();
        services.AddScoped<RulesEditorViewModel>();
        services.AddScoped<RewardShopViewModel>();
        services.AddScoped<AchievementsViewModel>();
        services.AddScoped<SuggestionsViewModel>();
        services.AddScoped<SettingsViewModel>();

        return services;
    }
}
