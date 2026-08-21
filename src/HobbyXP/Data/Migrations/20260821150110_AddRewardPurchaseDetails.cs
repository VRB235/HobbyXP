using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HobbyXP.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddRewardPurchaseDetails : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ImagePath",
                table: "Rewards",
                type: "TEXT",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Price",
                table: "Rewards",
                type: "TEXT",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PurchaseUrl",
                table: "Rewards",
                type: "TEXT",
                maxLength: 2000,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ImagePath",
                table: "Rewards");

            migrationBuilder.DropColumn(
                name: "Price",
                table: "Rewards");

            migrationBuilder.DropColumn(
                name: "PurchaseUrl",
                table: "Rewards");
        }
    }
}
