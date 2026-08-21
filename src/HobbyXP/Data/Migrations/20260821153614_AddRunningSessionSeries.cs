using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HobbyXP.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddRunningSessionSeries : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RunningSessionSeries",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    RunningSessionId = table.Column<int>(type: "INTEGER", nullable: false),
                    SortOrder = table.Column<int>(type: "INTEGER", nullable: false),
                    DistanceKm = table.Column<decimal>(type: "TEXT", precision: 8, scale: 3, nullable: false),
                    Duration = table.Column<TimeSpan>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RunningSessionSeries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RunningSessionSeries_RunningSessions_RunningSessionId",
                        column: x => x.RunningSessionId,
                        principalTable: "RunningSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RunningSessionSeries_RunningSessionId_SortOrder",
                table: "RunningSessionSeries",
                columns: new[] { "RunningSessionId", "SortOrder" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RunningSessionSeries");
        }
    }
}
