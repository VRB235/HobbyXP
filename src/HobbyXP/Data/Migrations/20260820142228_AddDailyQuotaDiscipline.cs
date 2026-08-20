using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HobbyXP.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddDailyQuotaDiscipline : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DailyQuotaEvaluations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    SourceType = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    DayUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    RequiredPrimary = table.Column<int>(type: "INTEGER", nullable: false),
                    ActualPrimary = table.Column<int>(type: "INTEGER", nullable: false),
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
                    table.PrimaryKey("PK_DailyQuotaEvaluations", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DailyQuotaEvaluations_SourceType_DayUtc",
                table: "DailyQuotaEvaluations",
                columns: new[] { "SourceType", "DayUtc" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DailyQuotaEvaluations");
        }
    }
}
