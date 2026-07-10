using System.Collections.ObjectModel;
using System.Windows.Input;
using HobbyXP.Helpers;
using HobbyXP.Models.Achievements;
using HobbyXP.Services.Abstractions;
using HobbyXP.ViewModels.Common;

namespace HobbyXP.ViewModels.Achievements;

public sealed class MedalsEditorViewModel : LoadableViewModelBase
{
    private readonly IMedalService _medalService;
    private readonly IFileDialogService _fileDialogService;
    private readonly MedalShowcaseViewModel _showcase;
    private MedalDefinition? _selectedMedal;

    public MedalsEditorViewModel(
        IMedalService medalService,
        IFileDialogService fileDialogService,
        MedalShowcaseViewModel showcase)
    {
        _medalService = medalService;
        _fileDialogService = fileDialogService;
        _showcase = showcase;
        Medals = new ObservableCollection<MedalDefinition>();

        SaveMedalCommand = new AsyncRelayCommand(SaveMedalAsync, CanSaveMedal);
        PickIconCommand = new RelayCommand(PickIcon, () => SelectedMedal is not null);
        ClearIconCommand = new RelayCommand(ClearIcon, () => SelectedMedal is not null && !string.IsNullOrWhiteSpace(EditIconPath));
    }

    public ObservableCollection<MedalDefinition> Medals { get; }

    public MedalDefinition? SelectedMedal
    {
        get => _selectedMedal;
        set
        {
            if (!SetProperty(ref _selectedMedal, value))
                return;

            ClearValidation();
            NotifyEditPropertiesChanged();
            RefreshMedalValidation();
            CommandManager.InvalidateRequerySuggested();
        }
    }

    public bool HasSelectedMedal => SelectedMedal is not null;

    public string SelectedMedalCategory =>
        SelectedMedal is null
            ? string.Empty
            : AchievementDisplayNames.ForMedalCode(SelectedMedal.Code);

    public string EditName
    {
        get => SelectedMedal?.Name ?? string.Empty;
        set
        {
            if (SelectedMedal is null || SelectedMedal.Name == value)
                return;

            SelectedMedal.Name = value;
            OnPropertyChanged();
            RefreshMedalValidation();
        }
    }

    public string EditDescription
    {
        get => SelectedMedal?.Description ?? string.Empty;
        set
        {
            if (SelectedMedal is null || SelectedMedal.Description == value)
                return;

            SelectedMedal.Description = value;
            OnPropertyChanged();
            RefreshMedalValidation();
        }
    }

    public string EditUnlockHint
    {
        get => SelectedMedal?.UnlockHint ?? string.Empty;
        set
        {
            if (SelectedMedal is null || SelectedMedal.UnlockHint == value)
                return;

            SelectedMedal.UnlockHint = value;
            OnPropertyChanged();
            RefreshMedalValidation();
        }
    }

    public string? EditIconPath
    {
        get => SelectedMedal?.IconPath;
        set
        {
            if (SelectedMedal is null || SelectedMedal.IconPath == value)
                return;

            SelectedMedal.IconPath = string.IsNullOrWhiteSpace(value) ? null : value.Trim();
            OnPropertyChanged();
            CommandManager.InvalidateRequerySuggested();
        }
    }

    public AsyncRelayCommand SaveMedalCommand { get; }

    public RelayCommand PickIconCommand { get; }

    public RelayCommand ClearIconCommand { get; }

    protected override async Task LoadCoreAsync()
    {
        var medals = await _medalService.GetAllDefinitionsAsync();
        Medals.Clear();
        foreach (var medal in medals)
            Medals.Add(medal);

        SelectedMedal ??= Medals.FirstOrDefault();
    }

    private ValidationResult ValidateSelectedMedal()
    {
        if (SelectedMedal is null)
            return ValidationResult.Fail("Seleccione una medalla.");

        return FormValidation.FirstFailure(
            FormValidation.RequireText(SelectedMedal.Name, "el nombre"),
            FormValidation.RequireText(SelectedMedal.Description, "la descripción"),
            FormValidation.RequireText(SelectedMedal.UnlockHint, "la pista de desbloqueo"));
    }

    private void RefreshMedalValidation() =>
        RefreshValidation(ValidateSelectedMedal(), SaveMedalCommand);

    private void NotifyEditPropertiesChanged()
    {
        OnPropertyChanged(nameof(HasSelectedMedal));
        OnPropertyChanged(nameof(SelectedMedalCategory));
        OnPropertyChanged(nameof(EditName));
        OnPropertyChanged(nameof(EditDescription));
        OnPropertyChanged(nameof(EditUnlockHint));
        OnPropertyChanged(nameof(EditIconPath));
    }

    private bool CanSaveMedal() => SelectedMedal is not null && ValidateSelectedMedal().IsValid;

    private void PickIcon()
    {
        if (SelectedMedal is null)
            return;

        var path = _fileDialogService.PickImageFile();
        if (path is null)
            return;

        EditIconPath = path;
    }

    private void ClearIcon() => EditIconPath = null;

    private async Task SaveMedalAsync()
    {
        if (!ValidateSelectedMedal().IsValid)
        {
            RefreshMedalValidation();
            return;
        }

        if (SelectedMedal is null)
            return;

        await RunBusyAsync(async () =>
        {
            var updated = await _medalService.UpdateDefinitionAsync(SelectedMedal);
            var index = Medals.IndexOf(SelectedMedal);
            if (index >= 0)
                Medals[index] = updated;

            SelectedMedal = updated;
            ClearValidation();
            await _showcase.LoadAsync();
            StatusMessage = $"Medalla '{updated.Name}' actualizada.";
        }, "Guardando medalla...");
    }
}
