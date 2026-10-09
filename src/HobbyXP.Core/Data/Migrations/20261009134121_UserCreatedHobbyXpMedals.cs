using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HobbyXP.Data.Migrations
{
    /// <inheritdoc />
    public partial class UserCreatedHobbyXpMedals : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Sustituye el catálogo fijo: el usuario crea medallas por hobby + umbral XP.
            migrationBuilder.Sql("DELETE FROM EarnedMedals;");
            migrationBuilder.Sql("DELETE FROM MedalDefinitions;");

            migrationBuilder.AddColumn<string>(
                name: "SourceType",
                table: "MedalDefinitions",
                type: "TEXT",
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "XpThreshold",
                table: "MedalDefinitions",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_MedalDefinitions_SourceType_XpThreshold",
                table: "MedalDefinitions",
                columns: new[] { "SourceType", "XpThreshold" },
                unique: true);

            migrationBuilder.Sql(
                """
                UPDATE PlayerProfiles
                SET HonorTitle = NULL,
                    LastSeenEarnedMedalCount = 0,
                    UpdatedAt = CURRENT_TIMESTAMP
                WHERE HonorTitle IS NOT NULL OR LastSeenEarnedMedalCount <> 0;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_MedalDefinitions_SourceType_XpThreshold",
                table: "MedalDefinitions");

            migrationBuilder.DropColumn(
                name: "SourceType",
                table: "MedalDefinitions");

            migrationBuilder.DropColumn(
                name: "XpThreshold",
                table: "MedalDefinitions");
        }
    }
}
