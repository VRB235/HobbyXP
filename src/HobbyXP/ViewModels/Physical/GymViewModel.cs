using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Windows.Data;
using System.Windows.Input;
using HobbyXP.Helpers;
using HobbyXP.Models.Enums;
using HobbyXP.Models.Physical;
using HobbyXP.Services.Abstractions;
using HobbyXP.Services.Messaging;
using HobbyXP.ViewModels.Common;
using HobbyXP.ViewModels.Messaging;

namespace HobbyXP.ViewModels.Physical;

public sealed class GymViewModel : AchievementAwareViewModel
{
    private readonly IGymService _gymService;
    private readonly IMessageDialogService _messageDialogService;
    private readonly IProfileRefreshMessenger _profileRefreshMessenger;
    private readonly CollectionViewSource _catalogExercisesViewSource = new();
    private string _newExerciseName = string.Empty;
    private ExerciseTypeOption _newExerciseTypeOption;
    private MuscleGroupOption _newMuscleGroupOption;
    private MuscleGroupOption _exerciseFilterOption;
    private Exercise? _selectedCatalogExercise;
    private MuscleGroupOption _editMuscleGroupOption;
    private MuscleGroupOption _historyMuscleGroupFilterOption;
    private bool _isCatalogExpanded;
    private bool _isWorkoutExpanded = true;
    private bool _isHistoryExpanded;
    private bool _suppressSectionAccordion;
    private GymWorkout? _selectedWorkout;
    private DateTime? _historyFromDate;
    private DateTime? _historyToDate;
    private List<GymWorkout> _allWorkouts = [];
    private string? _exerciseValidationMessage;

    public GymViewModel(
        IGymService gymService,
        IMessageDialogService messageDialogService,
        IProfileRefreshMessenger profileRefreshMessenger,
        IAchievementMessenger achievementMessenger)
        : base(achievementMessenger)
    {
        _gymService = gymService;
        _messageDialogService = messageDialogService;
        _profileRefreshMessenger = profileRefreshMessenger;

        MuscleGroupCatalogOptions = MuscleGroupOption.CreateCatalogOptions();
        MuscleGroupFilterOptions = MuscleGroupOption.CreateFilterOptions();
        ExerciseTypeOptions = ExerciseTypeOption.All;
        _newMuscleGroupOption = MuscleGroupCatalogOptions[0];
        _exerciseFilterOption = MuscleGroupFilterOptions[0];
        _editMuscleGroupOption = MuscleGroupCatalogOptions[0];
        _historyMuscleGroupFilterOption = MuscleGroupFilterOptions[0];
        _newExerciseTypeOption = ExerciseTypeOptions[0];

        Exercises = new ObservableCollection<Exercise>();
        FilteredExercises = new ObservableCollection<Exercise>();
        Entries = new ObservableCollection<GymEntryRowViewModel>();
        History = new ObservableCollection<GymWorkout>();

        _catalogExercisesViewSource.Source = Exercises;
        _catalogExercisesViewSource.SortDescriptions.Add(
            new SortDescription(nameof(Exercise.MuscleGroupSortOrder), ListSortDirection.Ascending));
        _catalogExercisesViewSource.SortDescriptions.Add(
            new SortDescription(nameof(Exercise.Name), ListSortDirection.Ascending));
        _catalogExercisesViewSource.GroupDescriptions.Add(
            new PropertyGroupDescription(nameof(Exercise.MuscleGroupLabel)));

        AddRowCommand = new RelayCommand(AddRow);
        RemoveRowCommand = new RelayCommand(RemoveRow, _ => Entries.Count > 0);
        SaveWorkoutCommand = new AsyncRelayCommand(SaveWorkoutAsync, CanSaveWorkout);
        CreateExerciseCommand = new AsyncRelayCommand(CreateExerciseAsync, CanCreateExercise);
        UpdateCatalogMuscleGroupCommand = new AsyncRelayCommand(
            UpdateCatalogMuscleGroupAsync,
            CanUpdateCatalogMuscleGroup);
        ClearHistoryDateFilterCommand = new RelayCommand(ClearHistoryDateFilter);
        DeleteWorkoutCommand = new AsyncRelayCommand(p => DeleteWorkoutAsync(p));

        Entries.CollectionChanged += OnEntriesCollectionChanged;
        AddRow();
        RefreshWorkoutValidation();
        RefreshExerciseValidation();
    }

    public string? ExerciseValidationMessage
    {
        get => _exerciseValidationMessage;
        private set => SetProperty(ref _exerciseValidationMessage, value);
    }

    public ObservableCollection<Exercise> Exercises { get; }

