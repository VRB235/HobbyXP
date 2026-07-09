using HobbyXP.Models.Achievements;
using HobbyXP.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace HobbyXP.Data;

internal static class HobbyXpDbSeeder
{
    public static void Seed(ModelBuilder modelBuilder)
    {
        SeedMedalDefinitions(modelBuilder);
        SeedAchievementRules(modelBuilder);
    }

    private static void SeedMedalDefinitions(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<MedalDefinition>().HasData(
            new MedalDefinition
            {
                Id = 1,
                Code = MedalCode.GoldRace,
                Name = "Medalla de Oro",
                Description = "Completaste una carrera oficial.",
                UnlockHint = "Marca una carrera oficial como completada.",
                CreatedAt = SeedTimestamp
            },
            new MedalDefinition
            {
                Id = 2,
                Code = MedalCode.PlatinumGame,
                Name = "Medalla de Platino",
                Description = "Platinaste un videojuego al 100%.",
                UnlockHint = "Lleva un videojuego al 100% de completitud.",
                CreatedAt = SeedTimestamp
            },
            new MedalDefinition
            {
                Id = 3,
                Code = MedalCode.ProgressiveOverload,
                Name = "Sobrecarga Progresiva",
                Description = "Superaste tu récord histórico en gimnasio.",
                UnlockHint = "Mejora peso o tiempo respecto a tu máximo anterior.",
                CreatedAt = SeedTimestamp
            });
    }

    private static void SeedAchievementRules(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AchievementRule>().HasData(
            new AchievementRule
            {
                Id = 1,
                ActionType = AchievementActionType.RunningKilometer,
                DisplayName = "Running por kilómetro",
                UnitLabel = "km",
                PointsPerUnit = 10m,
                IsActive = true,
                CreatedAt = SeedTimestamp
            },
            new AchievementRule
            {
                Id = 2,
                ActionType = AchievementActionType.GymWorkoutSaved,
                DisplayName = "Sesión de gimnasio",
                UnitLabel = "sesión",
                PointsPerUnit = 25m,
                IsActive = true,
                CreatedAt = SeedTimestamp
            },
            new AchievementRule
            {
                Id = 3,
                ActionType = AchievementActionType.ProgressiveOverload,
                DisplayName = "Sobrecarga progresiva",
                UnitLabel = "logro",
                PointsPerUnit = 0m,
                FlatBonusPoints = 150,
                IsActive = true,
                CreatedAt = SeedTimestamp
            },
            new AchievementRule
            {
                Id = 4,
                ActionType = AchievementActionType.OfficialRaceCompleted,
                DisplayName = "Carrera oficial completada",
                UnitLabel = "carrera",
                PointsPerUnit = 0m,
                FlatBonusPoints = 500,
                IsActive = true,
                CreatedAt = SeedTimestamp
            },
            new AchievementRule
            {
                Id = 5,
                ActionType = AchievementActionType.PuzzleCompleted,
                DisplayName = "Rompecabezas completado",
                UnitLabel = "rompecabezas",
                PointsPerUnit = 50m,
                IsActive = true,
                CreatedAt = SeedTimestamp
            },
            new AchievementRule
            {
                Id = 6,
                ActionType = AchievementActionType.MediaCompleted,
                DisplayName = "Serie o película terminada",
                UnitLabel = "obra",
                PointsPerUnit = 30m,
                IsActive = true,
                CreatedAt = SeedTimestamp
            },
            new AchievementRule
            {
                Id = 7,
                ActionType = AchievementActionType.VideoGamePercent,
                DisplayName = "Avance de videojuego",
                UnitLabel = "%",
                PointsPerUnit = 10m,
                IsActive = true,
                CreatedAt = SeedTimestamp
            },
            new AchievementRule
            {
                Id = 8,
                ActionType = AchievementActionType.VideoGamePlatinum,
                DisplayName = "Videojuego platinado",
                UnitLabel = "juego",
                PointsPerUnit = 0m,
                FlatBonusPoints = 1000,
                IsActive = true,
                CreatedAt = SeedTimestamp
            },
            new AchievementRule
            {
                Id = 9,
                ActionType = AchievementActionType.BookPageRead,
                DisplayName = "Página leída",
                UnitLabel = "página",
                PointsPerUnit = 1m,
                IsActive = true,
                CreatedAt = SeedTimestamp
            },
            new AchievementRule
            {
                Id = 10,
                ActionType = AchievementActionType.BookCompleted,
                DisplayName = "Libro terminado",
                UnitLabel = "libro",
                PointsPerUnit = 0m,
                FlatBonusPoints = 200,
                IsActive = true,
                CreatedAt = SeedTimestamp
            },
            new AchievementRule
            {
                Id = 11,
                ActionType = AchievementActionType.CourseCompleted,
                DisplayName = "Curso terminado",
                UnitLabel = "curso",
                PointsPerUnit = 0m,
                FlatBonusPoints = 100,
                IsActive = true,
                CreatedAt = SeedTimestamp
            },
            new AchievementRule
            {
                Id = 12,
                ActionType = AchievementActionType.CourseSessionCompleted,
                DisplayName = "Sesión de curso",
                UnitLabel = "sesión",
                PointsPerUnit = 10m,
                IsActive = true,
                CreatedAt = SeedTimestamp
            });
    }

    private static readonly DateTime SeedTimestamp = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
}
