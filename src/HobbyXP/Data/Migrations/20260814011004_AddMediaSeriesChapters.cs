using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HobbyXP.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddMediaSeriesChapters : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MediaSeries",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Title = table.Column<string>(type: "TEXT", maxLength: 300, nullable: false),
                    TotalChapters = table.Column<int>(type: "INTEGER", nullable: false),
                    ChaptersWatched = table.Column<int>(type: "INTEGER", nullable: false),
                    Status = table.Column<string>(type: "TEXT", maxLength: 16, nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    XpEarned = table.Column<int>(type: "INTEGER", nullable: false),
                    CompletedMediaEntryId = table.Column<int>(type: "INTEGER", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MediaSeries", x => x.Id);
                    table.CheckConstraint("CK_MediaSeries_ChaptersWatched", "[ChaptersWatched] >= 0 AND [ChaptersWatched] <= [TotalChapters]");
                });

            migrationBuilder.CreateTable(
                name: "MediaSeriesChapterLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    MediaSeriesId = table.Column<int>(type: "INTEGER", nullable: false),
                    WatchDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ChaptersDone = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MediaSeriesChapterLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MediaSeriesChapterLogs_MediaSeries_MediaSeriesId",
                        column: x => x.MediaSeriesId,
                        principalTable: "MediaSeries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "AchievementRules",
                columns: new[] { "Id", "ActionType", "CreatedAt", "DisplayName", "FlatBonusPoints", "IsActive", "PointsPerUnit", "UnitLabel", "UpdatedAt" },
                values: new object[] { 13, "MediaChapterWatched", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Capítulo de serie", null, true, 5m, "capítulo", null });

            migrationBuilder.CreateIndex(
                name: "IX_MediaSeries_Status",
                table: "MediaSeries",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_MediaSeriesChapterLogs_MediaSeriesId_WatchDate",
                table: "MediaSeriesChapterLogs",
                columns: new[] { "MediaSeriesId", "WatchDate" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MediaSeriesChapterLogs");

            migrationBuilder.DropTable(
                name: "MediaSeries");

            migrationBuilder.DeleteData(
                table: "AchievementRules",
                keyColumn: "Id",
                keyValue: 13);
        }
    }
}
