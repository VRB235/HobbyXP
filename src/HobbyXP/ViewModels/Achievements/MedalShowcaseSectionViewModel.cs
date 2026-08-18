using HobbyXP.Models.Enums;
using HobbyXP.Services.Results;
using HobbyXP.ViewModels.Common;

namespace HobbyXP.ViewModels.Achievements;

public sealed class MedalShowcaseSectionViewModel : ViewModelBase
{
    private bool _isExpanded;

    public MedalShowcaseSectionViewModel(MedalShowcaseSection section)
    {
        SourceType = section.SourceType;
        DisplayName = section.DisplayName;
        ProgressText = section.ProgressText;
        Medals = section.Medals;
        _isExpanded = section.EarnedCount > 0;
    }

    public MilestoneSourceType SourceType { get; }

    public string DisplayName { get; }

    public string ProgressText { get; }

    public IReadOnlyList<MedalShowcaseItem> Medals { get; }

    public bool IsExpanded
    {
        get => _isExpanded;
        set => SetProperty(ref _isExpanded, value);
    }

    public override string ToString() => $"{DisplayName}  ·  {ProgressText}";
}
