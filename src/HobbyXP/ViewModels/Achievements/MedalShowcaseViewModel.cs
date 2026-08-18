using System.Collections.ObjectModel;
using HobbyXP.Services.Abstractions;
using HobbyXP.ViewModels.Common;

namespace HobbyXP.ViewModels.Achievements;

public sealed class MedalShowcaseViewModel : LoadableViewModelBase
{
    private readonly IMedalService _medalService;

    public MedalShowcaseViewModel(IMedalService medalService)
    {
        _medalService = medalService;
        Sections = new ObservableCollection<MedalShowcaseSectionViewModel>();
    }

    public ObservableCollection<MedalShowcaseSectionViewModel> Sections { get; }

    protected override async Task LoadCoreAsync()
    {
        var sections = await _medalService.GetShowcaseSectionsAsync();
        Sections.Clear();
        foreach (var section in sections)
            Sections.Add(new MedalShowcaseSectionViewModel(section));
    }
}
