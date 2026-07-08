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

    public GymEntryRowViewModel(int sortOrder) => SortOrder = sortOrder;

    public int SortOrder { get; }

    public int? SelectedExerciseId
    {
        get => _selectedExerciseId;
        set
        {
            if (!SetProperty(ref _selectedExerciseId, value))
                return;

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
