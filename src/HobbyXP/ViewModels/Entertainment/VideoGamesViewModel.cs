using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using HobbyXP.Helpers;
using HobbyXP.Models.Entertainment;
using HobbyXP.Models.Enums;
using HobbyXP.Services.Abstractions;
using HobbyXP.Services.Messaging;
using HobbyXP.ViewModels.Common;
using HobbyXP.ViewModels.Messaging;
using HobbyXP.Views.Dialogs;

namespace HobbyXP.ViewModels.Entertainment;

public sealed class VideoGamesViewModel : AchievementAwareViewModel
{
    private readonly IVideoGameService _videoGameService;
    private readonly IFileDialogService _fileDialogService;
    private readonly IMessageDialogService _messageDialogService;
    private readonly IProfileRefreshMessenger _profileRefreshMessenger;
    private readonly CoverImageDraft _cover;
    private string _title = string.Empty;
    private VideoGamePlatform _platform = VideoGamePlatform.Pc;
    private int _initialCompletion;
    private DateTime? _startedDate = DateTime.Today;
    private string _searchText = string.Empty;
    private EnumFilterOption<VideoGamePlatform> _platformFilterOption;
    private DateTime? _filterFromDate;
    private DateTime? _filterToDate;
    private List<VideoGame> _allInProgress = [];
    private List<VideoGame> _allPlatinum = [];

    public VideoGamesViewModel(
        IVideoGameService videoGameService,
        IXpService xpService,
        IWeeklyQuotaService weeklyQuotaService,
        IFileDialogService fileDialogService,
        IMessageDialogService messageDialogService,
        IProfileRefreshMessenger profileRefreshMessenger,
        IAchievementMessenger achievementMessenger,
        IAchievementProgressService achievementProgress)
        : base(achievementMessenger)
    {
        _videoGameService = videoGameService;
        _fileDialogService = fileDialogService;
        _messageDialogService = messageDialogService;
        _profileRefreshMessenger = profileRefreshMessenger;
        _cover = new CoverImageDraft(HobbyCoverPhotoStorage.Folders.VideoGames);
        _cover.Changed += OnCoverChanged;

        HobbyXp = new HobbyProgressPresenter(xpService, MilestoneSourceType.VideoGame, weeklyQuotaService, achievementProgress);
        InProgressRows = new ObservableCollection<VideoGameProgressRowViewModel>();
        PlatinumGames = new ObservableCollection<VideoGame>();
        PlatformFilterOptions = EnumFilterOption<VideoGamePlatform>.Create(
            "Todas las plataformas",
            EntertainmentDisplayLabels.GetVideoGamePlatform);
        _platformFilterOption = PlatformFilterOptions[0];

        RegisterCommand = new AsyncRelayCommand(RegisterAsync, CanRegister);
        ClearDateFilterCommand = new RelayCommand(ClearHistoryFilters);
        DeleteGameCommand = new AsyncRelayCommand(p => DeleteGameAsync(p));
        PickImageCommand = new RelayCommand(() => _cover.Pick(_fileDialogService));
        ClearImageCommand = new RelayCommand(() => _cover.Clear(), () => _cover.HasPreview);
        OpenDetailCommand = new RelayCommand(OpenDetail);
        RefreshRegisterValidation();
    }

    public HobbyProgressPresenter HobbyXp { get; }

    public ObservableCollection<VideoGameProgressRowViewModel> InProgressRows { get; }

    public ObservableCollection<VideoGame> PlatinumGames { get; }

    public Array Platforms => Enum.GetValues(typeof(VideoGamePlatform));

    public IReadOnlyList<EnumFilterOption<VideoGamePlatform>> PlatformFilterOptions { get; }

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

    public string? PreviewImagePath => _cover.PreviewPath;

    public bool HasPreviewImage => _cover.HasPreview;

    public string SearchText
    {
        get => _searchText;
        set
        {
            if (SetProperty(ref _searchText, value))
                ApplyFilter();
        }
    }

