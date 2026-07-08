using System.Collections.ObjectModel;
using HobbyXP.Models.Enums;
using HobbyXP.Models.Physical;
using HobbyXP.Services.Abstractions;
using HobbyXP.ViewModels.Common;
using HobbyXP.ViewModels.Messaging;

namespace HobbyXP.ViewModels.Physical;

public sealed class GymViewModel : AchievementAwareViewModel
{
    private readonly IGymService _gymService;
    private string _newExerciseName = string.Empty;
    private ExerciseType _newExerciseType = ExerciseType.TraditionalWeight;

    public GymViewModel(IGymService gymService, IAchievementMessenger achievementMessenger)
        : base(achievementMessenger)
    {
        _gymService = gymService;
        Exercises = new ObservableCollection<Exercise>();
        Entries = new ObservableCollection<GymEntryRowViewModel>();

        AddRowCommand = new RelayCommand(AddRow);
        RemoveRowCommand = new RelayCommand(RemoveRow, _ => Entries.Count > 0);
        SaveWorkoutCommand = new AsyncRelayCommand(SaveWorkoutAsync, () => Entries.Count > 0);
        CreateExerciseCommand = new AsyncRelayCommand(CreateExerciseAsync, () => !string.IsNullOrWhiteSpace(NewExerciseName));

        AddRow();
    }

    public ObservableCollection<Exercise> Exercises { get; }

    public ObservableCollection<GymEntryRowViewModel> Entries { get; }

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

    public async Task LoadDataAsync()
    {
        var exercises = await _gymService.GetExercisesAsync();
        Exercises.Clear();
        foreach (var exercise in exercises)
            Exercises.Add(exercise);
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

            var overload = result.Value.TriggeredProgressiveOverload ? " · ¡Sobrecarga progresiva!" : string.Empty;
            StatusMessage = $"Entrenamiento guardado · +{result.Value.XpEarned} XP{overload}";
        }, "Guardando entrenamiento...");
    }
}
