using HobbyXP.Models.Enums;

namespace HobbyXP.Helpers;

public static class RewardShopCatalog
{
    public const string GeneralDisplayName = "General";

    public static string GetModuleDisplayName(MilestoneSourceType? sourceType) =>
        sourceType is { } type && HobbyProgressCatalog.IsTrackedHobby(type)
            ? HobbyProgressCatalog.GetDisplayName(type)
            : GeneralDisplayName;

    public static IReadOnlyList<RewardShopSection<T>> Group<T>(
        IEnumerable<T> items,
        Func<T, MilestoneSourceType?> sourceSelector)
    {
        var list = items.ToList();
        var sections = new List<RewardShopSection<T>>();

        foreach (var hobby in HobbyProgressCatalog.TrackedHobbies)
        {
            var group = list.Where(item => sourceSelector(item) == hobby).ToList();
            if (group.Count == 0)
                continue;

            sections.Add(new RewardShopSection<T>(
                hobby,
                HobbyProgressCatalog.GetDisplayName(hobby),
                group));
        }

        var general = list
            .Where(item =>
            {
                var source = sourceSelector(item);
                return source is null || !HobbyProgressCatalog.IsTrackedHobby(source.Value);
            })
            .ToList();

        if (general.Count > 0)
        {
            sections.Add(new RewardShopSection<T>(
                null,
                GeneralDisplayName,
                general));
        }

        return sections;
    }
}

public sealed record RewardShopSection<T>(
    MilestoneSourceType? SourceType,
    string DisplayName,
    IReadOnlyList<T> Items)
{
    public string ProgressText =>
        Items.Count == 1 ? "1 premio" : $"{Items.Count} premios";
}
