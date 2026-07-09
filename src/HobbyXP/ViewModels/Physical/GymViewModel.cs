using System.Collections.ObjectModel;
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
    private string _newExerciseName = string.Empty;
    private ExerciseType _newExerciseType = ExerciseType.TraditionalWeight;
    private GymWorkout? _selectedWorkout;
    private DateTime? _historyFromDate;
    private DateTime? _historyToDate;
    private List<GymWorkout> _allWorkouts = [];

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
        Exercises = new ObservableCollection<Exercise>();
        Entries = new ObservableCollection<GymEntryRowViewModel>();
        History = new ObservableCollection<GymWorkout>();

        AddRowCommand = new RelayCommand(AddRow);
        RemoveRowCommand = new RelayCommand(RemoveRow, _ => Entries.Count > 0);
        SaveWorkoutCommand = new AsyncRelayCommand(SaveWorkoutAsync, () => Entries.Count > 0);
        CreateExerciseCommand = new AsyncRelayCommand(CreateExerciseAsync, () => !string.IsNullOrWhiteSpace(NewExerciseName));
        ClearHistoryDateFilterCommand = new RelayCommand(ClearHistoryDateFilter);
        DeleteWorkoutCommand = new AsyncRelayCommand(p => DeleteWorkoutAsync(p));

        AddRow();
    }

    public ObservableCollection<Exercise> Exercises { get; }

    public ObservableCollection<GymEntryRowViewModel> Entries { get; }

    public ObservableCollection<GymWorkout> History { get; }

    public GymWorkout? SelectedWorkout
    {
        get => _selectedWorkout;
        set => SetProperty(ref _selectedWorkout, value);
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

    public Array ExerciseTypes => Enum.GetValues(typeof(ExerciseType));

    public string NewExerciseName
    {
        get => _newExerciseName;
        set => SetProperty(ref _newExerciseName, value);
    }

    public ExerciseType NewExerciseType
    {
        get => _newExerciseType;
        set => SetProperty(ref _newExerciseType, value);
    }

    public RelayCommand AddRowCommand { get; }

    public RelayCommand RemoveRowCommand { get; }

    public AsyncRelayCommand SaveWorkoutCommand { get; }

    public AsyncRelayCommand CreateExerciseCommand { get; }

    public RelayCommand ClearHistoryDateFilterCommand { get; }

    public AsyncRelayCommand DeleteWorkoutCommand { get; }

    public async Task LoadDataAsync()
    {
        var exercises = await _gymService.GetExercisesAsync();
        Exercises.Clear();
        foreach (var exercise in exercises)
            Exercises.Add(exercise);

        await LoadHistoryAsync();
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
        foreach (var workout in _allWorkouts.Where(w => DateRangeFilter.Matches(w.WorkoutDate, HistoryFromDate, HistoryToDate)))
            History.Add(workout);

        SelectedWorkout = selectedId.HasValue
            ? History.FirstOrDefault(w => w.Id == selectedId.Value)
            : History.FirstOrDefault();
    }

    private void ClearHistoryDateFilter()
    {
        _historyFromDate = null;
        _historyToDate = null;
        OnPropertyChanged(nameof(HistoryFromDate));
        OnPropertyChanged(nameof(HistoryToDate));
        ApplyHistoryFilter();
    }

    protected override Task LoadCoreAsync() => LoadDataAsync();

    private void AddRow()
    {
        var row = new GymEntryRowViewModel(Entries.Count);
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

    private void SyncRowExercise(GymEntryRowViewModel row)
    {
        var exercise = Exercises.FirstOrDefault(e => e.Id == row.SelectedExerciseId);
        if (exercise is not null)
            row.ApplyExercise(exercise);
    }

    private async Task CreateExerciseAsync()
    {
        await RunBusyAsync(async () =>
        {
            var exercise = await _gymService.CreateOrGetExerciseAsync(NewExerciseName, NewExerciseType);
            if (!Exercises.Any(e => e.Id == exercise.Id))
                Exercises.Add(exercise);

            NewExerciseName = string.Empty;
            StatusMessage = $"Ejercicio '{exercise.Name}' disponible.";
        }, "Creando ejercicio...");
    }

    private async Task SaveWorkoutAsync()
    {
        await RunBusyAsync(async () =>
        {
            var drafts = Entries.Select(e => e.ToDraft()).ToList();
            var result = await _gymService.SaveWorkoutAsync(drafts);
            PublishAchievements(result.Events);

            Entries.Clear();
            AddRow();

            await LoadHistoryAsync();
            SelectedWorkout = History.FirstOrDefault(w => w.Id == result.Value.Id) ?? History.FirstOrDefault();

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
