using System.Collections.ObjectModel;
using System.Globalization;
using HobbyXP.Helpers;
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
        SaveRuleCommand = new AsyncRelayCommand(SaveRuleAsync, CanSaveRule);
    }

    public ObservableCollection<AchievementRule> Rules { get; }

    public AchievementRule? SelectedRule
    {
        get => _selectedRule;
        set
        {
            if (!SetProperty(ref _selectedRule, value))
                return;

            ClearValidation();
            NotifyEditPropertiesChanged();
            RefreshRuleValidation();
        }
    }

    public bool HasSelectedRule => SelectedRule is not null;

    public string SelectedRuleActionLabel =>
        SelectedRule is null
            ? string.Empty
            : AchievementDisplayNames.ForActionType(SelectedRule.ActionType);

    public string SelectedRuleFormulaHint
    {
        get
        {
            if (SelectedRule is null)
                return string.Empty;

            if (SelectedRule.FlatBonusPoints is int bonus && SelectedRule.PointsPerUnit > 0m)
            {
                return $"Fórmula: ({SelectedRule.PointsPerUnit} × unidades) + {bonus} {SelectedRule.UnitLabel}";
            }

            if (SelectedRule.FlatBonusPoints is int flatOnly)
                return $"Fórmula: bono fijo de {flatOnly} XP por {SelectedRule.UnitLabel}";

            return $"Fórmula: {SelectedRule.PointsPerUnit} XP por {SelectedRule.UnitLabel}";
        }
    }

    public string EditDisplayName
    {
        get => SelectedRule?.DisplayName ?? string.Empty;
        set
        {
            if (SelectedRule is null || SelectedRule.DisplayName == value)
                return;

            SelectedRule.DisplayName = value;
            OnPropertyChanged();
            RefreshRuleValidation();
        }
    }

    public string EditUnitLabel
    {
        get => SelectedRule?.UnitLabel ?? string.Empty;
        set
        {
            if (SelectedRule is null || SelectedRule.UnitLabel == value)
                return;

            SelectedRule.UnitLabel = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(SelectedRuleFormulaHint));
            RefreshRuleValidation();
        }
    }

    public string EditPointsPerUnit
    {
        get => SelectedRule?.PointsPerUnit.ToString(CultureInfo.CurrentCulture) ?? string.Empty;
        set
        {
            if (SelectedRule is null)
                return;

            if (!decimal.TryParse(value, NumberStyles.Number, CultureInfo.CurrentCulture, out var parsed)
                && !decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out parsed))
            {
                OnPropertyChanged();
                RefreshRuleValidation();
                return;
            }

            if (SelectedRule.PointsPerUnit == parsed)
                return;

            SelectedRule.PointsPerUnit = parsed;
            OnPropertyChanged();
            OnPropertyChanged(nameof(SelectedRuleFormulaHint));
            RefreshRuleValidation();
        }
    }

    public string EditFlatBonusPoints
    {
        get => SelectedRule?.FlatBonusPoints?.ToString(CultureInfo.CurrentCulture) ?? string.Empty;
        set
        {
            if (SelectedRule is null)
                return;

            if (string.IsNullOrWhiteSpace(value))
            {
                if (SelectedRule.FlatBonusPoints is null)
                    return;

                SelectedRule.FlatBonusPoints = null;
                OnPropertyChanged();
                OnPropertyChanged(nameof(SelectedRuleFormulaHint));
                RefreshRuleValidation();
                return;
            }

            if (!int.TryParse(value, NumberStyles.Integer, CultureInfo.CurrentCulture, out var parsed)
                && !int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out parsed))
            {
                OnPropertyChanged();
                RefreshRuleValidation();
                return;
            }

            if (SelectedRule.FlatBonusPoints == parsed)
                return;

            SelectedRule.FlatBonusPoints = parsed;
            OnPropertyChanged();
            OnPropertyChanged(nameof(SelectedRuleFormulaHint));
            RefreshRuleValidation();
        }
    }

    public bool EditIsActive
    {
        get => SelectedRule?.IsActive ?? false;
        set
        {
            if (SelectedRule is null || SelectedRule.IsActive == value)
                return;

            SelectedRule.IsActive = value;
            OnPropertyChanged();
            RefreshRuleValidation();
        }
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

    private ValidationResult ValidateSelectedRule()
    {
        if (SelectedRule is null)
            return ValidationResult.Fail("Seleccione una regla.");

        return FormValidation.FirstFailure(
            FormValidation.RequireText(SelectedRule.DisplayName, "el nombre"),
            FormValidation.RequireText(SelectedRule.UnitLabel, "la unidad"),
            SelectedRule.PointsPerUnit >= 0m
                ? ValidationResult.Ok()
                : ValidationResult.Fail("Los puntos por unidad no pueden ser negativos."),
            SelectedRule.FlatBonusPoints is null or >= 0
                ? ValidationResult.Ok()
                : ValidationResult.Fail("El bono fijo no puede ser negativo."));
    }

    private void RefreshRuleValidation() =>
        RefreshValidation(ValidateSelectedRule(), SaveRuleCommand);

    private void NotifyEditPropertiesChanged()
    {
        OnPropertyChanged(nameof(HasSelectedRule));
        OnPropertyChanged(nameof(SelectedRuleActionLabel));
        OnPropertyChanged(nameof(SelectedRuleFormulaHint));
        OnPropertyChanged(nameof(EditDisplayName));
        OnPropertyChanged(nameof(EditUnitLabel));
        OnPropertyChanged(nameof(EditPointsPerUnit));
        OnPropertyChanged(nameof(EditFlatBonusPoints));
        OnPropertyChanged(nameof(EditIsActive));
    }

    private bool CanSaveRule() => SelectedRule is not null && ValidateSelectedRule().IsValid;

    private async Task SaveRuleAsync()
    {
        if (!ValidateSelectedRule().IsValid)
        {
            RefreshRuleValidation();
            return;
        }

        if (SelectedRule is null)
            return;

        await RunBusyAsync(async () =>
        {
            var updated = await _achievementEngine.UpdateRuleAsync(SelectedRule);
            var index = Rules.IndexOf(SelectedRule);
            if (index >= 0)
                Rules[index] = updated;

            SelectedRule = updated;
            ClearValidation();
            StatusMessage = $"Regla '{updated.DisplayName}' actualizada.";
        }, "Guardando regla...");
    }
}
