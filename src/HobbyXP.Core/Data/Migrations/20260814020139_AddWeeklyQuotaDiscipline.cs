using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HobbyXP.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddWeeklyQuotaDiscipline : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BookReadingLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    BookId = table.Column<int>(type: "INTEGER", nullable: false),
                    ReadDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    PagesDone = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BookReadingLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BookReadingLogs_Books_BookId",
                        column: x => x.BookId,
                        principalTable: "Books",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "VideoGameProgressLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    VideoGameId = table.Column<int>(type: "INTEGER", nullable: false),
                    ProgressDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    PercentDelta = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VideoGameProgressLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VideoGameProgressLogs_VideoGames_VideoGameId",
                        column: x => x.VideoGameId,
                        principalTable: "VideoGames",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "WeeklyQuotaEvaluations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    SourceType = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    WeekStartUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    RequiredPrimary = table.Column<int>(type: "INTEGER", nullable: false),
                    ActualPrimary = table.Column<int>(type: "INTEGER", nullable: false),
                    RequiredSecondary = table.Column<int>(type: "INTEGER", nullable: false),
                    ActualSecondary = table.Column<int>(type: "INTEGER", nullable: false),
                    Status = table.Column<string>(type: "TEXT", maxLength: 16, nullable: false),
                    HobbyXpRevoked = table.Column<int>(type: "INTEGER", nullable: false),
                    GlobalXpRevoked = table.Column<int>(type: "INTEGER", nullable: false),
                    HobbyLevelBefore = table.Column<int>(type: "INTEGER", nullable: false),
                    HobbyLevelAfter = table.Column<int>(type: "INTEGER", nullable: false),
                    PenalizedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    RestoredAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WeeklyQuotaEvaluations", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BookReadingLogs_BookId_ReadDate",
                table: "BookReadingLogs",
                columns: new[] { "BookId", "ReadDate" });

            migrationBuilder.CreateIndex(
                name: "IX_VideoGameProgressLogs_VideoGameId_ProgressDate",
                table: "VideoGameProgressLogs",
                columns: new[] { "VideoGameId", "ProgressDate" });

            migrationBuilder.CreateIndex(
                name: "IX_WeeklyQuotaEvaluations_SourceType_WeekStartUtc",
                table: "WeeklyQuotaEvaluations",
                columns: new[] { "SourceType", "WeekStartUtc" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BookReadingLogs");

            migrationBuilder.DropTable(
                name: "VideoGameProgressLogs");

            migrationBuilder.DropTable(
                name: "WeeklyQuotaEvaluations");
        }
    }
}
