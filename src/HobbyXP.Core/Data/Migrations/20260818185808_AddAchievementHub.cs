using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HobbyXP.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAchievementHub : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "RedeemedCostInPoints",
                table: "Rewards",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DisciplineImmunityUntilUtc",
                table: "PlayerProfiles",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "EquippedRewardId",
                table: "PlayerProfiles",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "HonorTitle",
                table: "PlayerProfiles",
                type: "TEXT",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "LastSeenEarnedMedalCount",
                table: "PlayerProfiles",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RedeemedCostInPoints",
                table: "Rewards");

            migrationBuilder.DropColumn(
                name: "DisciplineImmunityUntilUtc",
                table: "PlayerProfiles");

            migrationBuilder.DropColumn(
                name: "EquippedRewardId",
                table: "PlayerProfiles");

            migrationBuilder.DropColumn(
                name: "HonorTitle",
                table: "PlayerProfiles");

            migrationBuilder.DropColumn(
                name: "LastSeenEarnedMedalCount",
                table: "PlayerProfiles");
        }
    }
}
