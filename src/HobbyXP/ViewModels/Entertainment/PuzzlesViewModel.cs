using System.Collections.ObjectModel;
using System.Windows;
using HobbyXP.Helpers;
using HobbyXP.Models.Entertainment;
using HobbyXP.Models.Enums;
using HobbyXP.Services.Abstractions;
using HobbyXP.Services.Messaging;
using HobbyXP.ViewModels.Common;
using HobbyXP.ViewModels.Messaging;
using HobbyXP.Views.Dialogs;

namespace HobbyXP.ViewModels.Entertainment;

public sealed class PuzzlesViewModel : AchievementAwareViewModel
{
    private readonly IPuzzleService _puzzleService;
    private readonly IFileDialogService _fileDialogService;
    private readonly IMessageDialogService _messageDialogService;
    private readonly IImagePreviewService _imagePreviewService;
    private readonly IProfileRefreshMessenger _profileRefreshMessenger;
    private string _name = string.Empty;
    private string _pieceCount = "500";
    private PuzzleCategory _category = PuzzleCategory.TwoD;
    private DateTime? _completedDate = DateTime.Today;
    private string _searchText = string.Empty;
    private EnumFilterOption<PuzzleCategory> _categoryFilterOption;
    private DateTime? _filterFromDate;
    private DateTime? _filterToDate;
    private List<Puzzle> _allPuzzles = [];

    public PuzzlesViewModel(
        IPuzzleService puzzleService,
        IXpService xpService,
        IWeeklyQuotaService weeklyQuotaService,
        IFileDialogService fileDialogService,
        IMessageDialogService messageDialogService,
        IImagePreviewService imagePreviewService,
        IProfileRefreshMessenger profileRefreshMessenger,
        IAchievementMessenger achievementMessenger,
        IAchievementProgressService achievementProgress)
        : base(achievementMessenger)
    {
        _puzzleService = puzzleService;
        _fileDialogService = fileDialogService;
        _messageDialogService = messageDialogService;
        _imagePreviewService = imagePreviewService;
        _profileRefreshMessenger = profileRefreshMessenger;
        HobbyXp = new HobbyProgressPresenter(xpService, MilestoneSourceType.Puzzle, weeklyQuotaService, achievementProgress);
        Puzzles = new ObservableCollection<Puzzle>();
        SelectedPhotos = new ObservableCollection<PuzzlePhotoItem>();
        CategoryFilterOptions = EnumFilterOption<PuzzleCategory>.Create(
            "Todas las categorías",
            EntertainmentDisplayLabels.GetPuzzleCategory);
        _categoryFilterOption = CategoryFilterOptions[0];
        RegisterCommand = new AsyncRelayCommand(RegisterAsync, CanRegister);
        PickPhotosCommand = new RelayCommand(PickPhotos);
        RemovePhotoCommand = new RelayCommand(RemovePhoto);
        OpenPhotoCommand = new RelayCommand(OpenPhoto);
        ClearDateFilterCommand = new RelayCommand(ClearHistoryFilters);
        DeletePuzzleCommand = new AsyncRelayCommand(p => DeletePuzzleAsync(p));
        OpenDetailCommand = new RelayCommand(OpenDetail);
        RefreshRegisterValidation();
    }

    public HobbyProgressPresenter HobbyXp { get; }

    public ObservableCollection<Puzzle> Puzzles { get; }

    public ObservableCollection<PuzzlePhotoItem> SelectedPhotos { get; }

    public Array Categories => Enum.GetValues(typeof(PuzzleCategory));

    public IReadOnlyList<EnumFilterOption<PuzzleCategory>> CategoryFilterOptions { get; }

    public string Name
    {
        get => _name;
        set
        {
            if (SetProperty(ref _name, value))
                RefreshRegisterValidation();
        }
    }

    public string PieceCount
    {
        get => _pieceCount;
        set
        {
            if (SetProperty(ref _pieceCount, value))
                RefreshRegisterValidation();
        }
    }

    public PuzzleCategory Category
    {
        get => _category;
        set => SetProperty(ref _category, value);
    }

    public DateTime? CompletedDate
    {
        get => _completedDate;
        set
        {
            if (SetProperty(ref _completedDate, value))
                RefreshRegisterValidation();
        }
    }

    public string SearchText
    {
        get => _searchText;
        set
        {
            if (SetProperty(ref _searchText, value))
                ApplyFilter();
        }
    }

