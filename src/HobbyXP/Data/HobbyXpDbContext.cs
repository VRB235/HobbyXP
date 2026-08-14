using HobbyXP.Models.Achievements;
using HobbyXP.Models.Core;
using HobbyXP.Models.Entertainment;
using HobbyXP.Models.Enums;
using HobbyXP.Models.PersonalGrowth;
using HobbyXP.Models.Physical;
using Microsoft.EntityFrameworkCore;

namespace HobbyXP.Data;

public class HobbyXpDbContext : DbContext
{
    public HobbyXpDbContext(DbContextOptions<HobbyXpDbContext> options)
        : base(options)
    {
    }

    public DbSet<PlayerProfile> PlayerProfiles => Set<PlayerProfile>();
    public DbSet<XpTransaction> XpTransactions => Set<XpTransaction>();
    public DbSet<HobbyProgress> HobbyProgresses => Set<HobbyProgress>();
    public DbSet<Milestone> Milestones => Set<Milestone>();
    public DbSet<WeeklyQuotaEvaluation> WeeklyQuotaEvaluations => Set<WeeklyQuotaEvaluation>();

    public DbSet<OfficialRace> OfficialRaces => Set<OfficialRace>();
    public DbSet<RunningSession> RunningSessions => Set<RunningSession>();
    public DbSet<Exercise> Exercises => Set<Exercise>();
    public DbSet<GymWorkout> GymWorkouts => Set<GymWorkout>();
    public DbSet<GymWorkoutEntry> GymWorkoutEntries => Set<GymWorkoutEntry>();

    public DbSet<Puzzle> Puzzles => Set<Puzzle>();
    public DbSet<MediaEntry> MediaEntries => Set<MediaEntry>();
    public DbSet<MediaSeries> MediaSeries => Set<MediaSeries>();
    public DbSet<MediaSeriesChapterLog> MediaSeriesChapterLogs => Set<MediaSeriesChapterLog>();
    public DbSet<VideoGame> VideoGames => Set<VideoGame>();
    public DbSet<VideoGameProgressLog> VideoGameProgressLogs => Set<VideoGameProgressLog>();

    public DbSet<Book> Books => Set<Book>();
    public DbSet<BookReadingLog> BookReadingLogs => Set<BookReadingLog>();
    public DbSet<Course> Courses => Set<Course>();
    public DbSet<CourseSessionLog> CourseSessionLogs => Set<CourseSessionLog>();

    public DbSet<MedalDefinition> MedalDefinitions => Set<MedalDefinition>();
    public DbSet<EarnedMedal> EarnedMedals => Set<EarnedMedal>();
    public DbSet<AchievementRule> AchievementRules => Set<AchievementRule>();
    public DbSet<Reward> Rewards => Set<Reward>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(HobbyXpDbContext).Assembly);
        HobbyXpDbSeeder.Seed(modelBuilder);
    }
}
