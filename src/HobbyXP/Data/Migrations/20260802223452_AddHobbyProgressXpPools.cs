using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HobbyXP.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddHobbyProgressXpPools : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsGlobal",
                table: "XpTransactions",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "SourceType",
                table: "XpTransactions",
                type: "TEXT",
                maxLength: 32,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "HobbyProgresses",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PlayerProfileId = table.Column<int>(type: "INTEGER", nullable: false),
                    SourceType = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    CurrentLevel = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 1),
                    TotalXp = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HobbyProgresses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HobbyProgresses_PlayerProfiles_PlayerProfileId",
                        column: x => x.PlayerProfileId,
                        principalTable: "PlayerProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_XpTransactions_SourceType",
                table: "XpTransactions",
                column: "SourceType");

            migrationBuilder.CreateIndex(
                name: "IX_HobbyProgresses_PlayerProfileId_SourceType",
                table: "HobbyProgresses",
                columns: new[] { "PlayerProfileId", "SourceType" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "HobbyProgresses");

            migrationBuilder.DropIndex(
                name: "IX_XpTransactions_SourceType",
                table: "XpTransactions");

            migrationBuilder.DropColumn(
                name: "IsGlobal",
                table: "XpTransactions");

            migrationBuilder.DropColumn(
                name: "SourceType",
                table: "XpTransactions");
        }
    }
}
