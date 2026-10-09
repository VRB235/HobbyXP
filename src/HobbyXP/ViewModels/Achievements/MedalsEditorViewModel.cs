using System.Collections.ObjectModel;
using System.Windows.Input;
using HobbyXP.Helpers;
using HobbyXP.Models.Achievements;
using HobbyXP.Models.Enums;
using HobbyXP.Services.Abstractions;
using HobbyXP.ViewModels.Common;

namespace HobbyXP.ViewModels.Achievements;

public sealed class MedalsEditorViewModel : LoadableViewModelBase
{
    private readonly IMedalService _medalService;
    private readonly IFileDialogService _fileDialogService;
    private readonly MedalShowcaseViewModel _showcase;
    private MedalDefinition? _selectedMedal;
    private bool _isCreateMode;
    private MilestoneSourceType _editSourceType = MilestoneSourceType.Running;
    private string _editXpThresholdText = "100";
    private string _editName = string.Empty;
    private string _editDescription = string.Empty;
    private string _editUnlockHint = string.Empty;
    private string? _editIconPath;
    private string? _selectedNameSuggestion;

    public MedalsEditorViewModel(
        IMedalService medalService,
        IFileDialogService fileDialogService,
        MedalShowcaseViewModel showcase)
    {
        _medalService = medalService;
        _fileDialogService = fileDialogService;
        _showcase = showcase;
        Medals = new ObservableCollection<MedalDefinition>();
        NameSuggestions = new ObservableCollection<string>();
        HobbyOptions = HobbyProgressCatalog.TrackedHobbies
            .Select(h => new HobbyOption(h, HobbyProgressCatalog.GetDisplayName(h)))
            .ToList();

        BeginCreateCommand = new RelayCommand(BeginCreate);
        CancelCreateCommand = new RelayCommand(CancelCreate, () => IsCreateMode);
        SaveMedalCommand = new AsyncRelayCommand(SaveMedalAsync, CanSaveMedal);
        DeleteMedalCommand = new AsyncRelayCommand(DeleteMedalAsync, () => !IsCreateMode && SelectedMedal is not null);
        PickIconCommand = new RelayCommand(PickIcon);
        ClearIconCommand = new RelayCommand(ClearIcon, () => !string.IsNullOrWhiteSpace(EditIconPath));
        RefreshSuggestionsCommand = new RelayCommand(RefreshSuggestions);
    }

    public ObservableCollection<MedalDefinition> Medals { get; }

    public ObservableCollection<string> NameSuggestions { get; }

    public IReadOnlyList<HobbyOption> HobbyOptions { get; }

    public sealed record HobbyOption(MilestoneSourceType Value, string Label);

    public MedalDefinition? SelectedMedal
    {
        get => _selectedMedal;
        set
        {
            if (!SetProperty(ref _selectedMedal, value))
                return;

            if (IsCreateMode)
                return;

            LoadFromSelection();
            CommandManager.InvalidateRequerySuggested();
        }
    }

    public bool HasSelectedMedal => IsCreateMode || SelectedMedal is not null;

    public bool IsCreateMode
    {
        get => _isCreateMode;
        private set
        {
            if (!SetProperty(ref _isCreateMode, value))
                return;

            OnPropertyChanged(nameof(HasSelectedMedal));
            OnPropertyChanged(nameof(EditorTitle));
            OnPropertyChanged(nameof(SaveButtonLabel));
            OnPropertyChanged(nameof(CanEditHobbyAndThreshold));
            CommandManager.InvalidateRequerySuggested();
        }
    }

    public string EditorTitle => IsCreateMode ? "Nueva medalla" : "Detalle editable";

    public string SaveButtonLabel => IsCreateMode ? "Crear medalla" : "Guardar medalla";

    public bool CanEditHobbyAndThreshold => IsCreateMode || SelectedMedal is not null;

