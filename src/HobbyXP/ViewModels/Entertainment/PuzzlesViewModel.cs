using System.Collections.ObjectModel;
using HobbyXP.Helpers;
using HobbyXP.Models.Entertainment;
using HobbyXP.Models.Enums;
using HobbyXP.Services.Abstractions;
using HobbyXP.Services.Messaging;
using HobbyXP.ViewModels.Common;
using HobbyXP.ViewModels.Messaging;

namespace HobbyXP.ViewModels.Entertainment;

public sealed class PuzzlesViewModel : AchievementAwareViewModel
{
    private readonly IPuzzleService _puzzleService;
    private readonly IFileDialogService _fileDialogService;
    private readonly IMessageDialogService _messageDialogService;
    private readonly IProfileRefreshMessenger _profileRefreshMessenger;
    private string _name = string.Empty;
    private string _pieceCount = "500";
    private PuzzleCategory _category = PuzzleCategory.TwoD;
    private DateTime? _completedDate = DateTime.Today;
    private DateTime? _filterFromDate;
    private DateTime? _filterToDate;
    private List<Puzzle> _allPuzzles = [];

    public PuzzlesViewModel(
        IPuzzleService puzzleService,
        IFileDialogService fileDialogService,
        IMessageDialogService messageDialogService,
        IProfileRefreshMessenger profileRefreshMessenger,
        IAchievementMessenger achievementMessenger)
        : base(achievementMessenger)
    {
        _puzzleService = puzzleService;
        _fileDialogService = fileDialogService;
        _messageDialogService = messageDialogService;
        _profileRefreshMessenger = profileRefreshMessenger;
        Puzzles = new ObservableCollection<Puzzle>();
        SelectedPhotos = new ObservableCollection<PuzzlePhotoItem>();
        RegisterCommand = new AsyncRelayCommand(RegisterAsync, CanRegister);
        PickPhotosCommand = new RelayCommand(PickPhotos);
        RemovePhotoCommand = new RelayCommand(RemovePhoto);
        ClearDateFilterCommand = new RelayCommand(ClearDateFilter);
        DeletePuzzleCommand = new AsyncRelayCommand(p => DeletePuzzleAsync(p));
    }

    public ObservableCollection<Puzzle> Puzzles { get; }

    public ObservableCollection<PuzzlePhotoItem> SelectedPhotos { get; }

    public Array Categories => Enum.GetValues(typeof(PuzzleCategory));

    public string Name
    {
        get => _name;
        set => SetProperty(ref _name, value);
    }

    public string PieceCount
    {
        get => _pieceCount;
        set => SetProperty(ref _pieceCount, value);
    }

    public PuzzleCategory Category
    {
        get => _category;
        set => SetProperty(ref _category, value);
    }

    public DateTime? CompletedDate
    {
        get => _completedDate;
        set => SetProperty(ref _completedDate, value);
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

    public RelayCommand PickPhotosCommand { get; }

    public RelayCommand RemovePhotoCommand { get; }

    public RelayCommand ClearDateFilterCommand { get; }

    public AsyncRelayCommand DeletePuzzleCommand { get; }

    public bool HasSelectedPhotos => SelectedPhotos.Count > 0;

    protected override async Task LoadCoreAsync()
    {
        _allPuzzles = (await _puzzleService.GetAllAsync()).ToList();
        ApplyFilter();
    }

    private void ApplyFilter()
    {
        Puzzles.Clear();
        foreach (var puzzle in _allPuzzles.Where(p => DateRangeFilter.Matches(p.CompletedAt, FilterFromDate, FilterToDate)))
            Puzzles.Add(puzzle);
    }

    private void ClearDateFilter()
    {
        _filterFromDate = null;
        _filterToDate = null;
        OnPropertyChanged(nameof(FilterFromDate));
        OnPropertyChanged(nameof(FilterToDate));
        ApplyFilter();
    }

    private void PickPhotos()
    {
        var paths = _fileDialogService.PickImageFiles();
        if (paths.Count == 0)
            return;

        var existing = SelectedPhotos
            .Select(photo => photo.FilePath)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var path in paths)
        {
            if (existing.Contains(path))
                continue;

            var item = PuzzlePhotoItem.TryCreate(path);
            if (item is null)
                continue;

            SelectedPhotos.Add(item);
            existing.Add(path);
        }

        OnPropertyChanged(nameof(HasSelectedPhotos));
    }

    private void RemovePhoto(object? parameter)
    {
        if (parameter is not PuzzlePhotoItem photo)
            return;

        SelectedPhotos.Remove(photo);
        OnPropertyChanged(nameof(HasSelectedPhotos));
    }

    private bool CanRegister() =>
        !string.IsNullOrWhiteSpace(Name) && int.TryParse(PieceCount, out var count) && count > 0;

    private async Task RegisterAsync()
    {
        if (!CanRegister())
            return;

        var pieces = int.Parse(PieceCount);
        var photoPaths = SelectedPhotos.Select(photo => photo.FilePath).ToList();
        var completedAt = DateTimeHelper.ToUtcFromLocalDate(CompletedDate ?? DateTime.Today);

        await RunBusyAsync(async () =>
        {
            var result = await _puzzleService.RegisterCompletedAsync(
                Name, pieces, Category, photoPaths, completedAt);
            PublishAchievements(result.Events);

            _allPuzzles.Insert(0, result.Value);
            ApplyFilter();

            Name = string.Empty;
            PieceCount = "500";
            CompletedDate = DateTime.Today;
            SelectedPhotos.Clear();
            OnPropertyChanged(nameof(HasSelectedPhotos));
            StatusMessage = $"Rompecabezas registrado · +{result.Value.XpEarned} XP";
        }, "Guardando rompecabezas...");
    }

    private async Task DeletePuzzleAsync(object? parameter)
    {
        if (parameter is not Puzzle puzzle)
            return;

        if (!_messageDialogService.Confirm(
                $"¿Eliminar «{puzzle.Name}» del historial?\nSe revertirá el XP asociado.",
                "Eliminar del historial"))
            return;

        await RunBusyAsync(async () =>
        {
            if (!await _puzzleService.DeleteAsync(puzzle.Id))
                return;

            _allPuzzles.RemoveAll(p => p.Id == puzzle.Id);
            ApplyFilter();
            _profileRefreshMessenger.RequestRefresh();
            StatusMessage = $"«{puzzle.Name}» eliminado del historial.";
        }, "Eliminando rompecabezas...");
    }
}
