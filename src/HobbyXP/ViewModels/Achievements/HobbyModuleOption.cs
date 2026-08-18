using HobbyXP.Helpers;
using HobbyXP.Models.Enums;

namespace HobbyXP.ViewModels.Achievements;

public sealed record HobbyModuleOption(MilestoneSourceType Value, string Label)
{
    public static IReadOnlyList<HobbyModuleOption> Catalog { get; } =
        HobbyProgressCatalog.TrackedHobbies
            .Select(hobby => new HobbyModuleOption(hobby, HobbyProgressCatalog.GetDisplayName(hobby)))
            .ToArray();
}
