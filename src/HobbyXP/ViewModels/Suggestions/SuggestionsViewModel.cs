using System.Collections.ObjectModel;
using HobbyXP.Helpers;
using HobbyXP.Models.Enums;
using HobbyXP.Models.Feedback;
using HobbyXP.Services.Abstractions;
using HobbyXP.Services.Messaging;
using HobbyXP.ViewModels.Common;

namespace HobbyXP.ViewModels.Suggestions;

public sealed class SuggestionsViewModel : LoadableViewModelBase
{
    private readonly ISuggestionService _suggestionService;
    private readonly IFileDialogService _fileDialogService;
    private readonly IMessageDialogService _messageDialogService;
    private readonly IImagePreviewService _imagePreviewService;
    private string _title = string.Empty;
    private string _description = string.Empty;
    private SuggestionKind _kind = SuggestionKind.Improvement;
    private DateTime? _reportedDate = DateTime.Today;
    private string _searchText = string.Empty;
    private EnumFilterOption<SuggestionKind> _kindFilterOption;
    private EnumFilterOption<SuggestionStatus> _statusFilterOption;
    private DateTime? _filterFromDate;
    private DateTime? _filterToDate;
    private List<Suggestion> _allSuggestions = [];

    public SuggestionsViewModel(
        ISuggestionService suggestionService,
        IFileDialogService fileDialogService,
        IMessageDialogService messageDialogService,
        IImagePreviewService imagePreviewService)
    {
        _suggestionService = suggestionService;
        _fileDialogService = fileDialogService;
        _messageDialogService = messageDialogService;
        _imagePreviewService = imagePreviewService;

        Suggestions = new ObservableCollection<Suggestion>();
        SelectedPhotos = new ObservableCollection<SuggestionPhotoItem>();

        KindFilterOptions = EnumFilterOption<SuggestionKind>.Create(
            "Todos los tipos",
            SuggestionDisplayLabels.GetKind);
        _kindFilterOption = KindFilterOptions[0];

        StatusFilterOptions = EnumFilterOption<SuggestionStatus>.Create(
            "Todos los estados",
            SuggestionDisplayLabels.GetStatus);
        _statusFilterOption = StatusFilterOptions[0];

        RegisterCommand = new AsyncRelayCommand(RegisterAsync, CanRegister);
        PickPhotosCommand = new RelayCommand(PickPhotos);
        RemovePhotoCommand = new RelayCommand(RemovePhoto);
        OpenPhotoCommand = new RelayCommand(OpenPhoto);
        ClearFiltersCommand = new RelayCommand(ClearHistoryFilters);
        ToggleResolvedCommand = new AsyncRelayCommand(ToggleResolvedAsync);
        DeleteSuggestionCommand = new AsyncRelayCommand(DeleteSuggestionAsync);
        RefreshRegisterValidation();
    }

    public ObservableCollection<Suggestion> Suggestions { get; }

    public ObservableCollection<SuggestionPhotoItem> SelectedPhotos { get; }

    public Array Kinds => Enum.GetValues(typeof(SuggestionKind));

    public IReadOnlyList<EnumFilterOption<SuggestionKind>> KindFilterOptions { get; }

    public IReadOnlyList<EnumFilterOption<SuggestionStatus>> StatusFilterOptions { get; }

    public string Title
    {
        get => _title;
        set
        {
            if (SetProperty(ref _title, value))
                RefreshRegisterValidation();
        }
    }

    public string Description
    {
        get => _description;
        set
        {
            if (SetProperty(ref _description, value))
                RefreshRegisterValidation();
        }
    }

    public SuggestionKind Kind
    {
        get => _kind;
        set => SetProperty(ref _kind, value);
    }

