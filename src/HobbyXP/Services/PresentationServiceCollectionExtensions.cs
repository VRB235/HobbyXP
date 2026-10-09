using HobbyXP.Services.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace HobbyXP.Services;

/// <summary>Implementaciones WPF de diálogos y previsualización.</summary>
public static class PresentationServiceCollectionExtensions
{
    public static IServiceCollection AddHobbyXpPresentationServices(this IServiceCollection services)
    {
        services.AddSingleton<IFileDialogService, FileDialogService>();
        services.AddSingleton<IMessageDialogService, MessageDialogService>();
        services.AddSingleton<IImagePreviewService, ImagePreviewService>();
        services.AddSingleton<ISuggestionDetailService, SuggestionDetailService>();
        return services;
    }
}
