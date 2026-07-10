using System.Collections.ObjectModel;
using HobbyXP.Helpers;
using HobbyXP.Models.PersonalGrowth;
using HobbyXP.Services.Abstractions;
using HobbyXP.Services.Messaging;
using HobbyXP.ViewModels.Common;
using HobbyXP.ViewModels.Messaging;

namespace HobbyXP.ViewModels.PersonalGrowth;

public sealed class CoursesViewModel : AchievementAwareViewModel
{
    private readonly ICourseService _courseService;
    private readonly IProfileRefreshMessenger _profileRefreshMessenger;
    private string _name = string.Empty;
    private string _platform = "Udemy";
    private string _totalSessions = "10";
    private DateTime? _completedFromDate;
    private DateTime? _completedToDate;
    private List<Course> _allInProgress = [];
    private List<Course> _allCompleted = [];

    public CoursesViewModel(
        ICourseService courseService,
        IProfileRefreshMessenger profileRefreshMessenger,
        IAchievementMessenger achievementMessenger)
        : base(achievementMessenger)
    {
        _courseService = courseService;
        _profileRefreshMessenger = profileRefreshMessenger;
        InProgressRows = new ObservableCollection<CourseProgressRowViewModel>();
        CompletedCourses = new ObservableCollection<Course>();
        RegisterCommand = new AsyncRelayCommand(RegisterAsync, CanRegister);
        ClearCompletedDateFilterCommand = new RelayCommand(ClearCompletedDateFilter);
        RefreshRegisterValidation();
    }

    public ObservableCollection<CourseProgressRowViewModel> InProgressRows { get; }

    public ObservableCollection<Course> CompletedCourses { get; }

    public string Name
    {
        get => _name;
        set
        {
            if (SetProperty(ref _name, value))
                RefreshRegisterValidation();
        }
    }

    public string Platform
    {
        get => _platform;
        set => SetProperty(ref _platform, value);
    }

    public string TotalSessions
    {
        get => _totalSessions;
        set
        {
            if (SetProperty(ref _totalSessions, value))
                RefreshRegisterValidation();
        }
    }

    public DateTime? CompletedFromDate
    {
        get => _completedFromDate;
        set
        {
            if (SetProperty(ref _completedFromDate, value))
                ApplyFilter();
        }
    }

    public DateTime? CompletedToDate
    {
        get => _completedToDate;
        set
        {
            if (SetProperty(ref _completedToDate, value))
                ApplyFilter();
        }
    }

    public AsyncRelayCommand RegisterCommand { get; }

    public RelayCommand ClearCompletedDateFilterCommand { get; }

    protected override Task LoadCoreAsync() => ReloadAsync();

    private async Task ReloadAsync()
    {
        _allInProgress = (await _courseService.GetInProgressAsync()).ToList();
        _allCompleted = (await _courseService.GetCompletedAsync()).ToList();
        ApplyFilter();
    }

    private void ApplyFilter()
    {
        InProgressRows.Clear();
        foreach (var course in _allInProgress)
            InProgressRows.Add(new CourseProgressRowViewModel(course, LogSessionsAsync));

        CompletedCourses.Clear();
        foreach (var course in _allCompleted.Where(MatchesCompletedDateFilter))
            CompletedCourses.Add(course);
    }

    private bool MatchesCompletedDateFilter(Course course) =>
        course.CompletedAt.HasValue
            ? DateRangeFilter.Matches(course.CompletedAt.Value, CompletedFromDate, CompletedToDate)
            : !CompletedFromDate.HasValue && !CompletedToDate.HasValue;

    private void ClearCompletedDateFilter()
    {
        _completedFromDate = null;
        _completedToDate = null;
        OnPropertyChanged(nameof(CompletedFromDate));
        OnPropertyChanged(nameof(CompletedToDate));
        ApplyFilter();
    }

    private ValidationResult ValidateRegisterForm() =>
        FormValidation.FirstFailure(
            FormValidation.RequireText(Name, "el nombre del curso"),
            FormValidation.RequirePositiveInt(TotalSessions, "Las sesiones totales", out _));

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

        var totalSessions = int.Parse(TotalSessions);
        await RunBusyAsync(async () =>
        {
            var course = await _courseService.RegisterAsync(Name, Platform, totalSessions);
            _allInProgress.Insert(0, course);
            ApplyFilter();

            Name = string.Empty;
            TotalSessions = "10";
            ClearValidation();
            StatusMessage = $"Curso '{course.Name}' agregado ({course.TotalSessions} sesiones).";
        }, "Agregando curso...");
    }

    private async Task LogSessionsAsync(Course course, DateTime sessionDate, int sessionsDone)
    {
        await RunBusyAsync(async () =>
        {
            var result = await _courseService.LogSessionsAsync(course.Id, sessionDate, sessionsDone);
            PublishAchievements(result.Events);
            await ReloadAsync();
            _profileRefreshMessenger.RequestRefresh();

            var xpGained = result.Events.Sum(e => e.PointsEarned);
            StatusMessage = xpGained > 0
                ? $"{result.Value.Name}: {result.Value.SessionsCompleted}/{result.Value.TotalSessions} sesiones · +{xpGained} XP"
                : $"{result.Value.Name}: {result.Value.SessionsCompleted}/{result.Value.TotalSessions} sesiones";
        }, "Registrando sesiones...");
    }
}
