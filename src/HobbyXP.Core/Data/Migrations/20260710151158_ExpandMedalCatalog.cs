using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace HobbyXP.Data.Migrations
{
    /// <inheritdoc />
    public partial class ExpandMedalCatalog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "MedalDefinitions",
                columns: new[] { "Id", "Code", "CreatedAt", "Description", "IconPath", "Name", "UnlockHint", "UpdatedAt" },
                values: new object[,]
                {
                    { 4, "BookCompleted", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Terminaste un libro de principio a fin.", null, "Lector Voraz", "Marca un libro como completado al leer todas sus páginas.", null },
                    { 5, "CourseCompleted", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Completaste todas las sesiones de un curso.", null, "Graduado", "Finaliza un curso registrando todas sus sesiones.", null },
                    { 6, "PuzzleMaster", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Completaste un rompecabezas.", null, "Maestro del Puzzle", "Registra un rompecabezas como terminado.", null },
                    { 7, "MediaMarathon", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Terminaste una serie o película.", null, "Maratón Cultural", "Registra una obra de entretenimiento como completada.", null }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "MedalDefinitions",
                keyColumn: "Id",
                keyValue: 4);

            migrationBuilder.DeleteData(
                table: "MedalDefinitions",
                keyColumn: "Id",
                keyValue: 5);

            migrationBuilder.DeleteData(
                table: "MedalDefinitions",
                keyColumn: "Id",
                keyValue: 6);

            migrationBuilder.DeleteData(
                table: "MedalDefinitions",
                keyColumn: "Id",
                keyValue: 7);
        }
    }
}
