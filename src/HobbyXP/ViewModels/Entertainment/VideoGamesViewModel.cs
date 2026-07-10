using System.Collections.ObjectModel;
using HobbyXP.Helpers;
using HobbyXP.Models.Entertainment;
using HobbyXP.Models.Enums;
using HobbyXP.Services.Abstractions;
using HobbyXP.Services.Messaging;
using HobbyXP.ViewModels.Common;
using HobbyXP.ViewModels.Messaging;

namespace HobbyXP.ViewModels.Entertainment;

public sealed class VideoGamesViewModel : AchievementAwareViewModel
{
    private readonly IVideoGameService _videoGameService;
    private readonly IMessageDialogService _messageDialogService;
    private readonly IProfileRefreshMessenger _profileRefreshMessenger;
    private string _title = string.Empty;
    private VideoGamePlatform _platform = VideoGamePlatform.Pc;
    private int _initialCompletion;
    private DateTime? _startedDate = DateTime.Today;
    private DateTime? _filterFromDate;
    private DateTime? _filterToDate;
    private List<VideoGame> _allInProgress = [];
    private List<VideoGame> _allPlatinum = [];

    public VideoGamesViewModel(
        IVideoGameService videoGameService,
        IMessageDialogService messageDialogService,
        IProfileRefreshMessenger profileRefreshMessenger,
        IAchievementMessenger achievementMessenger)
        : base(achievementMessenger)
    {
        _videoGameService = videoGameService;
        _messageDialogService = messageDialogService;
        _profileRefreshMessenger = profileRefreshMessenger;
        InProgressRows = new ObservableCollection<VideoGameProgressRowViewModel>();
        PlatinumGames = new ObservableCollection<VideoGame>();

        RegisterCommand = new AsyncRelayCommand(RegisterAsync, CanRegister);
        ClearDateFilterCommand = new RelayCommand(ClearDateFilter);
        DeleteGameCommand = new AsyncRelayCommand(p => DeleteGameAsync(p));
        RefreshRegisterValidation();
    }

    public ObservableCollection<VideoGameProgressRowViewModel> InProgressRows { get; }

    public ObservableCollection<VideoGame> PlatinumGames { get; }

    public Array Platforms => Enum.GetValues(typeof(VideoGamePlatform));

    public string Title
    {
        get => _title;
        set
        {
            if (SetProperty(ref _title, value))
                RefreshRegisterValidation();
        }
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

    public DateTime? StartedDate
    {
        get => _startedDate;
        set
        {
            if (SetProperty(ref _startedDate, value))
                RefreshRegisterValidation();
        }
    }

    public DateTime? FilterFromDate
    {
        get => _filterFromDate;
        set
        {
            if (SetProperty(ref _filterFromDate, value))
                ApplyFilter();
        }
    }

    public DateTime? FilterToDate
    {
        get => _filterToDate;
        set
        {
            if (SetProperty(ref _filterToDate, value))
                ApplyFilter();
        }
    }

    public AsyncRelayCommand RegisterCommand { get; }

    public RelayCommand ClearDateFilterCommand { get; }

    public AsyncRelayCommand DeleteGameCommand { get; }

    protected override Task LoadCoreAsync() => ReloadGamesAsync();

    private async Task ReloadGamesAsync()
    {
        _allInProgress = (await _videoGameService.GetInProgressAsync()).ToList();
        _allPlatinum = (await _videoGameService.GetPlatinumAsync()).ToList();
        ApplyFilter();
    }

    private void ApplyFilter()
    {
        InProgressRows.Clear();
        foreach (var game in _allInProgress.Where(g => MatchesGameDate(g.StartedAt, FilterFromDate, FilterToDate)))
            InProgressRows.Add(new VideoGameProgressRowViewModel(game, ApplyProgressAsync));

        PlatinumGames.Clear();
        foreach (var game in _allPlatinum.Where(g => MatchesGameDate(g.PlatinumUnlockedAt ?? g.StartedAt, FilterFromDate, FilterToDate)))
            PlatinumGames.Add(game);
    }

    private static bool MatchesGameDate(DateTime? value, DateTime? from, DateTime? to) =>
        value.HasValue
            ? DateRangeFilter.Matches(value.Value, from, to)
            : !from.HasValue && !to.HasValue;

    private void ClearDateFilter()
    {
        _filterFromDate = null;
        _filterToDate = null;
        OnPropertyChanged(nameof(FilterFromDate));
        OnPropertyChanged(nameof(FilterToDate));
        ApplyFilter();
    }

    private async Task ApplyProgressAsync(VideoGame game, int targetCompletion)
    {
        await RunBusyAsync(async () =>
        {
            var result = await _videoGameService.UpdateCompletionAsync(game.Id, targetCompletion);
            PublishAchievements(result.Events);
            await ReloadGamesAsync();
            _profileRefreshMessenger.RequestRefresh();
            StatusMessage = $"{result.Value.Title}: {result.Value.CompletionPercentage}%";
        }, "Actualizando progreso...");
    }

    private ValidationResult ValidateRegisterForm() =>
        FormValidation.FirstFailure(
            FormValidation.RequireText(Title, "el título"),
            StartedDate.HasValue
                ? ValidationResult.Ok()
                : ValidationResult.Fail("Indique la fecha de inicio."));

    private void RefreshRegisterValidation() =>
        RefreshValidation(ValidateRegisterForm(), RegisterCommand);

    private bool CanRegister() => ValidateRegisterForm().IsValid;

    private async Task RegisterAsync()
    {
        if (!ValidateRegisterForm().IsValid)
        {
            RefreshRegisterValidation();
            return;
        }

        await RunBusyAsync(async () =>
        {
            var startedAt = DateTimeHelper.ToUtcFromLocalDate(StartedDate ?? DateTime.Today);
            var result = await _videoGameService.RegisterAsync(Title, Platform, InitialCompletion, startedAt);
            PublishAchievements(result.Events);
            await ReloadGamesAsync();

            Title = string.Empty;
            InitialCompletion = 0;
            StartedDate = DateTime.Today;
            ClearValidation();
            StatusMessage = $"Juego registrado · +{result.Value.XpEarned} XP";
        }, "Registrando juego...");
    }

    private async Task DeleteGameAsync(object? parameter)
    {
        if (parameter is not VideoGame game)
            return;

        if (!_messageDialogService.Confirm(
                $"¿Eliminar «{game.Title}» del historial?\nSe revertirá el XP asociado.",
                "Eliminar del historial"))
            return;

        await RunBusyAsync(async () =>
        {
            if (!await _videoGameService.DeleteAsync(game.Id))
                return;

            _allInProgress.RemoveAll(g => g.Id == game.Id);
            _allPlatinum.RemoveAll(g => g.Id == game.Id);
            ApplyFilter();
            _profileRefreshMessenger.RequestRefresh();
            StatusMessage = $"«{game.Title}» eliminado del historial.";
        }, "Eliminando videojuego...");
    }
}
