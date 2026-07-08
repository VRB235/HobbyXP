using System.Collections.ObjectModel;
using HobbyXP.Models.PersonalGrowth;
using HobbyXP.Services.Abstractions;
using HobbyXP.ViewModels.Common;
using HobbyXP.ViewModels.Messaging;

namespace HobbyXP.ViewModels.PersonalGrowth;

public sealed class CoursesViewModel : AchievementAwareViewModel
{
    private readonly ICourseService _courseService;
    private string _name = string.Empty;
    private string _platform = "Udemy";

    public CoursesViewModel(ICourseService courseService, IAchievementMessenger achievementMessenger)
        : base(achievementMessenger)
    {
        _courseService = courseService;
        Courses = new ObservableCollection<Course>();
        RegisterCommand = new AsyncRelayCommand(RegisterAsync, () => !string.IsNullOrWhiteSpace(Name));
    }

    public ObservableCollection<Course> Courses { get; }

    public string Name
    {
        get => _name;
        set => SetProperty(ref _name, value);
    }

    public string Platform
    {
        get => _platform;
        set => SetProperty(ref _platform, value);
    }

    public AsyncRelayCommand RegisterCommand { get; }

    protected override async Task LoadCoreAsync()
    {
        var courses = await _courseService.GetAllAsync();
        Courses.Clear();
        foreach (var course in courses)
            Courses.Add(course);
    }

    private async Task RegisterAsync()
    {
        await RunBusyAsync(async () =>
        {
            var result = await _courseService.RegisterCompletedAsync(Name, Platform);
            PublishAchievements(result.Events);
            Courses.Insert(0, result.Value);

            Name = string.Empty;
            StatusMessage = $"Curso registrado · +{result.Value.XpEarned} XP";
        }, "Registrando curso...");
    }
}
