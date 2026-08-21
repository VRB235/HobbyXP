using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using HobbyXP.Helpers;
using HobbyXP.Models.Enums;
using HobbyXP.Models.PersonalGrowth;
using HobbyXP.Services.Abstractions;
using HobbyXP.Services.Messaging;
using HobbyXP.ViewModels.Common;
using HobbyXP.ViewModels.Messaging;
using HobbyXP.Views.Dialogs;

namespace HobbyXP.ViewModels.PersonalGrowth;

public sealed class CoursesViewModel : AchievementAwareViewModel
{
    private readonly ICourseService _courseService;
    private readonly IFileDialogService _fileDialogService;
    private readonly IProfileRefreshMessenger _profileRefreshMessenger;
    private readonly CoverImageDraft _cover;
    private string _name = string.Empty;
    private string _platform = "Udemy";
    private string _totalSessions = "10";
    private DateTime? _completedFromDate;
    private DateTime? _completedToDate;
    private List<Course> _allInProgress = [];
    private List<Course> _allCompleted = [];

    public CoursesViewModel(
        ICourseService courseService,
        IXpService xpService,
        IWeeklyQuotaService weeklyQuotaService,
        IFileDialogService fileDialogService,
        IProfileRefreshMessenger profileRefreshMessenger,
        IAchievementMessenger achievementMessenger,
        IAchievementProgressService achievementProgress)
        : base(achievementMessenger)
    {
        _courseService = courseService;
        _fileDialogService = fileDialogService;
        _profileRefreshMessenger = profileRefreshMessenger;
        _cover = new CoverImageDraft(HobbyCoverPhotoStorage.Folders.Courses);
        _cover.Changed += OnCoverChanged;

        HobbyXp = new HobbyProgressPresenter(xpService, MilestoneSourceType.Course, weeklyQuotaService, achievementProgress);
        InProgressRows = new ObservableCollection<CourseProgressRowViewModel>();
        CompletedCourses = new ObservableCollection<Course>();
        RegisterCommand = new AsyncRelayCommand(RegisterAsync, CanRegister);
        ClearCompletedDateFilterCommand = new RelayCommand(ClearCompletedDateFilter);
        PickImageCommand = new RelayCommand(() => _cover.Pick(_fileDialogService));
        ClearImageCommand = new RelayCommand(() => _cover.Clear(), () => _cover.HasPreview);
        OpenDetailCommand = new RelayCommand(OpenDetail);
        RefreshRegisterValidation();
    }

    public HobbyProgressPresenter HobbyXp { get; }

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

    public string? PreviewImagePath => _cover.PreviewPath;

    public bool HasPreviewImage => _cover.HasPreview;

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

    public RelayCommand PickImageCommand { get; }

    public RelayCommand ClearImageCommand { get; }

    public RelayCommand OpenDetailCommand { get; }

    protected override Task LoadCoreAsync() => ReloadAsync();

    private void OnCoverChanged()
    {
        OnPropertyChanged(nameof(PreviewImagePath));
        OnPropertyChanged(nameof(HasPreviewImage));
        CommandManager.InvalidateRequerySuggested();
    }

    private void ResetCover()
    {
        _cover.MarkSaved();
        _cover.Clear();
        OnCoverChanged();
    }

    private async Task ReloadAsync()
    {
        await HobbyXp.RefreshAsync();
        _allInProgress = (await _courseService.GetInProgressAsync()).ToList();
        _allCompleted = (await _courseService.GetCompletedAsync()).ToList();
        ApplyFilter();
    }

    private void ApplyFilter()
    {
        InProgressRows.Clear();
        foreach (var course in _allInProgress)
            InProgressRows.Add(new CourseProgressRowViewModel(
                course,
                LogSessionsAsync,
                UpdateCourseImageAsync,
                _fileDialogService));

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
            var course = await _courseService.RegisterAsync(Name, Platform, totalSessions, _cover.PendingSourcePath);
            ResetCover();
            _allInProgress.Insert(0, course);
            ApplyFilter();

            Name = string.Empty;
            TotalSessions = "10";
            ClearValidation();
            StatusMessage = $"Curso '{course.Name}' agregado ({course.TotalSessions} sesiones).";
        }, "Agregando curso...");
    }

    private void OpenDetail(object? parameter)
    {
        if (parameter is not Course course)
            return;

        var detailVm = new CourseDetailViewModel(course, _courseService, _fileDialogService);
        var dialog = new CourseDetailWindow(detailVm)
        {
            Owner = Application.Current.MainWindow
        };

        var accepted = dialog.ShowDialog() == true;
        if (!accepted || detailVm.SavedCourse is null)
            return;

        _ = ReloadAsync();
        StatusMessage = $"Curso actualizado: {detailVm.SavedCourse.Name}";
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

    private async Task<Course> UpdateCourseImageAsync(Course course, string? imageSourcePath, bool clearImage)
    {
        var updated = await _courseService.UpdateImageAsync(course.Id, imageSourcePath, clearImage);
        var index = _allInProgress.FindIndex(c => c.Id == updated.Id);
        if (index >= 0)
            _allInProgress[index] = updated;

        StatusMessage = clearImage
            ? $"Portada quitada de «{updated.Name}»."
            : $"Portada actualizada: «{updated.Name}».";
        return updated;
    }
}
