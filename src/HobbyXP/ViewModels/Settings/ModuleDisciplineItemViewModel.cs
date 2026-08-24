using HobbyXP.Models.Enums;
using HobbyXP.ViewModels.Common;

namespace HobbyXP.ViewModels.Settings;

public sealed class ModuleDisciplineItemViewModel : ViewModelBase
{
    private bool _isPaused;

    public ModuleDisciplineItemViewModel(MilestoneSourceType sourceType, string displayName, bool isPaused)
    {
        SourceType = sourceType;
        DisplayName = displayName;
        _isPaused = isPaused;
    }

    public MilestoneSourceType SourceType { get; }

    public string DisplayName { get; }

    public bool IsPaused
    {
        get => _isPaused;
        set => SetProperty(ref _isPaused, value);
    }
}
