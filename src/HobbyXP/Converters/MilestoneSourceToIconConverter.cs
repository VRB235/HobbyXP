using System.Globalization;
using System.Windows.Data;
using HobbyXP.Models.Enums;

namespace HobbyXP.Converters;

public sealed class MilestoneSourceToIconConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not MilestoneSourceType sourceType)
            return "⭐";

        return sourceType switch
        {
            MilestoneSourceType.Running => "🏃",
            MilestoneSourceType.Gym => "💪",
            MilestoneSourceType.Puzzle => "🧩",
            MilestoneSourceType.Media => "🎬",
            MilestoneSourceType.VideoGame => "🎮",
            MilestoneSourceType.Book => "📖",
            MilestoneSourceType.Course => "🎓",
            MilestoneSourceType.OfficialRace => "🏅",
            MilestoneSourceType.Diet => "🥗",
            MilestoneSourceType.Reward => "🎁",
            MilestoneSourceType.System => "⚔️",
            _ => "⭐"
        };
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