    public EnumFilterOption<PuzzleCategory> CategoryFilterOption
    {
        get => _categoryFilterOption;
        set
        {
            if (SetProperty(ref _categoryFilterOption, value))
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

    public RelayCommand PickPhotosCommand { get; }

    public RelayCommand RemovePhotoCommand { get; }

    public RelayCommand OpenPhotoCommand { get; }

    public RelayCommand ClearDateFilterCommand { get; }

    public AsyncRelayCommand DeletePuzzleCommand { get; }

    public RelayCommand OpenDetailCommand { get; }

    public bool HasSelectedPhotos => SelectedPhotos.Count > 0;

    protected override async Task LoadCoreAsync()
    {
        await HobbyXp.RefreshAsync();
        _allPuzzles = (await _puzzleService.GetAllAsync()).ToList();
        ApplyFilter();
    }

    private void ApplyFilter()
    {
        Puzzles.Clear();
        foreach (var puzzle in _allPuzzles.Where(MatchesFilters))
            Puzzles.Add(puzzle);
    }

    private bool MatchesFilters(Puzzle puzzle) =>
        TextSearchFilter.Matches(puzzle.Name, SearchText) &&
        CategoryFilterOption.Matches(puzzle.Category) &&
        DateRangeFilter.Matches(puzzle.CompletedAt, FilterFromDate, FilterToDate);

    private void ClearHistoryFilters()
    {
        _searchText = string.Empty;
        _categoryFilterOption = CategoryFilterOptions[0];
        _filterFromDate = null;
        _filterToDate = null;
        OnPropertyChanged(nameof(SearchText));
        OnPropertyChanged(nameof(CategoryFilterOption));
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

    private void OpenPhoto(object? parameter)
    {
        var path = parameter switch
        {
            string filePath => filePath,
            PuzzlePhotoItem selected => selected.FilePath,
            PhotoPreviewItem preview => preview.FilePath,
            _ => null
        };

        if (string.IsNullOrWhiteSpace(path))
            return;

        _imagePreviewService.Show(path);
    }

    private ValidationResult ValidateRegisterForm() =>
        FormValidation.FirstFailure(
            FormValidation.RequireText(Name, "el nombre"),
            FormValidation.RequirePositiveInt(PieceCount, "La cantidad de piezas", out _),
            CompletedDate.HasValue
                ? ValidationResult.Ok()
                : ValidationResult.Fail("Indique la fecha de finalización."));

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

        var pieces = int.Parse(PieceCount);
        var photoPaths = SelectedPhotos.Select(photo => photo.FilePath).ToList();
        var completedAt = DateTimeHelper.ToUtcFromLocalDate(CompletedDate ?? DateTime.Today);

        await RunBusyAsync(async () =>
        {
            var result = await _puzzleService.RegisterCompletedAsync(
                Name, pieces, Category, photoPaths, completedAt);
            PublishAchievements(result.Events);
            await HobbyXp.RefreshAsync();

            _allPuzzles.Insert(0, result.Value);
            ApplyFilter();

            Name = string.Empty;
            PieceCount = "500";
            CompletedDate = DateTime.Today;
            SelectedPhotos.Clear();
            OnPropertyChanged(nameof(HasSelectedPhotos));
            ClearValidation();
            StatusMessage = $"Rompecabezas registrado · +{result.Value.XpEarned} XP";
        }, "Guardando rompecabezas...");
    }

    private void OpenDetail(object? parameter)
    {
        if (parameter is not Puzzle puzzle)
            return;

        var detailVm = new PuzzleDetailViewModel(puzzle, _puzzleService, _fileDialogService);
        var dialog = new PuzzleDetailWindow(detailVm)
        {
            Owner = Application.Current.MainWindow
        };

        var accepted = dialog.ShowDialog() == true;
        if (!accepted || detailVm.SavedPuzzle is null)
            return;

        var index = _allPuzzles.FindIndex(p => p.Id == detailVm.SavedPuzzle.Id);
        if (index >= 0)
            _allPuzzles[index] = detailVm.SavedPuzzle;
        else
            _allPuzzles.Insert(0, detailVm.SavedPuzzle);

        ApplyFilter();
        StatusMessage = $"Rompecabezas actualizado: {detailVm.SavedPuzzle.Name}";
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
            await HobbyXp.RefreshAsync();
            _profileRefreshMessenger.RequestRefresh();
            StatusMessage = $"«{puzzle.Name}» eliminado del historial.";
        }, "Eliminando rompecabezas...");
    }
}
