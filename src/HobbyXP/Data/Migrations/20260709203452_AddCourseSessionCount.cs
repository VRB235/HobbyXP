using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HobbyXP.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCourseSessionCount : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "SessionCount",
                table: "Courses",
                type: "INTEGER",
                nullable: false,
                defaultValue: 1);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SessionCount",
                table: "Courses");
        }
    }
}
