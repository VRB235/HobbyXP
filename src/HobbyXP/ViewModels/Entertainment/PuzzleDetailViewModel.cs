using System.Windows.Input;
using HobbyXP.Helpers;
using HobbyXP.Models.Entertainment;
using HobbyXP.Models.Enums;
using HobbyXP.Services.Abstractions;
using HobbyXP.ViewModels.Common;

namespace HobbyXP.ViewModels.Entertainment;

public sealed class PuzzleDetailViewModel : ViewModelBase
{
    private readonly IPuzzleService _puzzleService;
    private readonly IFileDialogService _fileDialogService;
    private readonly Puzzle _original;

    private string _name;
    private string _pieceCount;
    private PuzzleCategory _category;
    private DateTime? _completedDate;
    private string? _validationMessage;
    private string? _errorMessage;
    private bool _isBusy;
    private bool _photosChanged;

    public PuzzleDetailViewModel(
        Puzzle puzzle,
        IPuzzleService puzzleService,
        IFileDialogService fileDialogService)
    {
        _puzzleService = puzzleService;
        _fileDialogService = fileDialogService;
        _original = puzzle;

        _name = puzzle.Name;
        _pieceCount = puzzle.PieceCount.ToString();
        _category = puzzle.Category;
        _completedDate = puzzle.CompletedAt.ToLocalTime().Date;

        Photos = new System.Collections.ObjectModel.ObservableCollection<PuzzlePhotoItem>();
        foreach (var path in PuzzlePhotoStorage.Deserialize(puzzle.PhotoPath))
        {
            var item = PuzzlePhotoItem.TryCreate(path);
            if (item is not null)
                Photos.Add(item);
        }

        SaveCommand = new AsyncRelayCommand(SaveAsync, () => !IsBusy && CanSave());
        CancelCommand = new RelayCommand(() => RequestClose?.Invoke(false), () => !IsBusy);
        PickPhotosCommand = new RelayCommand(PickPhotos, () => !IsBusy);
        RemovePhotoCommand = new RelayCommand(RemovePhoto, _ => !IsBusy);
        RefreshValidation();
    }

    public event Action<bool>? RequestClose;

    public System.Collections.ObjectModel.ObservableCollection<PuzzlePhotoItem> Photos { get; }

    public bool HasPhotos => Photos.Count > 0;

    public Array Categories => Enum.GetValues(typeof(PuzzleCategory));

    public string Name
    {
        get => _name;
        set
        {
            if (SetProperty(ref _name, value))
                RefreshValidation();
        }
    }

    public string PieceCount
    {
        get => _pieceCount;
        set
        {
            if (SetProperty(ref _pieceCount, value))
                RefreshValidation();
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
                RefreshValidation();
        }
    }

    public int XpEarned => _original.XpEarned;

    public string? ValidationMessage
    {
        get => _validationMessage;
        private set => SetProperty(ref _validationMessage, value);
    }

    public string? ErrorMessage
    {
        get => _errorMessage;
        private set => SetProperty(ref _errorMessage, value);
    }

    public bool IsBusy
    {
        get => _isBusy;
        private set
        {
            if (!SetProperty(ref _isBusy, value))
                return;
            SaveCommand.RaiseCanExecuteChanged();
            CommandManager.InvalidateRequerySuggested();
        }
    }

    public Puzzle? SavedPuzzle { get; private set; }

    public AsyncRelayCommand SaveCommand { get; }
    public RelayCommand CancelCommand { get; }
    public RelayCommand PickPhotosCommand { get; }
    public RelayCommand RemovePhotoCommand { get; }

    private bool CanSave() => ValidateForm().IsValid;

    private ValidationResult ValidateForm() =>
        FormValidation.FirstFailure(
            FormValidation.RequireText(Name, "el nombre"),
            FormValidation.RequirePositiveInt(PieceCount, "La cantidad de piezas", out _),
            CompletedDate.HasValue
                ? ValidationResult.Ok()
                : ValidationResult.Fail("Indique la fecha de finalización."));

    private void RefreshValidation()
    {
        var result = ValidateForm();
        ValidationMessage = result.IsValid ? null : result.Message;
        SaveCommand.RaiseCanExecuteChanged();
    }

    private void PickPhotos()
    {
        var paths = _fileDialogService.PickImageFiles();
        if (paths.Count == 0)
            return;

        var existing = Photos.Select(p => p.FilePath).ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var path in paths)
        {
            if (existing.Contains(path))
                continue;
            var item = PuzzlePhotoItem.TryCreate(path);
            if (item is null)
                continue;
            Photos.Add(item);
            existing.Add(path);
            _photosChanged = true;
        }

        OnPropertyChanged(nameof(HasPhotos));
    }

    private void RemovePhoto(object? parameter)
    {
        if (parameter is not PuzzlePhotoItem photo)
            return;
        Photos.Remove(photo);
        _photosChanged = true;
        OnPropertyChanged(nameof(HasPhotos));
    }

    private async Task SaveAsync()
    {
        if (!ValidateForm().IsValid)
        {
            RefreshValidation();
            return;
        }

        FormValidation.RequirePositiveInt(PieceCount, "La cantidad de piezas", out var pieces);
        IsBusy = true;
        ErrorMessage = null;

        try
        {
            var completedAt = DateTimeHelper.ToUtcFromLocalDate(CompletedDate ?? DateTime.Today);
            var photoPaths = Photos.Select(p => p.FilePath).ToList();
            SavedPuzzle = await _puzzleService.UpdateAsync(
                _original.Id,
                Name.Trim(),
                pieces,
                Category,
                completedAt,
                photoPaths,
                replacePhotos: _photosChanged);
            RequestClose?.Invoke(true);
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }
}
