using System.Collections.ObjectModel;
using HobbyXP.Models.Achievements;
using HobbyXP.Services.Abstractions;
using HobbyXP.Services.Results;
using HobbyXP.ViewModels.Common;

namespace HobbyXP.ViewModels.Achievements;

public sealed class MedalShowcaseViewModel : LoadableViewModelBase
{
    private readonly IMedalService _medalService;

    public MedalShowcaseViewModel(IMedalService medalService)
    {
        _medalService = medalService;
        Medals = new ObservableCollection<MedalShowcaseItem>();
    }

    public ObservableCollection<MedalShowcaseItem> Medals { get; }

    protected override async Task LoadCoreAsync()
    {
        var items = await _medalService.GetShowcaseAsync();
        Medals.Clear();
        foreach (var item in items)
            Medals.Add(item);
    }
}
