using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HobbyXP.Data.Migrations
{
    /// <inheritdoc />
    public partial class CourseSessionProgress : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "SessionCount",
                table: "Courses",
                newName: "TotalSessions");

            migrationBuilder.AlterColumn<DateTime>(
                name: "CompletedAt",
                table: "Courses",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "TEXT");

            migrationBuilder.AddColumn<int>(
                name: "SessionsCompleted",
                table: "Courses",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "Courses",
                type: "TEXT",
                maxLength: 16,
                nullable: false,
                defaultValue: "InProgress");

            migrationBuilder.Sql(
                """
                UPDATE Courses
                SET SessionsCompleted = TotalSessions,
                    Status = 'Completed'
                WHERE CompletedAt IS NOT NULL;
                """);

            migrationBuilder.CreateTable(
                name: "CourseSessionLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    CourseId = table.Column<int>(type: "INTEGER", nullable: false),
                    SessionDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    SessionsDone = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CourseSessionLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CourseSessionLogs_Courses_CourseId",
                        column: x => x.CourseId,
                        principalTable: "Courses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.UpdateData(
                table: "AchievementRules",
                keyColumn: "Id",
                keyValue: 11,
                columns: new[] { "DisplayName", "FlatBonusPoints", "PointsPerUnit" },
                values: new object[] { "Curso terminado", 100, 0m });

            migrationBuilder.InsertData(
                table: "AchievementRules",
                columns: new[] { "Id", "ActionType", "CreatedAt", "DisplayName", "FlatBonusPoints", "IsActive", "PointsPerUnit", "UnitLabel", "UpdatedAt" },
                values: new object[] { 12, "CourseSessionCompleted", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Sesión de curso", null, true, 10m, "sesión", null });

            migrationBuilder.CreateIndex(
                name: "IX_Courses_Status",
                table: "Courses",
                column: "Status");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Courses_SessionsCompleted",
                table: "Courses",
                sql: "[SessionsCompleted] >= 0 AND [SessionsCompleted] <= [TotalSessions]");

            migrationBuilder.CreateIndex(
                name: "IX_CourseSessionLogs_CourseId_SessionDate",
                table: "CourseSessionLogs",
                columns: new[] { "CourseId", "SessionDate" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CourseSessionLogs");

            migrationBuilder.DropIndex(
                name: "IX_Courses_Status",
                table: "Courses");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Courses_SessionsCompleted",
                table: "Courses");

            migrationBuilder.DeleteData(
                table: "AchievementRules",
                keyColumn: "Id",
                keyValue: 12);

            migrationBuilder.DropColumn(
                name: "SessionsCompleted",
                table: "Courses");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "Courses");

            migrationBuilder.AlterColumn<DateTime>(
                name: "CompletedAt",
                table: "Courses",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                oldClrType: typeof(DateTime),
                oldType: "TEXT",
                oldNullable: true);

            migrationBuilder.RenameColumn(
                name: "TotalSessions",
                table: "Courses",
                newName: "SessionCount");

            migrationBuilder.UpdateData(
                table: "AchievementRules",
                keyColumn: "Id",
                keyValue: 11,
                columns: new[] { "DisplayName", "FlatBonusPoints", "PointsPerUnit" },
                values: new object[] { "Curso completado", null, 100m });
        }
    }
}
