using System.Collections.ObjectModel;
using HobbyXP.Helpers;
using HobbyXP.Models.Enums;
using HobbyXP.Models.Physical;
using HobbyXP.Services.Abstractions;
using HobbyXP.ViewModels.Common;

namespace HobbyXP.ViewModels.Physical;

public sealed class GymEntryRowViewModel : ViewModelBase
{
    private int? _selectedExerciseId;
    private ExerciseType _exerciseType = ExerciseType.TraditionalWeight;
    private int _sets = 3;
    private int? _repetitions = 10;
    private decimal? _weightKg = 20m;
    private int _durationMinutes;
    private int _durationSeconds = 30;
    private string _exerciseQuery = string.Empty;
    private IReadOnlyList<Exercise> _pickerCatalog = [];
    private bool _suppressPickerRebuild;

    public GymEntryRowViewModel(int sortOrder)
    {
        SortOrder = sortOrder;
        PickerExercises = new ObservableCollection<Exercise>();
    }

    public int SortOrder { get; }

    public ObservableCollection<Exercise> PickerExercises { get; }

    public string ExerciseQuery
    {
        get => _exerciseQuery;
        set
        {
            if (SetProperty(ref _exerciseQuery, value ?? string.Empty))
                RebuildPickerExercises();
        }
    }

    public int? SelectedExerciseId
    {
        get => _selectedExerciseId;
        set
        {
            // El ComboBox editable pone null al escribir un texto que no coincide;
            // se conserva la selección previa (se cambia eligiendo otro ítem o borrando la fila).
            if (value is null)
                return;

            if (!SetProperty(ref _selectedExerciseId, value))
                return;

            SyncQueryFromSelection();
            RebuildPickerExercises();
            OnPropertyChanged(nameof(CanEditWeight));
            OnPropertyChanged(nameof(CanEditRepetitions));
            OnPropertyChanged(nameof(CanEditDuration));
        }
    }

    public ExerciseType ExerciseType
    {
        get => _exerciseType;
        set
        {
            if (!SetProperty(ref _exerciseType, value))
                return;

            ApplyExerciseTypeDefaults();
            OnPropertyChanged(nameof(CanEditWeight));
            OnPropertyChanged(nameof(CanEditRepetitions));
            OnPropertyChanged(nameof(CanEditDuration));
        }
    }

    public int Sets
    {
        get => _sets;
        set => SetProperty(ref _sets, value);
    }

    public int? Repetitions
    {
        get => _repetitions;
        set => SetProperty(ref _repetitions, value);
    }

    public decimal? WeightKg
    {
        get => _weightKg;
        set => SetProperty(ref _weightKg, value);
    }

    public int DurationMinutes
    {
        get => _durationMinutes;
        set => SetProperty(ref _durationMinutes, value);
    }

    public int DurationSeconds
    {
        get => _durationSeconds;
        set => SetProperty(ref _durationSeconds, value);
    }

    public bool CanEditWeight => ExerciseType == ExerciseType.TraditionalWeight;

    public bool CanEditRepetitions => ExerciseType is ExerciseType.TraditionalWeight or ExerciseType.BodyWeight;

    public bool CanEditDuration => ExerciseType == ExerciseType.TimeBased;

    public void ApplyExercise(Exercise exercise)
    {
        SelectedExerciseId = exercise.Id;
        ExerciseType = exercise.ExerciseType;
        SyncQueryFromSelection();
    }

    /// <summary>Rellena la fila desde un historial (referencia); no persiste.</summary>
    public void LoadFromHistoryEntry(GymWorkoutEntry entry)
    {
        SelectedExerciseId = entry.ExerciseId;
        ExerciseType = entry.ExerciseType;
        LoadPerformanceFromHistory(entry);
        SyncQueryFromSelection();
    }

    /// <summary>Copia series/reps/peso/tiempo de un registro previo; el usuario puede editarlos.</summary>
    public void LoadPerformanceFromHistory(GymWorkoutEntry entry)
    {
        Sets = entry.Sets;
        Repetitions = entry.Repetitions;
        WeightKg = entry.WeightKg;

        if (entry.Duration is { } duration)
        {
            DurationMinutes = (int)duration.TotalMinutes;
            DurationSeconds = duration.Seconds;
        }
        else
        {
            DurationMinutes = 0;
            DurationSeconds = ExerciseType == ExerciseType.TimeBased ? 30 : 0;
        }
    }

    public void UpdatePickerCatalog(IReadOnlyList<Exercise> catalog)
    {
        _pickerCatalog = catalog;
        RebuildPickerExercises();
        SyncQueryFromSelection();
        if (SelectedExerciseId.HasValue)
            OnPropertyChanged(nameof(SelectedExerciseId));
    }

    private void SyncQueryFromSelection()
    {
        var selected = _pickerCatalog.FirstOrDefault(e => e.Id == SelectedExerciseId)
            ?? PickerExercises.FirstOrDefault(e => e.Id == SelectedExerciseId);
        if (selected is null)
            return;

        if (!string.Equals(_exerciseQuery, selected.PickerDisplayName, StringComparison.Ordinal))
        {
            _exerciseQuery = selected.PickerDisplayName;
            OnPropertyChanged(nameof(ExerciseQuery));
        }
    }

    private void RebuildPickerExercises()
    {
        if (_suppressPickerRebuild)
            return;

        _suppressPickerRebuild = true;
        try
        {
            var selectedId = SelectedExerciseId;
            var matches = ExercisePickerFilter
                .Filter(_pickerCatalog, _exerciseQuery, selectedId)
                .ToList();

            PickerExercises.Clear();
            foreach (var exercise in matches)
                PickerExercises.Add(exercise);

            if (selectedId.HasValue && SelectedExerciseId != selectedId)
                SelectedExerciseId = selectedId;
        }
        finally
        {
            _suppressPickerRebuild = false;
        }
    }

    public GymWorkoutEntryDraft ToDraft()
    {
        TimeSpan? duration = ExerciseType == ExerciseType.TimeBased
            ? new TimeSpan(0, DurationMinutes, DurationSeconds)
            : null;

        return new GymWorkoutEntryDraft(
            SelectedExerciseId ?? throw new InvalidOperationException("Seleccione un ejercicio."),
            ExerciseType,
            Sets,
            CanEditRepetitions ? Repetitions : null,
            CanEditWeight ? WeightKg : null,
            duration,
            SortOrder);
    }

    private void ApplyExerciseTypeDefaults()
    {
        switch (ExerciseType)
        {
            case ExerciseType.TraditionalWeight:
                Repetitions ??= 10;
                WeightKg ??= 20m;
                break;
            case ExerciseType.BodyWeight:
                Repetitions ??= 10;
                WeightKg = null;
                break;
            case ExerciseType.TimeBased:
                Repetitions = null;
                WeightKg = null;
                DurationMinutes = 0;
                DurationSeconds = 30;
                break;
        }
    }
}
