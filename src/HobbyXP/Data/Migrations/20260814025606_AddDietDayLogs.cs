using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace HobbyXP.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddDietDayLogs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DietDayLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    DayDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    BreakfastStatus = table.Column<string>(type: "TEXT", maxLength: 16, nullable: false),
                    LunchStatus = table.Column<string>(type: "TEXT", maxLength: 16, nullable: false),
                    DinnerStatus = table.Column<string>(type: "TEXT", maxLength: 16, nullable: false),
                    SnackStatus = table.Column<string>(type: "TEXT", maxLength: 16, nullable: false),
                    OnPlanCount = table.Column<int>(type: "INTEGER", nullable: false),
                    Notes = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    XpEarned = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DietDayLogs", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "AchievementRules",
                columns: new[] { "Id", "ActionType", "CreatedAt", "DisplayName", "FlatBonusPoints", "IsActive", "PointsPerUnit", "UnitLabel", "UpdatedAt" },
                values: new object[,]
                {
                    { 14, "DietMealOnPlan", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Comida en plan", null, true, 15m, "comida", null },
                    { 15, "DietPerfectDay", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Día perfecto de dieta", 40, true, 0m, "día", null }
                });

            migrationBuilder.InsertData(
                table: "MedalDefinitions",
                columns: new[] { "Id", "Code", "CreatedAt", "Description", "IconPath", "Name", "UnlockHint", "UpdatedAt" },
                values: new object[,]
                {
                    { 53, "DietGoodDays1", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Completaste tu primer día bueno de dieta (3 de 4 comidas en plan).", "Assets/Medals/gym-workout.png", "Primer Plato", "Registra un día con al menos 3 comidas en plan.", null },
                    { 54, "DietGoodDays10", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Diez días buenos de dieta.", "Assets/Medals/gym-workout.png", "Disciplina en la Mesa", "Acumula 10 días con al menos 3 comidas en plan.", null },
                    { 55, "DietGoodDays50", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Cincuenta días buenos. El plan ya es costumbre.", "Assets/Medals/gym-workout.png", "Hábito Forjado", "Acumula 50 días con al menos 3 comidas en plan.", null },
                    { 56, "DietPerfectDays7", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Siete días perfectos (4/4 en plan).", "Assets/Medals/progressive-overload.png", "Semana Impecable", "Acumula 7 días con las 4 comidas en plan.", null },
                    { 57, "DietPerfectDays30", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Treinta días perfectos. Adherencia de élite.", "Assets/Medals/progressive-overload.png", "Mes de Acero", "Acumula 30 días con las 4 comidas en plan.", null }
                });

            migrationBuilder.CreateIndex(
                name: "IX_DietDayLogs_DayDate",
                table: "DietDayLogs",
                column: "DayDate",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DietDayLogs");

            migrationBuilder.DeleteData(
                table: "AchievementRules",
                keyColumn: "Id",
                keyValue: 14);

            migrationBuilder.DeleteData(
                table: "AchievementRules",
                keyColumn: "Id",
                keyValue: 15);

            migrationBuilder.DeleteData(
                table: "MedalDefinitions",
                keyColumn: "Id",
                keyValue: 53);

            migrationBuilder.DeleteData(
                table: "MedalDefinitions",
                keyColumn: "Id",
                keyValue: 54);

            migrationBuilder.DeleteData(
                table: "MedalDefinitions",
                keyColumn: "Id",
                keyValue: 55);

            migrationBuilder.DeleteData(
                table: "MedalDefinitions",
                keyColumn: "Id",
                keyValue: 56);

            migrationBuilder.DeleteData(
                table: "MedalDefinitions",
                keyColumn: "Id",
                keyValue: 57);
        }
    }
}
