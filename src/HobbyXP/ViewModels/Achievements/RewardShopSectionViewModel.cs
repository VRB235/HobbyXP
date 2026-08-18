using HobbyXP.Helpers;
using HobbyXP.Models.Enums;
using HobbyXP.ViewModels.Common;

namespace HobbyXP.ViewModels.Achievements;

public sealed class RewardShopSectionViewModel : ViewModelBase
{
    private bool _isExpanded;

    public RewardShopSectionViewModel(RewardShopSection<RewardRowViewModel> section)
    {
        SourceType = section.SourceType;
        DisplayName = section.DisplayName;
        ProgressText = section.ProgressText;
        Rewards = section.Items;
        _isExpanded = section.Items.Count > 0;
    }

    public MilestoneSourceType? SourceType { get; }

    public string DisplayName { get; }

    public string ProgressText { get; }

    public IReadOnlyList<RewardRowViewModel> Rewards { get; }

    public bool IsExpanded
    {
        get => _isExpanded;
        set => SetProperty(ref _isExpanded, value);
    }

    public override string ToString() => $"{DisplayName}  ·  {ProgressText}";
}
