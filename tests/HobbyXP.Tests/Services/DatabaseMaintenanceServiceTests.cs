using HobbyXP.Models.Achievements;
using HobbyXP.Models.Core;
using HobbyXP.Models.Enums;
using HobbyXP.Models.Physical;
using HobbyXP.Services;
using HobbyXP.Tests.Helpers;
using Microsoft.EntityFrameworkCore;

namespace HobbyXP.Tests.Services;

public sealed class DatabaseMaintenanceServiceTests : IDisposable
{
    private readonly TestDbContextFactory _factory;
    private readonly DatabaseMaintenanceService _sut;

    public DatabaseMaintenanceServiceTests()
    {
        _factory = new TestDbContextFactory(db =>
        {
            var profile = db.PlayerProfiles.Single();
            profile.DisplayName = "Tester";
            profile.BaseXpPerLevel = 1500;
            profile.TotalXp = 2500;
            profile.CurrentLevel = 3;
            profile.SpendableXp = 800;
            profile.AvatarPath = @"C:\temp\avatar.png";

            db.HobbyProgresses.Add(new HobbyProgress
            {
                PlayerProfileId = profile.Id,
                SourceType = MilestoneSourceType.Gym,
                CurrentLevel = 2,
                TotalXp = 1200
            });

            var squat = new Exercise
            {
                Name = "Sentadilla",
                ExerciseType = ExerciseType.TraditionalWeight,
                MuscleGroup = MuscleGroup.Cuadriceps
            };
            db.Exercises.Add(squat);

            var workout = new GymWorkout
            {
                WorkoutDate = DateTime.UtcNow.Date,
                XpEarned = 50,
                Notes = "Sesión de prueba"
            };
            db.GymWorkouts.Add(workout);
            db.SaveChanges();

            db.GymWorkoutEntries.Add(new GymWorkoutEntry
            {
                GymWorkoutId = workout.Id,
                ExerciseId = squat.Id,
                ExerciseType = ExerciseType.TraditionalWeight,
                Sets = 3,
                Repetitions = 8,
                WeightKg = 60,
                SortOrder = 0
            });

            db.XpTransactions.Add(new XpTransaction
            {
                PlayerProfileId = profile.Id,
                Amount = 50,
                ActionType = AchievementActionType.GymWorkoutSaved,
                Description = "Entrenamiento"
            });

            db.Rewards.Add(new Reward
            {
                Name = "Café",
                CostInPoints = 100,
                Status = RewardStatus.Redeemed,
                RedeemedAt = DateTime.UtcNow
            });
        });

        _sut = new DatabaseMaintenanceService(_factory);
    }

    public void Dispose() => _factory.Dispose();

    [Fact]
    public async Task ResetApplicationDataAsync_ClearsProgressButKeepsExerciseCatalog()
    {
        await _sut.ResetApplicationDataAsync();

        await using var db = _factory.CreateDbContext();

        Assert.Equal(1, await db.Exercises.CountAsync());
        Assert.Equal("Sentadilla", await db.Exercises.Select(e => e.Name).SingleAsync());

        Assert.Equal(0, await db.GymWorkouts.CountAsync());
        Assert.Equal(0, await db.GymWorkoutEntries.CountAsync());
        Assert.Equal(0, await db.XpTransactions.CountAsync());

        var profile = await db.PlayerProfiles.Include(p => p.HobbyProgresses).SingleAsync();
        Assert.Equal("Tester", profile.DisplayName);
        Assert.Equal(1500, profile.BaseXpPerLevel);
        Assert.Equal(@"C:\temp\avatar.png", profile.AvatarPath);
        Assert.Equal(0, profile.TotalXp);
        Assert.Equal(1, profile.CurrentLevel);
        Assert.Equal(0, profile.SpendableXp);
        Assert.Null(profile.WeeklyQuotaTrackingStartedAtUtc);
        Assert.All(profile.HobbyProgresses, h =>
        {
            Assert.Equal(0, h.TotalXp);
            Assert.Equal(1, h.CurrentLevel);
        });

        var reward = await db.Rewards.SingleAsync();
        Assert.Equal(RewardStatus.Available, reward.Status);
        Assert.Null(reward.RedeemedAt);
    }
}
