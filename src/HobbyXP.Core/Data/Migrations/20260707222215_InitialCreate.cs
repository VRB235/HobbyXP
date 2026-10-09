using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace HobbyXP.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AchievementRules",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ActionType = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    DisplayName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    UnitLabel = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    PointsPerUnit = table.Column<decimal>(type: "TEXT", precision: 10, scale: 2, nullable: false),
                    FlatBonusPoints = table.Column<int>(type: "INTEGER", nullable: true),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AchievementRules", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Books",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Title = table.Column<string>(type: "TEXT", maxLength: 300, nullable: false),
                    Author = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    TotalPages = table.Column<int>(type: "INTEGER", nullable: false),
                    PagesRead = table.Column<int>(type: "INTEGER", nullable: false),
                    Status = table.Column<string>(type: "TEXT", maxLength: 16, nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    XpEarned = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Books", x => x.Id);
                    table.CheckConstraint("CK_Books_PagesRead", "[PagesRead] >= 0 AND [PagesRead] <= [TotalPages]");
                });

            migrationBuilder.CreateTable(
                name: "Courses",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 300, nullable: false),
                    Platform = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    XpEarned = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Courses", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Exercises",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 150, nullable: false),
                    ExerciseType = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Exercises", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "GymWorkouts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    WorkoutDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Notes = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    XpEarned = table.Column<int>(type: "INTEGER", nullable: false),
                    TriggeredProgressiveOverload = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GymWorkouts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MedalDefinitions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Code = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 150, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    UnlockHint = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    IconPath = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MedalDefinitions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MediaEntries",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Title = table.Column<string>(type: "TEXT", maxLength: 300, nullable: false),
                    MediaType = table.Column<string>(type: "TEXT", maxLength: 16, nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    XpEarned = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MediaEntries", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Milestones",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Title = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    PointsEarned = table.Column<int>(type: "INTEGER", nullable: false),
                    SourceType = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    SourceEntityId = table.Column<int>(type: "INTEGER", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Milestones", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "OfficialRaces",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    DistanceKm = table.Column<decimal>(type: "TEXT", precision: 8, scale: 3, nullable: false),
                    EventDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Location = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    Description = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    IsCompleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    BonusXpAwarded = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OfficialRaces", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PlayerProfiles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    CurrentLevel = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 1),
                    TotalXp = table.Column<int>(type: "INTEGER", nullable: false),
                    BaseXpPerLevel = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 1000),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlayerProfiles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Puzzles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    PieceCount = table.Column<int>(type: "INTEGER", nullable: false),
                    Category = table.Column<string>(type: "TEXT", maxLength: 8, nullable: false),
                    PhotoPath = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    XpEarned = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Puzzles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Rewards",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    CostInPoints = table.Column<int>(type: "INTEGER", nullable: false),
                    Status = table.Column<string>(type: "TEXT", maxLength: 16, nullable: false),
                    RedeemedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Rewards", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "VideoGames",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Title = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Platform = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    CompletionPercentage = table.Column<int>(type: "INTEGER", nullable: false),
                    Status = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    StartedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    PlatinumUnlockedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    XpEarned = table.Column<int>(type: "INTEGER", nullable: false),
                    PlatinumBonusXp = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VideoGames", x => x.Id);
                    table.CheckConstraint("CK_VideoGames_CompletionPercentage", "[CompletionPercentage] >= 0 AND [CompletionPercentage] <= 100");
                });

            migrationBuilder.CreateTable(
                name: "GymWorkoutEntries",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    GymWorkoutId = table.Column<int>(type: "INTEGER", nullable: false),
                    ExerciseId = table.Column<int>(type: "INTEGER", nullable: false),
                    ExerciseType = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    Sets = table.Column<int>(type: "INTEGER", nullable: false),
                    Repetitions = table.Column<int>(type: "INTEGER", nullable: true),
                    WeightKg = table.Column<decimal>(type: "TEXT", precision: 8, scale: 2, nullable: true),
                    Duration = table.Column<TimeSpan>(type: "TEXT", nullable: true),
                    SortOrder = table.Column<int>(type: "INTEGER", nullable: false),
                    IsPersonalRecord = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GymWorkoutEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GymWorkoutEntries_Exercises_ExerciseId",
                        column: x => x.ExerciseId,
                        principalTable: "Exercises",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_GymWorkoutEntries_GymWorkouts_GymWorkoutId",
                        column: x => x.GymWorkoutId,
                        principalTable: "GymWorkouts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EarnedMedals",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    MedalDefinitionId = table.Column<int>(type: "INTEGER", nullable: false),
                    EarnedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    SourceEntityType = table.Column<string>(type: "TEXT", nullable: true),
                    SourceEntityId = table.Column<int>(type: "INTEGER", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EarnedMedals", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EarnedMedals_MedalDefinitions_MedalDefinitionId",
                        column: x => x.MedalDefinitionId,
                        principalTable: "MedalDefinitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RunningSessions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    DistanceKm = table.Column<decimal>(type: "TEXT", precision: 8, scale: 3, nullable: false),
                    Duration = table.Column<TimeSpan>(type: "TEXT", nullable: false),
                    PaceMinPerKm = table.Column<double>(type: "REAL", precision: 8, scale: 3, nullable: false),
                    CarreraId = table.Column<int>(type: "INTEGER", nullable: true),
                    RecordedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Notes = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    XpEarned = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RunningSessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RunningSessions_OfficialRaces_CarreraId",
                        column: x => x.CarreraId,
                        principalTable: "OfficialRaces",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "XpTransactions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PlayerProfileId = table.Column<int>(type: "INTEGER", nullable: false),
                    Amount = table.Column<int>(type: "INTEGER", nullable: false),
                    ActionType = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    SourceEntityType = table.Column<string>(type: "TEXT", nullable: true),
                    SourceEntityId = table.Column<int>(type: "INTEGER", nullable: true),
                    EarnedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_XpTransactions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_XpTransactions_PlayerProfiles_PlayerProfileId",
                        column: x => x.PlayerProfileId,
                        principalTable: "PlayerProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "AchievementRules",
                columns: new[] { "Id", "ActionType", "CreatedAt", "DisplayName", "FlatBonusPoints", "IsActive", "PointsPerUnit", "UnitLabel", "UpdatedAt" },
                values: new object[,]
                {
                    { 1, "RunningKilometer", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Running por kilómetro", null, true, 10m, "km", null },
                    { 2, "GymWorkoutSaved", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Sesión de gimnasio", null, true, 25m, "sesión", null },
                    { 3, "ProgressiveOverload", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Sobrecarga progresiva", 150, true, 0m, "logro", null },
                    { 4, "OfficialRaceCompleted", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Carrera oficial completada", 500, true, 0m, "carrera", null },
                    { 5, "PuzzleCompleted", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Rompecabezas completado", null, true, 50m, "rompecabezas", null },
                    { 6, "MediaCompleted", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Serie o película terminada", null, true, 30m, "obra", null },
                    { 7, "VideoGamePercent", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Avance de videojuego", null, true, 10m, "%", null },
                    { 8, "VideoGamePlatinum", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Videojuego platinado", 1000, true, 0m, "juego", null },
                    { 9, "BookPageRead", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Página leída", null, true, 1m, "página", null },
                    { 10, "BookCompleted", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Libro terminado", 200, true, 0m, "libro", null },
                    { 11, "CourseCompleted", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Curso completado", null, true, 100m, "curso", null }
                });

            migrationBuilder.InsertData(
                table: "MedalDefinitions",
                columns: new[] { "Id", "Code", "CreatedAt", "Description", "IconPath", "Name", "UnlockHint", "UpdatedAt" },
                values: new object[,]
                {
                    { 1, "GoldRace", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Completaste una carrera oficial.", null, "Medalla de Oro", "Marca una carrera oficial como completada.", null },
                    { 2, "PlatinumGame", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Platinaste un videojuego al 100%.", null, "Medalla de Platino", "Lleva un videojuego al 100% de completitud.", null },
                    { 3, "ProgressiveOverload", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Superaste tu récord histórico en gimnasio.", null, "Sobrecarga Progresiva", "Mejora peso o tiempo respecto a tu máximo anterior.", null }
                });

            migrationBuilder.CreateIndex(
                name: "IX_AchievementRules_ActionType",
                table: "AchievementRules",
                column: "ActionType",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Books_Status",
                table: "Books",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_EarnedMedals_MedalDefinitionId_EarnedAt",
                table: "EarnedMedals",
                columns: new[] { "MedalDefinitionId", "EarnedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Exercises_Name",
                table: "Exercises",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GymWorkoutEntries_ExerciseId_CreatedAt",
                table: "GymWorkoutEntries",
                columns: new[] { "ExerciseId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_GymWorkoutEntries_GymWorkoutId",
                table: "GymWorkoutEntries",
                column: "GymWorkoutId");

            migrationBuilder.CreateIndex(
                name: "IX_GymWorkouts_WorkoutDate",
                table: "GymWorkouts",
                column: "WorkoutDate");

            migrationBuilder.CreateIndex(
                name: "IX_MedalDefinitions_Code",
                table: "MedalDefinitions",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MediaEntries_CompletedAt",
                table: "MediaEntries",
                column: "CompletedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Milestones_CompletedAt",
                table: "Milestones",
                column: "CompletedAt");

            migrationBuilder.CreateIndex(
                name: "IX_OfficialRaces_IsCompleted",
                table: "OfficialRaces",
                column: "IsCompleted");

            migrationBuilder.CreateIndex(
                name: "IX_Rewards_Status",
                table: "Rewards",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_RunningSessions_CarreraId",
                table: "RunningSessions",
                column: "CarreraId");

            migrationBuilder.CreateIndex(
                name: "IX_RunningSessions_RecordedAt",
                table: "RunningSessions",
                column: "RecordedAt");

            migrationBuilder.CreateIndex(
                name: "IX_VideoGames_CompletionPercentage",
                table: "VideoGames",
                column: "CompletionPercentage");

            migrationBuilder.CreateIndex(
                name: "IX_VideoGames_Status",
                table: "VideoGames",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_XpTransactions_EarnedAt",
                table: "XpTransactions",
                column: "EarnedAt");

            migrationBuilder.CreateIndex(
                name: "IX_XpTransactions_PlayerProfileId",
                table: "XpTransactions",
                column: "PlayerProfileId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AchievementRules");

            migrationBuilder.DropTable(
                name: "Books");

            migrationBuilder.DropTable(
                name: "Courses");

            migrationBuilder.DropTable(
                name: "EarnedMedals");

            migrationBuilder.DropTable(
                name: "GymWorkoutEntries");

            migrationBuilder.DropTable(
                name: "MediaEntries");

            migrationBuilder.DropTable(
                name: "Milestones");

            migrationBuilder.DropTable(
                name: "Puzzles");

            migrationBuilder.DropTable(
                name: "Rewards");

            migrationBuilder.DropTable(
                name: "RunningSessions");

            migrationBuilder.DropTable(
                name: "VideoGames");

            migrationBuilder.DropTable(
                name: "XpTransactions");

            migrationBuilder.DropTable(
                name: "MedalDefinitions");

            migrationBuilder.DropTable(
                name: "Exercises");

            migrationBuilder.DropTable(
                name: "GymWorkouts");

            migrationBuilder.DropTable(
                name: "OfficialRaces");

            migrationBuilder.DropTable(
                name: "PlayerProfiles");
        }
    }
}