    /// <summary>
    /// Lista plana filtrada para el ComboBox de entrenamiento (evita bug de ICollectionView agrupada).
    /// </summary>
    public ObservableCollection<Exercise> FilteredExercises { get; }

    public ICollectionView CatalogExercisesView => _catalogExercisesViewSource.View;

    public ObservableCollection<GymEntryRowViewModel> Entries { get; }

    public ObservableCollection<GymWorkout> History { get; }

    public IReadOnlyList<MuscleGroupOption> MuscleGroupCatalogOptions { get; }

    public IReadOnlyList<MuscleGroupOption> MuscleGroupFilterOptions { get; }

    public IReadOnlyList<ExerciseTypeOption> ExerciseTypeOptions { get; }

    public GymWorkout? SelectedWorkout
    {
        get => _selectedWorkout;
        set => SetProperty(ref _selectedWorkout, value);
    }

    public Exercise? SelectedCatalogExercise
    {
        get => _selectedCatalogExercise;
        set
        {
            if (!SetProperty(ref _selectedCatalogExercise, value))
                return;

            EditMuscleGroupOption = MuscleGroupCatalogOptions.First(o =>
                !o.MatchesUnassignedOnly && o.Value == value?.MuscleGroup);
            UpdateCatalogMuscleGroupCommand.RaiseCanExecuteChanged();
        }
    }

    public DateTime? HistoryFromDate
    {
        get => _historyFromDate;
        set
        {
            if (SetProperty(ref _historyFromDate, value))
                ApplyHistoryFilter();
        }
    }

    public DateTime? HistoryToDate
    {
        get => _historyToDate;
        set
        {
            if (SetProperty(ref _historyToDate, value))
                ApplyHistoryFilter();
        }
    }

    public string NewExerciseName
    {
        get => _newExerciseName;
        set
        {
            if (SetProperty(ref _newExerciseName, value))
                RefreshExerciseValidation();
        }
    }

    public ExerciseTypeOption NewExerciseTypeOption
    {
        get => _newExerciseTypeOption;
        set => SetProperty(ref _newExerciseTypeOption, value);
    }

    public MuscleGroupOption NewMuscleGroupOption
    {
        get => _newMuscleGroupOption;
        set => SetProperty(ref _newMuscleGroupOption, value);
    }

    public MuscleGroupOption ExerciseFilterOption
    {
        get => _exerciseFilterOption;
        set
        {
            if (SetProperty(ref _exerciseFilterOption, value))
                RebuildFilteredExercises();
        }
    }

    public MuscleGroupOption HistoryMuscleGroupFilterOption
    {
        get => _historyMuscleGroupFilterOption;
        set
        {
            if (SetProperty(ref _historyMuscleGroupFilterOption, value))
                ApplyHistoryFilter();
        }
    }

    public bool IsCatalogExpanded
    {
        get => _isCatalogExpanded;
        set => SetSectionExpanded(GymSection.Catalog, value, ref _isCatalogExpanded, nameof(IsCatalogExpanded));
    }

    public bool IsWorkoutExpanded
    {
        get => _isWorkoutExpanded;
        set => SetSectionExpanded(GymSection.Workout, value, ref _isWorkoutExpanded, nameof(IsWorkoutExpanded));
    }

    public bool IsHistoryExpanded
    {
        get => _isHistoryExpanded;
        set => SetSectionExpanded(GymSection.History, value, ref _isHistoryExpanded, nameof(IsHistoryExpanded));
    }

    public MuscleGroupOption EditMuscleGroupOption
    {
        get => _editMuscleGroupOption;
        set
        {
            if (SetProperty(ref _editMuscleGroupOption, value))
                UpdateCatalogMuscleGroupCommand.RaiseCanExecuteChanged();
        }
    }

    public RelayCommand AddRowCommand { get; }

    public RelayCommand RemoveRowCommand { get; }

    public AsyncRelayCommand SaveWorkoutCommand { get; }

    public AsyncRelayCommand CreateExerciseCommand { get; }

    public AsyncRelayCommand UpdateCatalogMuscleGroupCommand { get; }

    public RelayCommand ClearHistoryDateFilterCommand { get; }

    public AsyncRelayCommand DeleteWorkoutCommand { get; }

    public async Task LoadDataAsync()
    {
        var exercises = await _gymService.GetExercisesAsync();
        Exercises.Clear();
        foreach (var exercise in exercises)
            Exercises.Add(exercise);

        RebuildFilteredExercises();
        CatalogExercisesView.Refresh();
        await LoadHistoryAsync();
    }

