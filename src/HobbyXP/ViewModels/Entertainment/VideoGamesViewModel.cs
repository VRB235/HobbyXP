using System.Collections.ObjectModel;
using HobbyXP.Models.Entertainment;
using HobbyXP.Models.Enums;
using HobbyXP.Services.Abstractions;
using HobbyXP.ViewModels.Common;
using HobbyXP.ViewModels.Messaging;

namespace HobbyXP.ViewModels.Entertainment;

public sealed class VideoGamesViewModel : AchievementAwareViewModel
{
    private readonly IVideoGameService _videoGameService;
    private string _title = string.Empty;
    private VideoGamePlatform _platform = VideoGamePlatform.Pc;
    private int _initialCompletion;

    public VideoGamesViewModel(IVideoGameService videoGameService, IAchievementMessenger achievementMessenger)
        : base(achievementMessenger)
    {
        _videoGameService = videoGameService;
        InProgressGames = new ObservableCollection<VideoGame>();
        PlatinumGames = new ObservableCollection<VideoGame>();

        RegisterCommand = new AsyncRelayCommand(RegisterAsync, () => !string.IsNullOrWhiteSpace(Title));
        IncrementCompletionCommand = new AsyncRelayCommand(IncrementCompletionAsync);
    }

    public ObservableCollection<VideoGame> InProgressGames { get; }

    public ObservableCollection<VideoGame> PlatinumGames { get; }

    public Array Platforms => Enum.GetValues(typeof(VideoGamePlatform));

    public string Title
    {
        get => _title;
        set => SetProperty(ref _title, value);
    }

    public VideoGamePlatform Platform
    {
        get => _platform;
        set => SetProperty(ref _platform, value);
    }

    public int InitialCompletion
    {
        get => _initialCompletion;
        set => SetProperty(ref _initialCompletion, Math.Clamp(value, 0, 100));
    }

    public AsyncRelayCommand RegisterCommand { get; }

    public AsyncRelayCommand IncrementCompletionCommand { get; }

    protected override Task LoadCoreAsync() => ReloadGamesAsync();

    private async Task ReloadGamesAsync()
    {
        var inProgress = await _videoGameService.GetInProgressAsync();
        var platinum = await _videoGameService.GetPlatinumAsync();

        InProgressGames.Clear();
        foreach (var game in inProgress)
            InProgressGames.Add(game);

        PlatinumGames.Clear();
        foreach (var game in platinum)
            PlatinumGames.Add(game);
    }

    public async Task UpdateCompletionAsync(VideoGame game, int newPercentage)
    {
        await RunBusyAsync(async () =>
        {
            var result = await _videoGameService.UpdateCompletionAsync(game.Id, newPercentage);
            PublishAchievements(result.Events);
            await ReloadGamesAsync();
            StatusMessage = $"{result.Value.Title}: {result.Value.CompletionPercentage}%";
        }, "Actualizando progreso...");
    }

    private async Task RegisterAsync()
    {
        await RunBusyAsync(async () =>
        {
            var result = await _videoGameService.RegisterAsync(Title, Platform, InitialCompletion);
            PublishAchievements(result.Events);
            await ReloadGamesAsync();

            Title = string.Empty;
            InitialCompletion = 0;
            StatusMessage = $"Juego registrado · +{result.Value.XpEarned} XP";
        }, "Registrando juego...");
    }

    private async Task IncrementCompletionAsync(object? parameter)
    {
        if (parameter is not VideoGame game)
            return;

        await RunBusyAsync(async () =>
        {
            var result = await _videoGameService.IncrementCompletionAsync(game.Id);
            PublishAchievements(result.Events);
            await ReloadGamesAsync();
            StatusMessage = $"{result.Value.Title}: {result.Value.CompletionPercentage}%";
        }, "Actualizando progreso...");
    }
}