    public MilestoneSourceType EditSourceType
    {
        get => _editSourceType;
        set
        {
            if (!SetProperty(ref _editSourceType, value))
                return;

            RefreshSuggestions();
            if (string.IsNullOrWhiteSpace(EditIconPath) || IsDefaultHobbyIcon(EditIconPath))
                EditIconPath = MedalIconPaths.ForHobby(value);

            RefreshMedalValidation();
        }
    }

    public string EditXpThresholdText
    {
        get => _editXpThresholdText;
        set
        {
            if (!SetProperty(ref _editXpThresholdText, value))
                return;

            RefreshSuggestions();
            RefreshMedalValidation();
        }
    }

    public string EditName
    {
        get => _editName;
        set
        {
            if (!SetProperty(ref _editName, value))
                return;

            RefreshMedalValidation();
        }
    }

    public string? SelectedNameSuggestion
    {
        get => _selectedNameSuggestion;
        set
        {
            if (!SetProperty(ref _selectedNameSuggestion, value))
                return;

            if (!string.IsNullOrWhiteSpace(value))
                EditName = value;
        }
    }

    public string EditDescription
    {
        get => _editDescription;
        set
        {
            if (!SetProperty(ref _editDescription, value))
                return;

            RefreshMedalValidation();
        }
    }

    public string EditUnlockHint
    {
        get => _editUnlockHint;
        set
        {
            if (!SetProperty(ref _editUnlockHint, value))
                return;

            RefreshMedalValidation();
        }
    }

    public string? EditIconPath
    {
        get => _editIconPath;
        set
        {
            if (!SetProperty(ref _editIconPath, string.IsNullOrWhiteSpace(value) ? null : value.Trim()))
                return;

            CommandManager.InvalidateRequerySuggested();
        }
    }

    public string HobbyLabel => HobbyProgressCatalog.GetDisplayName(EditSourceType);

    public RelayCommand BeginCreateCommand { get; }

    public RelayCommand CancelCreateCommand { get; }

    public AsyncRelayCommand SaveMedalCommand { get; }

    public AsyncRelayCommand DeleteMedalCommand { get; }

    public RelayCommand PickIconCommand { get; }

    public RelayCommand ClearIconCommand { get; }

    public RelayCommand RefreshSuggestionsCommand { get; }

    protected override async Task LoadCoreAsync()
    {
        var medals = await _medalService.GetAllDefinitionsAsync();
        Medals.Clear();
        foreach (var medal in medals)
            Medals.Add(medal);

        if (IsCreateMode)
            return;

        SelectedMedal = Medals.FirstOrDefault(m => m.Id == SelectedMedal?.Id) ?? Medals.FirstOrDefault();
        if (SelectedMedal is null)
            BeginCreate();
        else
            LoadFromSelection();
    }

    private void BeginCreate()
    {
        IsCreateMode = true;
        _selectedMedal = null;
        OnPropertyChanged(nameof(SelectedMedal));

        EditSourceType = MilestoneSourceType.Running;
        EditXpThresholdText = "100";
        EditDescription = "Alcanza el umbral de XP en este hobby.";
        EditUnlockHint = "Gana XP en este módulo hasta llegar al umbral.";
        EditIconPath = MedalIconPaths.ForHobby(EditSourceType);
        RefreshSuggestions();
        EditName = NameSuggestions.FirstOrDefault() ?? "Nueva medalla";
        ClearValidation();
        RefreshMedalValidation();
        OnPropertyChanged(nameof(HasSelectedMedal));
        CommandManager.InvalidateRequerySuggested();
    }

    private void CancelCreate()
    {
        IsCreateMode = false;
        if (SelectedMedal is null)
            SelectedMedal = Medals.FirstOrDefault();
        else
            LoadFromSelection();
    }

    private void LoadFromSelection()
    {
        if (SelectedMedal is null)
        {
            ClearValidation();
            return;
        }

        IsCreateMode = false;
        EditSourceType = SelectedMedal.SourceType;
        EditXpThresholdText = SelectedMedal.XpThreshold.ToString();
        EditName = SelectedMedal.Name;
        EditDescription = SelectedMedal.Description;
        EditUnlockHint = SelectedMedal.UnlockHint;
        EditIconPath = SelectedMedal.IconPath;
        RefreshSuggestions();
        ClearValidation();
        RefreshMedalValidation();
        OnPropertyChanged(nameof(HasSelectedMedal));
        OnPropertyChanged(nameof(HobbyLabel));
    }