    private void RebuildFilteredExercises()
    {
        FilteredExercises.Clear();
        foreach (var exercise in Exercises
                     .Where(e => ExerciseFilterOption.Matches(e.MuscleGroup))
                     .OrderBy(e => e.MuscleGroupSortOrder)
                     .ThenBy(e => e.Name))
        {
            FilteredExercises.Add(exercise);
        }
    }

    private async Task LoadHistoryAsync()
    {
        _allWorkouts = (await _gymService.GetWorkoutHistoryAsync()).ToList();
        ApplyHistoryFilter();
    }

    private void ApplyHistoryFilter()
    {
        var selectedId = SelectedWorkout?.Id;

        History.Clear();
        foreach (var workout in _allWorkouts.Where(MatchesHistoryFilters))
            History.Add(workout);

        SelectedWorkout = selectedId.HasValue
            ? History.FirstOrDefault(w => w.Id == selectedId.Value)
            : History.FirstOrDefault();
    }

    private bool MatchesHistoryFilters(GymWorkout workout) =>
        DateRangeFilter.Matches(workout.WorkoutDate, HistoryFromDate, HistoryToDate) &&
        HistoryMuscleGroupFilterOption.MatchesWorkout(workout);

    private void ClearHistoryDateFilter()
    {
        _historyFromDate = null;
        _historyToDate = null;
        _historyMuscleGroupFilterOption = MuscleGroupFilterOptions[0];
        OnPropertyChanged(nameof(HistoryFromDate));
        OnPropertyChanged(nameof(HistoryToDate));
        OnPropertyChanged(nameof(HistoryMuscleGroupFilterOption));
        ApplyHistoryFilter();
    }

    private void SetSectionExpanded(GymSection section, bool isExpanded, ref bool field, string propertyName)
    {
        if (!SetProperty(ref field, isExpanded, propertyName) || _suppressSectionAccordion)
            return;

        if (!isExpanded)
            return;

        _suppressSectionAccordion = true;
        try
        {
            if (section != GymSection.Catalog && _isCatalogExpanded)
            {
                _isCatalogExpanded = false;
                OnPropertyChanged(nameof(IsCatalogExpanded));
            }

            if (section != GymSection.Workout && _isWorkoutExpanded)
            {
                _isWorkoutExpanded = false;
                OnPropertyChanged(nameof(IsWorkoutExpanded));
            }

            if (section != GymSection.History && _isHistoryExpanded)
            {
                _isHistoryExpanded = false;
                OnPropertyChanged(nameof(IsHistoryExpanded));
            }
        }
        finally
        {
            _suppressSectionAccordion = false;
        }
    }

    private enum GymSection
    {
        Catalog,
        Workout,
        History
    }

    protected override Task LoadCoreAsync() => LoadDataAsync();

