using HobbyXP.Models.Enums;

namespace HobbyXP.Helpers;

/// <summary>
/// Pistas de medalla asociadas a cada hobby (para «siguiente logro» en banners).
/// </summary>
public static class MedalTrackMap
{
    public static IReadOnlyList<MedalMilestoneTrack> ForSource(MilestoneSourceType sourceType) =>
        sourceType switch
        {
            MilestoneSourceType.Running =>
            [
                MedalMilestoneTrack.RunningSessions,
                MedalMilestoneTrack.RunningKilometers
            ],
            MilestoneSourceType.OfficialRace => [MedalMilestoneTrack.OfficialRacesCompleted],
            MilestoneSourceType.Gym =>
            [
                MedalMilestoneTrack.GymWorkouts,
                MedalMilestoneTrack.ProgressiveOverloadPrs
            ],
            MilestoneSourceType.Diet =>
            [
                MedalMilestoneTrack.DietGoodDays,
                MedalMilestoneTrack.DietPerfectDays
            ],
            MilestoneSourceType.Puzzle => [MedalMilestoneTrack.PuzzlesCompleted],
            MilestoneSourceType.Media => [MedalMilestoneTrack.MediaCompleted],
            MilestoneSourceType.VideoGame => [MedalMilestoneTrack.VideoGamesPlatinum],
            MilestoneSourceType.Book =>
            [
                MedalMilestoneTrack.BooksCompleted,
                MedalMilestoneTrack.BookPagesRead
            ],
            MilestoneSourceType.Course =>
            [
                MedalMilestoneTrack.CoursesCompleted,
                MedalMilestoneTrack.CourseSessions
            ],
            _ => []
        };

    public static MilestoneSourceType? SourceFor(MedalMilestoneTrack track)
    {
        foreach (var source in HobbyProgressCatalog.TrackedHobbies)
        {
            if (ForSource(source).Contains(track))
                return source;
        }

        return null;
    }
}
