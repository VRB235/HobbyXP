using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HobbyXP.Data.Migrations
{
    /// <inheritdoc />
    public partial class RebalanceActivityXpRules : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "AchievementRules",
                keyColumn: "Id",
                keyValue: 2,
                column: "PointsPerUnit",
                value: 10m);

            migrationBuilder.UpdateData(
                table: "AchievementRules",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "DisplayName", "FlatBonusPoints", "PointsPerUnit", "UnitLabel" },
                values: new object[] { "Récord personal (ejercicio)", null, 5m, "ejercicio" });

            migrationBuilder.UpdateData(
                table: "AchievementRules",
                keyColumn: "Id",
                keyValue: 5,
                column: "PointsPerUnit",
                value: 100m);

            migrationBuilder.UpdateData(
                table: "AchievementRules",
                keyColumn: "Id",
                keyValue: 6,
                columns: new[] { "DisplayName", "PointsPerUnit", "UnitLabel" },
                values: new object[] { "Película terminada", 20m, "película" });

            migrationBuilder.UpdateData(
                table: "AchievementRules",
                keyColumn: "Id",
                keyValue: 13,
                columns: new[] { "DisplayName", "PointsPerUnit", "UnitLabel" },
                values: new object[] { "Serie (XP total repartido en capítulos)", 100m, "serie" });

            migrationBuilder.UpdateData(
                table: "AchievementRules",
                keyColumn: "Id",
                keyValue: 14,
                column: "PointsPerUnit",
                value: 2m);

            migrationBuilder.UpdateData(
                table: "AchievementRules",
                keyColumn: "Id",
                keyValue: 15,
                column: "FlatBonusPoints",
                value: 2);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "AchievementRules",
                keyColumn: "Id",
                keyValue: 2,
                column: "PointsPerUnit",
                value: 25m);

            migrationBuilder.UpdateData(
                table: "AchievementRules",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "DisplayName", "FlatBonusPoints", "PointsPerUnit", "UnitLabel" },
                values: new object[] { "Sobrecarga progresiva", 150, 0m, "logro" });

            migrationBuilder.UpdateData(
                table: "AchievementRules",
                keyColumn: "Id",
                keyValue: 5,
                column: "PointsPerUnit",
                value: 50m);

            migrationBuilder.UpdateData(
                table: "AchievementRules",
                keyColumn: "Id",
                keyValue: 6,
                columns: new[] { "DisplayName", "PointsPerUnit", "UnitLabel" },
                values: new object[] { "Serie o película terminada", 30m, "obra" });

            migrationBuilder.UpdateData(
                table: "AchievementRules",
                keyColumn: "Id",
                keyValue: 13,
                columns: new[] { "DisplayName", "PointsPerUnit", "UnitLabel" },
                values: new object[] { "Capítulo de serie", 5m, "capítulo" });

            migrationBuilder.UpdateData(
                table: "AchievementRules",
                keyColumn: "Id",
                keyValue: 14,
                column: "PointsPerUnit",
                value: 15m);

            migrationBuilder.UpdateData(
                table: "AchievementRules",
                keyColumn: "Id",
                keyValue: 15,
                column: "FlatBonusPoints",
                value: 40);
        }
    }
}
