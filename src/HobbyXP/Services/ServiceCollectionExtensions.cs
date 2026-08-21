using HobbyXP.Services.Abstractions;
using HobbyXP.Services.Messaging;
using Microsoft.Extensions.DependencyInjection;

namespace HobbyXP.Services;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddHobbyXpServices(this IServiceCollection services)
    {
        services.AddScoped<IXpService, XpService>();
        services.AddScoped<IAchievementEngineService, AchievementEngineService>();
        services.AddScoped<IPlayerProfileService, PlayerProfileService>();
        services.AddScoped<IDatabaseMaintenanceService, DatabaseMaintenanceService>();
        services.AddScoped<IDashboardService, DashboardService>();
        services.AddScoped<IRunningService, RunningService>();
        services.AddScoped<IGymService, GymService>();
        services.AddScoped<IDietService, DietService>();
        services.AddScoped<IPuzzleService, PuzzleService>();
        services.AddScoped<IMediaService, MediaService>();
        services.AddScoped<IVideoGameService, VideoGameService>();
        services.AddScoped<IBookService, BookService>();
        services.AddScoped<ICourseService, CourseService>();
        services.AddScoped<IMedalService, MedalService>();
        services.AddScoped<IRewardService, RewardService>();
        services.AddScoped<IAchievementProgressService, AchievementProgressService>();
        services.AddScoped<IWeeklyQuotaService, WeeklyQuotaService>();
        services.AddScoped<ISuggestionService, SuggestionService>();

        services.AddSingleton<IFileDialogService, FileDialogService>();
        services.AddSingleton<IMessageDialogService, MessageDialogService>();
        services.AddSingleton<IImagePreviewService, ImagePreviewService>();
        services.AddSingleton<ISuggestionDetailService, SuggestionDetailService>();
        services.AddSingleton<ILevelUpMessenger, LevelUpMessenger>();
        services.AddSingleton<IProfileRefreshMessenger, ProfileRefreshMessenger>();
        services.AddSingleton<IApplicationDataResetMessenger, ApplicationDataResetMessenger>();

        return services;
    }
}
