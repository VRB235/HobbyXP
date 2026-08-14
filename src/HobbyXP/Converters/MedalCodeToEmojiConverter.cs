using System.Globalization;
using System.Windows.Data;
using HobbyXP.Data;
using HobbyXP.Models.Enums;

namespace HobbyXP.Converters;

public sealed class MedalCodeToEmojiConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not MedalCode code)
            return "⭐";

        var entry = MedalCatalog.Entries.FirstOrDefault(e => e.Code == code);
        if (entry is null)
            return "⭐";

        return entry.Track switch
        {
            MedalMilestoneTrack.OfficialRacesCompleted => "🏅",
            MedalMilestoneTrack.RunningSessions => "🏃",
            MedalMilestoneTrack.RunningKilometers => "🛣️",
            MedalMilestoneTrack.ProgressiveOverloadPrs => "💪",
            MedalMilestoneTrack.GymWorkouts => "🏋️",
            MedalMilestoneTrack.VideoGamesPlatinum => "💎",
            MedalMilestoneTrack.BooksCompleted => "📚",
            MedalMilestoneTrack.BookPagesRead => "📖",
            MedalMilestoneTrack.CoursesCompleted => "🎓",
            MedalMilestoneTrack.CourseSessions => "📝",
            MedalMilestoneTrack.PuzzlesCompleted => "🧩",
            MedalMilestoneTrack.MediaCompleted => "🎬",
            MedalMilestoneTrack.DietGoodDays => "🥗",
            MedalMilestoneTrack.DietPerfectDays => "✨",
            _ => "⭐"
        };
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
