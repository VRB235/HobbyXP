using System.Collections.ObjectModel;
using HobbyXP.Models.Achievements;
using HobbyXP.Services.Abstractions;
using HobbyXP.ViewModels.Common;

namespace HobbyXP.ViewModels.Achievements;

public sealed class RulesEditorViewModel : LoadableViewModelBase
{
    private readonly IAchievementEngineService _achievementEngine;
    private AchievementRule? _selectedRule;

    public RulesEditorViewModel(IAchievementEngineService achievementEngine)
    {
        _achievementEngine = achievementEngine;
        Rules = new ObservableCollection<AchievementRule>();
        SaveRuleCommand = new AsyncRelayCommand(SaveRuleAsync, () => SelectedRule is not null);
    }

    public ObservableCollection<AchievementRule> Rules { get; }

    public AchievementRule? SelectedRule
    {
        get => _selectedRule;
        set => SetProperty(ref _selectedRule, value);
    }

    public AsyncRelayCommand SaveRuleCommand { get; }

    protected override async Task LoadCoreAsync()
    {
        var rules = await _achievementEngine.GetAllRulesAsync();
        Rules.Clear();
        foreach (var rule in rules)
            Rules.Add(rule);

        SelectedRule ??= Rules.FirstOrDefault();
    }

    private async Task SaveRuleAsync()
    {
        if (SelectedRule is null)
            return;

        await RunBusyAsync(async () =>
        {
            var updated = await _achievementEngine.UpdateRuleAsync(SelectedRule);
            var index = Rules.IndexOf(SelectedRule);
            if (index >= 0)
                Rules[index] = updated;

            SelectedRule = updated;
            StatusMessage = $"Regla '{updated.DisplayName}' actualizada.";
        }, "Guardando regla...");
    }
}
