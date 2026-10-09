using System.Globalization;
using System.Windows.Data;
using HobbyXP.Models.Enums;

namespace HobbyXP.Converters;

/// <summary>
/// Emoji de respaldo por hobby cuando la medalla no tiene icono de imagen.
/// </summary>
public sealed class MedalCodeToEmojiConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not MilestoneSourceType sourceType)
            return "⭐";

        return sourceType switch
        {
            MilestoneSourceType.OfficialRace => "🏅",
            MilestoneSourceType.Running => "🏃",
            MilestoneSourceType.Gym => "🏋️",
            MilestoneSourceType.VideoGame => "💎",
            MilestoneSourceType.Book => "📚",
            MilestoneSourceType.Course => "🎓",
            MilestoneSourceType.Puzzle => "🧩",
            MilestoneSourceType.Media => "🎬",
            MilestoneSourceType.Diet => "🥗",
            _ => "⭐"
        };
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