    private void RefreshSuggestions()
    {
        var threshold = TryParseThreshold(out var xp) ? xp : 100;
        var suggestions = _medalService.SuggestNames(EditSourceType, threshold);
        NameSuggestions.Clear();
        foreach (var suggestion in suggestions)
            NameSuggestions.Add(suggestion);

        OnPropertyChanged(nameof(HobbyLabel));
    }

    private ValidationResult ValidateEditor()
    {
        if (!TryParseThreshold(out var xp) || xp < 1)
            return ValidationResult.Fail("Indique un umbral de XP válido (entero ≥ 1).");

        return FormValidation.FirstFailure(
            FormValidation.RequireText(EditName, "el nombre"),
            FormValidation.RequireText(EditDescription, "la descripción"),
            FormValidation.RequireText(EditUnlockHint, "la pista de desbloqueo"));
    }

    private void RefreshMedalValidation() =>
        RefreshValidation(ValidateEditor(), SaveMedalCommand);

    private bool CanSaveMedal() => HasSelectedMedal && ValidateEditor().IsValid;

    private bool TryParseThreshold(out int xp) =>
        int.TryParse(EditXpThresholdText.Replace(",", "").Trim(), out xp);

    private void PickIcon()
    {
        var path = _fileDialogService.PickImageFile();
        if (path is null)
            return;

        EditIconPath = path;
    }

    private void ClearIcon() =>
        EditIconPath = MedalIconPaths.ForHobby(EditSourceType);

    private static bool IsDefaultHobbyIcon(string? path) =>
        !string.IsNullOrWhiteSpace(path)
        && HobbyProgressCatalog.TrackedHobbies.Any(h =>
            string.Equals(path, MedalIconPaths.ForHobby(h), StringComparison.OrdinalIgnoreCase));

    private async Task SaveMedalAsync()
    {
        if (!ValidateEditor().IsValid || !TryParseThreshold(out var xp))
        {
            RefreshMedalValidation();
            return;
        }

        await RunBusyAsync(async () =>
        {
            if (IsCreateMode)
            {
                var created = await _medalService.CreateAsync(
                    EditSourceType,
                    xp,
                    EditName,
                    EditDescription,
                    EditUnlockHint,
                    EditIconPath);

                Medals.Add(created);
                IsCreateMode = false;
                SelectedMedal = created;
                StatusMessage = $"Medalla '{created.Name}' creada.";
            }
            else if (SelectedMedal is not null)
            {
                SelectedMedal.SourceType = EditSourceType;
                SelectedMedal.XpThreshold = xp;
                SelectedMedal.Name = EditName;
                SelectedMedal.Description = EditDescription;
                SelectedMedal.UnlockHint = EditUnlockHint;
                SelectedMedal.IconPath = EditIconPath;

                var updated = await _medalService.UpdateDefinitionAsync(SelectedMedal);
                var index = Medals.IndexOf(SelectedMedal);
                if (index >= 0)
                    Medals[index] = updated;

                SelectedMedal = updated;
                StatusMessage = $"Medalla '{updated.Name}' actualizada.";
            }

            ClearValidation();
            await _showcase.LoadAsync();
        }, IsCreateMode ? "Creando medalla..." : "Guardando medalla...");
    }

    private async Task DeleteMedalAsync()
    {
        if (SelectedMedal is null || IsCreateMode)
            return;

        var toDelete = SelectedMedal;
        await RunBusyAsync(async () =>
        {
            await _medalService.DeleteAsync(toDelete.Id);
            Medals.Remove(toDelete);
            SelectedMedal = Medals.FirstOrDefault();
            if (SelectedMedal is null)
                BeginCreate();

            await _showcase.LoadAsync();
            StatusMessage = $"Medalla '{toDelete.Name}' eliminada.";
        }, "Eliminando medalla...");
    }
}
