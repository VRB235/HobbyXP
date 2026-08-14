using HobbyXP.Helpers;
using HobbyXP.Models.Enums;
using HobbyXP.ViewModels.Common;

namespace HobbyXP.ViewModels.Physical;

public sealed class DietMealRowViewModel : ViewModelBase
{
    private DietMealStatus _status = DietMealStatus.Unlogged;
    private readonly Action _onChanged;

    public DietMealRowViewModel(DietMealSlot slot, Action onChanged)
    {
        Slot = slot;
        Label = DietMealLabels.Slot(slot);
        _onChanged = onChanged;
        SetOnPlanCommand = new RelayCommand(_ => Toggle(DietMealStatus.OnPlan));
        SetOffPlanCommand = new RelayCommand(_ => Toggle(DietMealStatus.OffPlan));
    }

    public DietMealSlot Slot { get; }

    public string Label { get; }

    public DietMealStatus Status
    {
        get => _status;
        set
        {
            if (!SetProperty(ref _status, value))
                return;

            OnPropertyChanged(nameof(IsOnPlan));
            OnPropertyChanged(nameof(IsOffPlan));
            OnPropertyChanged(nameof(StatusLabel));
            _onChanged();
        }
    }

    public bool IsOnPlan => Status == DietMealStatus.OnPlan;

    public bool IsOffPlan => Status == DietMealStatus.OffPlan;

    public string StatusLabel => DietMealLabels.Status(Status);

    public RelayCommand SetOnPlanCommand { get; }

    public RelayCommand SetOffPlanCommand { get; }

    public void SetStatusSilent(DietMealStatus status)
    {
        _status = status;
        OnPropertyChanged(nameof(Status));
        OnPropertyChanged(nameof(IsOnPlan));
        OnPropertyChanged(nameof(IsOffPlan));
        OnPropertyChanged(nameof(StatusLabel));
    }

    private void Toggle(DietMealStatus target) =>
        Status = Status == target ? DietMealStatus.Unlogged : target;
}
