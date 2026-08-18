using HobbyXP.Data;
using HobbyXP.Helpers;
using HobbyXP.Models.Enums;
using HobbyXP.Models.PersonalGrowth;
using Microsoft.EntityFrameworkCore;

namespace HobbyXP.Services.Internal;

internal static class MedalTrackCounter
{
    public static async Task<int> ResolveAsync(
        HobbyXpDbContext db,
        MedalMilestoneTrack track,
        CancellationToken cancellationToken) => track switch
    {
        MedalMilestoneTrack.BooksCompleted => await db.Books
            .CountAsync(b => b.Status == BookStatus.Completed, cancellationToken),
        MedalMilestoneTrack.BookPagesRead => await db.Books
            .SumAsync(b => (int?)b.PagesRead, cancellationToken) ?? 0,
        MedalMilestoneTrack.MediaCompleted => await db.MediaEntries
            .CountAsync(cancellationToken),
        MedalMilestoneTrack.PuzzlesCompleted => await db.Puzzles
            .CountAsync(cancellationToken),
        MedalMilestoneTrack.CoursesCompleted => await db.Courses
            .CountAsync(c => c.Status == CourseStatus.Completed, cancellationToken),
        MedalMilestoneTrack.CourseSessions => await db.Courses
            .SumAsync(c => (int?)c.SessionsCompleted, cancellationToken) ?? 0,
        MedalMilestoneTrack.OfficialRacesCompleted => await db.OfficialRaces
            .CountAsync(r => r.IsCompleted, cancellationToken),
        MedalMilestoneTrack.RunningSessions => await db.RunningSessions
            .CountAsync(cancellationToken),
        MedalMilestoneTrack.RunningKilometers => (int)Math.Round(
            await db.RunningSessions.SumAsync(s => (double?)s.DistanceKm, cancellationToken) ?? 0d),
        MedalMilestoneTrack.GymWorkouts => await db.GymWorkouts
            .CountAsync(cancellationToken),
        MedalMilestoneTrack.ProgressiveOverloadPrs => await db.GymWorkouts
            .CountAsync(w => w.TriggeredProgressiveOverload, cancellationToken),
        MedalMilestoneTrack.VideoGamesPlatinum => await db.VideoGames
            .CountAsync(g => g.CompletionPercentage >= 100, cancellationToken),
        MedalMilestoneTrack.DietGoodDays => await db.DietDayLogs
            .CountAsync(d => d.OnPlanCount >= DietDayRules.GoodDayThreshold, cancellationToken),
        MedalMilestoneTrack.DietPerfectDays => await db.DietDayLogs
            .CountAsync(d => d.OnPlanCount == DietDayRules.MealsPerDay, cancellationToken),
        _ => 0
    };
}