    public DateTime? ReportedDate
    {
        get => _reportedDate;
        set
        {
            if (SetProperty(ref _reportedDate, value))
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

    public EnumFilterOption<SuggestionKind> KindFilterOption
    {
        get => _kindFilterOption;
        set
        {
            if (SetProperty(ref _kindFilterOption, value))
                ApplyFilter();
        }
    }

    public EnumFilterOption<SuggestionStatus> StatusFilterOption
    {
        get => _statusFilterOption;
        set
        {
            if (SetProperty(ref _statusFilterOption, value))
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

    public RelayCommand ClearFiltersCommand { get; }

    public AsyncRelayCommand ToggleResolvedCommand { get; }

    public AsyncRelayCommand DeleteSuggestionCommand { get; }

    public bool HasSelectedPhotos => SelectedPhotos.Count > 0;

    protected override async Task LoadCoreAsync()
    {
        _allSuggestions = (await _suggestionService.GetAllAsync()).ToList();
        ApplyFilter();
    }

    private void ApplyFilter()
    {
        Suggestions.Clear();
        foreach (var suggestion in _allSuggestions.Where(MatchesFilters))
            Suggestions.Add(suggestion);
    }

    private bool MatchesFilters(Suggestion suggestion) =>
        (TextSearchFilter.Matches(suggestion.Title, SearchText) ||
         TextSearchFilter.Matches(suggestion.Description, SearchText)) &&
        KindFilterOption.Matches(suggestion.Kind) &&
        StatusFilterOption.Matches(suggestion.Status) &&
        DateRangeFilter.Matches(suggestion.ReportedAt, FilterFromDate, FilterToDate);

    private void ClearHistoryFilters()
    {
        _searchText = string.Empty;
        _kindFilterOption = KindFilterOptions[0];
        _statusFilterOption = StatusFilterOptions[0];
        _filterFromDate = null;
        _filterToDate = null;
        OnPropertyChanged(nameof(SearchText));
        OnPropertyChanged(nameof(KindFilterOption));
        OnPropertyChanged(nameof(StatusFilterOption));
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

            var item = SuggestionPhotoItem.TryCreate(path);
            if (item is null)
                continue;

            SelectedPhotos.Add(item);
            existing.Add(path);
        }

        OnPropertyChanged(nameof(HasSelectedPhotos));
    }

    private void RemovePhoto(object? parameter)
    {
        if (parameter is not SuggestionPhotoItem photo)
            return;

        SelectedPhotos.Remove(photo);
        OnPropertyChanged(nameof(HasSelectedPhotos));
    }

    private void OpenPhoto(object? parameter)
    {
        var path = parameter switch
        {
            string filePath => filePath,
            SuggestionPhotoItem selected => selected.FilePath,
            PhotoPreviewItem preview => preview.FilePath,
            _ => null
        };

        if (string.IsNullOrWhiteSpace(path))
            return;

        _imagePreviewService.Show(path);
    }

    private ValidationResult ValidateRegisterForm() =>
        FormValidation.FirstFailure(
            FormValidation.RequireText(Title, "el título"),
            FormValidation.RequireText(Description, "la descripción"),
            ReportedDate.HasValue
                ? ValidationResult.Ok()
                : ValidationResult.Fail("Indique la fecha del reporte."));

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

        var photoPaths = SelectedPhotos.Select(photo => photo.FilePath).ToList();
        var reportedAt = DateTimeHelper.ToUtcFromLocalDate(ReportedDate ?? DateTime.Today);

        await RunBusyAsync(async () =>
        {
            var result = await _suggestionService.CreateAsync(
                Title, Description, Kind, photoPaths, reportedAt);

            _allSuggestions.Insert(0, result.Value);
            ApplyFilter();

            Title = string.Empty;
            Description = string.Empty;
            Kind = SuggestionKind.Improvement;
            ReportedDate = DateTime.Today;
            SelectedPhotos.Clear();
            OnPropertyChanged(nameof(HasSelectedPhotos));
            ClearValidation();
            StatusMessage = $"Sugerencia registrada: «{result.Value.Title}».";
        }, "Guardando sugerencia...");
    }

    private async Task ToggleResolvedAsync(object? parameter)
    {
        if (parameter is not Suggestion suggestion)
            return;

        var markResolved = suggestion.Status != SuggestionStatus.Resolved;

        await RunBusyAsync(async () =>
        {
            var result = await _suggestionService.SetResolvedAsync(suggestion.Id, markResolved);
            ReplaceInCache(result.Value);
            ApplyFilter();
            StatusMessage = markResolved
                ? $"«{result.Value.Title}» marcada como resuelta."
                : $"«{result.Value.Title}» reabierta.";
        }, markResolved ? "Marcando como resuelta..." : "Reabriendo...");
    }

    private async Task DeleteSuggestionAsync(object? parameter)
    {
        if (parameter is not Suggestion suggestion)
            return;

        if (!_messageDialogService.Confirm(
                $"¿Eliminar «{suggestion.Title}»?\nTambién se borrarán las imágenes asociadas.",
                "Eliminar sugerencia"))
            return;

        await RunBusyAsync(async () =>
        {
            if (!await _suggestionService.DeleteAsync(suggestion.Id))
                return;

            _allSuggestions.RemoveAll(s => s.Id == suggestion.Id);
            ApplyFilter();
            StatusMessage = $"«{suggestion.Title}» eliminada.";
        }, "Eliminando sugerencia...");
    }

    private void ReplaceInCache(Suggestion updated)
    {
        var index = _allSuggestions.FindIndex(s => s.Id == updated.Id);
        if (index >= 0)
            _allSuggestions[index] = updated;
    }
}