    public EnumFilterOption<VideoGamePlatform> PlatformFilterOption
    {
        get => _platformFilterOption;
        set
        {
            if (SetProperty(ref _platformFilterOption, value))
                ApplyFilter();
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

    public RelayCommand PickImageCommand { get; }

    public RelayCommand ClearImageCommand { get; }

    public RelayCommand OpenDetailCommand { get; }

    protected override Task LoadCoreAsync() => ReloadGamesAsync();

    private void OnCoverChanged()
    {
        OnPropertyChanged(nameof(PreviewImagePath));
        OnPropertyChanged(nameof(HasPreviewImage));
        CommandManager.InvalidateRequerySuggested();
    }

    private void ResetCover()
    {
        _cover.MarkSaved();
        _cover.Clear();
        OnCoverChanged();
    }

    private async Task ReloadGamesAsync()
    {
        await HobbyXp.RefreshAsync();
        _allInProgress = (await _videoGameService.GetInProgressAsync()).ToList();
        _allPlatinum = (await _videoGameService.GetPlatinumAsync()).ToList();
        ApplyFilter();
    }

    private void ApplyFilter()
    {
        InProgressRows.Clear();
        foreach (var game in _allInProgress.Where(MatchesInProgressFilters))
            InProgressRows.Add(new VideoGameProgressRowViewModel(
                game,
                ApplyProgressAsync,
                UpdateGameImageAsync,
                _fileDialogService));

        PlatinumGames.Clear();
        foreach (var game in _allPlatinum.Where(MatchesPlatinumFilters))
            PlatinumGames.Add(game);
    }

    private bool MatchesInProgressFilters(VideoGame game) =>
        TextSearchFilter.Matches(game.Title, SearchText) &&
        PlatformFilterOption.Matches(game.Platform) &&
        MatchesGameDate(game.StartedAt, FilterFromDate, FilterToDate);

    private bool MatchesPlatinumFilters(VideoGame game) =>
        TextSearchFilter.Matches(game.Title, SearchText) &&
        PlatformFilterOption.Matches(game.Platform) &&
        MatchesGameDate(game.PlatinumUnlockedAt ?? game.StartedAt, FilterFromDate, FilterToDate);

    private static bool MatchesGameDate(DateTime? value, DateTime? from, DateTime? to) =>
        value.HasValue
            ? DateRangeFilter.Matches(value.Value, from, to)
            : !from.HasValue && !to.HasValue;

    private void ClearHistoryFilters()
    {
        _searchText = string.Empty;
        _platformFilterOption = PlatformFilterOptions[0];
        _filterFromDate = null;
        _filterToDate = null;
        OnPropertyChanged(nameof(SearchText));
        OnPropertyChanged(nameof(PlatformFilterOption));
        OnPropertyChanged(nameof(FilterFromDate));
        OnPropertyChanged(nameof(FilterToDate));
        ApplyFilter();
    }

    private async Task ApplyProgressAsync(VideoGame game, int targetCompletion, DateTime progressDate)
    {
        await RunBusyAsync(async () =>
        {
            var result = await _videoGameService.UpdateCompletionAsync(game.Id, targetCompletion, progressDate);
            PublishAchievements(result.Events);
            await ReloadGamesAsync();
            _profileRefreshMessenger.RequestRefresh();
            StatusMessage = $"{result.Value.Title}: {result.Value.CompletionPercentage}%";
        }, "Actualizando progreso...");
    }

    private async Task<VideoGame> UpdateGameImageAsync(VideoGame game, string? imageSourcePath, bool clearImage)
    {
        var updated = await _videoGameService.UpdateImageAsync(game.Id, imageSourcePath, clearImage);
        var index = _allInProgress.FindIndex(g => g.Id == updated.Id);
        if (index >= 0)
            _allInProgress[index] = updated;
        else
        {
            index = _allPlatinum.FindIndex(g => g.Id == updated.Id);
            if (index >= 0)
                _allPlatinum[index] = updated;
        }

        StatusMessage = clearImage
            ? $"Portada quitada de «{updated.Title}»."
            : $"Portada actualizada: «{updated.Title}».";
        return updated;
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
            var result = await _videoGameService.RegisterAsync(
                Title, Platform, InitialCompletion, startedAt, _cover.PendingSourcePath);
            ResetCover();
            PublishAchievements(result.Events);
            await ReloadGamesAsync();

            Title = string.Empty;
            InitialCompletion = 0;
            StartedDate = DateTime.Today;
            ClearValidation();
            StatusMessage = $"Juego registrado · +{result.Value.XpEarned} XP";
        }, "Registrando juego...");
    }

    private void OpenDetail(object? parameter)
    {
        var game = parameter switch
        {
            VideoGame videoGame => videoGame,
            VideoGameProgressRowViewModel row => row.Game,
            _ => null
        };

        if (game is null)
            return;

        var detailVm = new VideoGameDetailViewModel(game, _videoGameService, _fileDialogService);
        var dialog = new VideoGameDetailWindow(detailVm)
        {
            Owner = Application.Current.MainWindow
        };

        var accepted = dialog.ShowDialog() == true;
        if (!accepted || detailVm.SavedGame is null)
            return;

        _ = ReloadGamesAsync();
        StatusMessage = $"Juego actualizado: {detailVm.SavedGame.Title}";
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