    private void OnEntriesCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        RefreshWorkoutValidation();
        CommandManager.InvalidateRequerySuggested();
    }

    private void AddRow()
    {
        var row = new GymEntryRowViewModel(Entries.Count);
        row.PropertyChanged += (_, _) => RefreshWorkoutValidation();
        row.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName == nameof(GymEntryRowViewModel.SelectedExerciseId))
                SyncRowExercise(row);
        };
        Entries.Add(row);
    }

    private void RemoveRow(object? parameter)
    {
        if (parameter is GymEntryRowViewModel row)
            Entries.Remove(row);
    }

    private ValidationResult ValidateExerciseForm() =>
        FormValidation.RequireText(NewExerciseName, "el nombre del ejercicio");

    private ValidationResult ValidateWorkoutForm()
    {
        if (Entries.Count == 0)
            return ValidationResult.Fail("Agregue al menos un ejercicio al entrenamiento.");

        for (var index = 0; index < Entries.Count; index++)
        {
            var row = Entries[index];
            var rowNumber = index + 1;

            if (!row.SelectedExerciseId.HasValue)
                return ValidationResult.Fail($"Seleccione un ejercicio en la fila {rowNumber}.");

            if (row.Sets <= 0)
                return ValidationResult.Fail($"Las series deben ser mayor que cero (fila {rowNumber}).");

            if (row.CanEditRepetitions && row.Repetitions is <= 0)
                return ValidationResult.Fail($"Las repeticiones deben ser mayor que cero (fila {rowNumber}).");

            if (row.CanEditWeight && row.WeightKg is < 0)
                return ValidationResult.Fail($"El peso no puede ser negativo (fila {rowNumber}).");

            if (row.CanEditDuration && row.DurationMinutes == 0 && row.DurationSeconds == 0)
                return ValidationResult.Fail($"Indique una duración mayor que cero (fila {rowNumber}).");
        }

        return ValidationResult.Ok();
    }

    private void RefreshExerciseValidation()
    {
        var result = ValidateExerciseForm();
        ExerciseValidationMessage = result.IsValid ? null : result.Message;
        CreateExerciseCommand.RaiseCanExecuteChanged();
    }

    private void RefreshWorkoutValidation() =>
        RefreshValidation(ValidateWorkoutForm(), SaveWorkoutCommand);

    private bool CanCreateExercise() => ValidateExerciseForm().IsValid;

    private bool CanSaveWorkout() => ValidateWorkoutForm().IsValid;

    private bool CanUpdateCatalogMuscleGroup() =>
        SelectedCatalogExercise is not null &&
        EditMuscleGroupOption.Value != SelectedCatalogExercise.MuscleGroup;

    private void SyncRowExercise(GymEntryRowViewModel row)
    {
        var exercise = Exercises.FirstOrDefault(e => e.Id == row.SelectedExerciseId);
        if (exercise is not null)
            row.ApplyExercise(exercise);
    }

    private async Task CreateExerciseAsync()
    {
        if (!ValidateExerciseForm().IsValid)
        {
            RefreshExerciseValidation();
            return;
        }

        await RunBusyAsync(async () =>
        {
            var exercise = await _gymService.CreateOrGetExerciseAsync(
                NewExerciseName,
                NewExerciseTypeOption.Value,
                NewMuscleGroupOption.Value);

            var existing = Exercises.FirstOrDefault(e => e.Id == exercise.Id);
            if (existing is null)
            {
                Exercises.Add(exercise);
            }
            else
            {
                existing.MuscleGroup = exercise.MuscleGroup;
            }

            RebuildFilteredExercises();
            CatalogExercisesView.Refresh();
            NewExerciseName = string.Empty;
            NewMuscleGroupOption = MuscleGroupCatalogOptions[0];
            ExerciseValidationMessage = null;
            StatusMessage = $"Ejercicio '{exercise.Name}' disponible ({exercise.MuscleGroupLabel}).";
        }, "Creando ejercicio...");
    }

    private async Task UpdateCatalogMuscleGroupAsync()
    {
        if (SelectedCatalogExercise is null)
            return;

        await RunBusyAsync(async () =>
        {
            var updated = await _gymService.UpdateExerciseMuscleGroupAsync(
                SelectedCatalogExercise.Id,
                EditMuscleGroupOption.Value);

            if (updated is null)
                return;

            SelectedCatalogExercise.MuscleGroup = updated.MuscleGroup;
            RebuildFilteredExercises();
            CatalogExercisesView.Refresh();
            OnPropertyChanged(nameof(SelectedCatalogExercise));
            UpdateCatalogMuscleGroupCommand.RaiseCanExecuteChanged();
            StatusMessage = $"Grupo de '{updated.Name}' → {updated.MuscleGroupLabel}.";
        }, "Actualizando grupo muscular...");
    }

    private async Task SaveWorkoutAsync()
    {
        if (!ValidateWorkoutForm().IsValid)
        {
            RefreshWorkoutValidation();
            return;
        }

        await RunBusyAsync(async () =>
        {
            var drafts = Entries.Select(e => e.ToDraft()).ToList();
            var result = await _gymService.SaveWorkoutAsync(drafts);
            PublishAchievements(result.Events);

            Entries.Clear();
            AddRow();

            await LoadHistoryAsync();
            SelectedWorkout = History.FirstOrDefault(w => w.Id == result.Value.Id) ?? History.FirstOrDefault();

            ClearValidation();
            IsHistoryExpanded = true;
            var overload = result.Value.TriggeredProgressiveOverload ? " · ¡Sobrecarga progresiva!" : string.Empty;
            StatusMessage = $"Entrenamiento guardado · +{result.Value.XpEarned} XP{overload}";
        }, "Guardando entrenamiento...");
    }

    private async Task DeleteWorkoutAsync(object? parameter)
    {
        if (parameter is not GymWorkout workout)
            return;

        if (!_messageDialogService.Confirm(
                $"¿Eliminar el entrenamiento del {workout.WorkoutDate:dd/MM/yyyy}?\nSe revertirá el XP asociado.",
                "Eliminar del historial"))
            return;

        await RunBusyAsync(async () =>
        {
            if (!await _gymService.DeleteWorkoutAsync(workout.Id))
                return;

            _allWorkouts.RemoveAll(w => w.Id == workout.Id);
            ApplyHistoryFilter();
            _profileRefreshMessenger.RequestRefresh();
            StatusMessage = "Entrenamiento eliminado del historial.";
        }, "Eliminando entrenamiento...");
    }
}
