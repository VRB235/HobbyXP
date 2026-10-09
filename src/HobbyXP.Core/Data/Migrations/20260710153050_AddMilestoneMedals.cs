using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace HobbyXP.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddMilestoneMedals : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "MedalDefinitions",
                keyColumn: "Id",
                keyValue: 1,
                column: "Description",
                value: "Completaste tu primera carrera oficial.");

            migrationBuilder.UpdateData(
                table: "MedalDefinitions",
                keyColumn: "Id",
                keyValue: 2,
                column: "Description",
                value: "Platinaste tu primer videojuego al 100%.");

            migrationBuilder.UpdateData(
                table: "MedalDefinitions",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "Description", "UnlockHint" },
                values: new object[] { "Terminaste tu primer libro de principio a fin.", "Marca un libro como completado." });

            migrationBuilder.UpdateData(
                table: "MedalDefinitions",
                keyColumn: "Id",
                keyValue: 5,
                column: "Description",
                value: "Completaste tu primer curso.");

            migrationBuilder.UpdateData(
                table: "MedalDefinitions",
                keyColumn: "Id",
                keyValue: 6,
                column: "Description",
                value: "Completaste tu primer rompecabezas.");

            migrationBuilder.UpdateData(
                table: "MedalDefinitions",
                keyColumn: "Id",
                keyValue: 7,
                column: "Description",
                value: "Terminaste tu primera serie o película.");

            migrationBuilder.InsertData(
                table: "MedalDefinitions",
                columns: new[] { "Id", "Code", "CreatedAt", "Description", "IconPath", "Name", "UnlockHint", "UpdatedAt" },
                values: new object[,]
                {
                    { 8, "RacesCompleted3", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Tres carreras oficiales en tu historial.", null, "Podio en Entrenamiento", "Completa 3 carreras oficiales.", null },
                    { 9, "RacesCompleted5", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Cinco carreras oficiales conquistadas.", null, "Corredor Constante", "Completa 5 carreras oficiales.", null },
                    { 10, "RacesCompleted10", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Diez carreras oficiales en tu palmarés.", null, "Veterano del Asfalto", "Completa 10 carreras oficiales.", null },
                    { 11, "RacesCompleted25", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Veinticinco carreras oficiales. Eres imparable.", null, "Leyenda del Chip", "Completa 25 carreras oficiales.", null },
                    { 12, "RunningSessions10", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Diez sesiones de running registradas.", null, "Ritmo de Reloj", "Registra 10 sesiones de running.", null },
                    { 13, "RunningSessions50", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Cincuenta salidas al asfalto.", null, "Motor Cardíaco", "Registra 50 sesiones de running.", null },
                    { 14, "RunningSessions100", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Cien sesiones. El GPS te conoce por nombre.", null, "Máquina de Correr", "Registra 100 sesiones de running.", null },
                    { 15, "RunningKm100", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Acumulaste 100 km corriendo.", null, "Centurión del Kilómetro", "Corre un total de 100 km.", null },
                    { 16, "RunningKm500", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "500 km en tus piernas.", null, "Conquistador del Asfalto", "Corre un total de 500 km.", null },
                    { 17, "RunningKm1000", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "1.000 km. Distancia de leyenda.", null, "Ultra Alma", "Corre un total de 1.000 km.", null },
                    { 18, "ProgressiveOverload5", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Cinco récords personales en el gym.", null, "Forja de Hierro", "Logra 5 sobrecargas progresivas.", null },
                    { 19, "ProgressiveOverload10", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Diez PRs. Cada sesión te hace más fuerte.", null, "Titán en Evolución", "Logra 10 sobrecargas progresivas.", null },
                    { 20, "ProgressiveOverload25", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Veinticinco récords rotos. El gym tiembla.", null, "Coloso del Hierro", "Logra 25 sobrecargas progresivas.", null },
                    { 21, "GymWorkouts10", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Diez entrenamientos guardados.", null, "Hierro Temprano", "Registra 10 sesiones de gimnasio.", null },
                    { 22, "GymWorkouts50", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Cincuenta sesiones de gimnasio.", null, "Forja Personal", "Registra 50 sesiones de gimnasio.", null },
                    { 23, "GymWorkouts100", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Cien entrenamientos. Disciplina pura.", null, "Titán del Gym", "Registra 100 sesiones de gimnasio.", null },
                    { 24, "GymWorkouts250", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Doscientos cincuenta sesiones. Eres una institución.", null, "Monolito Humano", "Registra 250 sesiones de gimnasio.", null },
                    { 25, "PlatinumGames3", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Tres juegos al 100%.", null, "Coleccionista Platino", "Platina 3 videojuegos.", null },
                    { 26, "PlatinumGames5", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Cinco platinos en la estantería virtual.", null, "Salón Digital", "Platina 5 videojuegos.", null },
                    { 27, "PlatinumGames10", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Diez juegos al 100%. Completionista nato.", null, "Meta Absoluta", "Platina 10 videojuegos.", null },
                    { 28, "BooksCompleted5", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Cinco libros terminados.", null, "Club del Capítulo Cinco", "Completa 5 libros.", null },
                    { 29, "BooksCompleted10", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Diez libros en tu historial.", null, "Bibliófilo de Garra", "Completa 10 libros.", null },
                    { 30, "BooksCompleted25", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Veinticinco libros conquistados.", null, "Estantería Legendaria", "Completa 25 libros.", null },
                    { 31, "BooksCompleted50", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Cincuenta libros. Tu mente es un faro.", null, "Faros del Conocimiento", "Completa 50 libros.", null },
                    { 32, "BooksCompleted100", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Cien libros. Biblioteca personal de élite.", null, "Archivo del Sabio", "Completa 100 libros.", null },
                    { 33, "BookPages1000", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Leíste 1.000 páginas en total.", null, "Mil Páginas de Gloria", "Acumula 1.000 páginas leídas.", null },
                    { 34, "BookPages5000", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "5.000 páginas devoradas.", null, "Crónica Infinita", "Acumula 5.000 páginas leídas.", null },
                    { 35, "BookPages10000", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "10.000 páginas. Llevas una biblioteca encima.", null, "Biblioteca Ambulante", "Acumula 10.000 páginas leídas.", null },
                    { 36, "CoursesCompleted3", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Tres cursos terminados.", null, "Trilogía Académica", "Completa 3 cursos.", null },
                    { 37, "CoursesCompleted5", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Cinco cursos en tu currículo.", null, "Aprobado con Honores", "Completa 5 cursos.", null },
                    { 38, "CoursesCompleted10", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Diez cursos. Aprendiz perpetuo.", null, "Multidisciplinar", "Completa 10 cursos.", null },
                    { 39, "CoursesCompleted25", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Veinticinco cursos. Academia propia.", null, "Erudito Empírico", "Completa 25 cursos.", null },
                    { 40, "CourseSessions25", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Veinticinco sesiones de curso registradas.", null, "Asistencia Ejemplar", "Registra 25 sesiones de curso.", null },
                    { 41, "CourseSessions50", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Cincuenta sesiones de estudio.", null, "Beca de Disciplina", "Registra 50 sesiones de curso.", null },
                    { 42, "CourseSessions100", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Cien sesiones. El conocimiento te persigue.", null, "Doctor Honoris Causa", "Registra 100 sesiones de curso.", null },
                    { 43, "PuzzlesCompleted5", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Cinco rompecabezas resueltos.", null, "Pieza a Pieza", "Completa 5 rompecabezas.", null },
                    { 44, "PuzzlesCompleted10", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Diez puzzles. Orden desde el caos.", null, "Arquitecto del Caos", "Completa 10 rompecabezas.", null },
                    { 45, "PuzzlesCompleted25", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Veinticinco rompecabezas dominados.", null, "Visión Escher", "Completa 25 rompecabezas.", null },
                    { 46, "PuzzlesCompleted50", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Cincuenta puzzles. Ninguna pieza te resiste.", null, "Maestro del Encaje", "Completa 50 rompecabezas.", null },
                    { 47, "PuzzlesCompleted100", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Cien rompecabezas. Tu mesa es un templo.", null, "Leyenda del Tablero", "Completa 100 rompecabezas.", null },
                    { 48, "MediaCompleted5", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Cinco obras terminadas.", null, "Noche de Palomitas", "Completa 5 series o películas.", null },
                    { 49, "MediaCompleted10", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Diez maratones en el sofá.", null, "Binge Profesional", "Completa 10 series o películas.", null },
                    { 50, "MediaCompleted25", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Veinticinco obras en tu historial.", null, "Cinéfilo Serial", "Completa 25 series o películas.", null },
                    { 51, "MediaCompleted50", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Cincuenta obras. El control remoto es tuyo.", null, "Sofá Olímpico", "Completa 50 series o películas.", null },
                    { 52, "MediaCompleted100", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Cien obras. Algoritmo personalizado.", null, "Palmarés del Streaming", "Completa 100 series o películas.", null }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "MedalDefinitions",
                keyColumn: "Id",
                keyValue: 8);

            migrationBuilder.DeleteData(
                table: "MedalDefinitions",
                keyColumn: "Id",
                keyValue: 9);

            migrationBuilder.DeleteData(
                table: "MedalDefinitions",
                keyColumn: "Id",
                keyValue: 10);

            migrationBuilder.DeleteData(
                table: "MedalDefinitions",
                keyColumn: "Id",
                keyValue: 11);

            migrationBuilder.DeleteData(
                table: "MedalDefinitions",
                keyColumn: "Id",
                keyValue: 12);

            migrationBuilder.DeleteData(
                table: "MedalDefinitions",
                keyColumn: "Id",
                keyValue: 13);

            migrationBuilder.DeleteData(
                table: "MedalDefinitions",
                keyColumn: "Id",
                keyValue: 14);

            migrationBuilder.DeleteData(
                table: "MedalDefinitions",
                keyColumn: "Id",
                keyValue: 15);

            migrationBuilder.DeleteData(
                table: "MedalDefinitions",
                keyColumn: "Id",
                keyValue: 16);

            migrationBuilder.DeleteData(
                table: "MedalDefinitions",
                keyColumn: "Id",
                keyValue: 17);

            migrationBuilder.DeleteData(
                table: "MedalDefinitions",
                keyColumn: "Id",
                keyValue: 18);

            migrationBuilder.DeleteData(
                table: "MedalDefinitions",
                keyColumn: "Id",
                keyValue: 19);

            migrationBuilder.DeleteData(
                table: "MedalDefinitions",
                keyColumn: "Id",
                keyValue: 20);

            migrationBuilder.DeleteData(
                table: "MedalDefinitions",
                keyColumn: "Id",
                keyValue: 21);

            migrationBuilder.DeleteData(
                table: "MedalDefinitions",
                keyColumn: "Id",
                keyValue: 22);

            migrationBuilder.DeleteData(
                table: "MedalDefinitions",
                keyColumn: "Id",
                keyValue: 23);

            migrationBuilder.DeleteData(
                table: "MedalDefinitions",
                keyColumn: "Id",
                keyValue: 24);

            migrationBuilder.DeleteData(
                table: "MedalDefinitions",
                keyColumn: "Id",
                keyValue: 25);

            migrationBuilder.DeleteData(
                table: "MedalDefinitions",
                keyColumn: "Id",
                keyValue: 26);

            migrationBuilder.DeleteData(
                table: "MedalDefinitions",
                keyColumn: "Id",
                keyValue: 27);

            migrationBuilder.DeleteData(
                table: "MedalDefinitions",
                keyColumn: "Id",
                keyValue: 28);

            migrationBuilder.DeleteData(
                table: "MedalDefinitions",
                keyColumn: "Id",
                keyValue: 29);

            migrationBuilder.DeleteData(
                table: "MedalDefinitions",
                keyColumn: "Id",
                keyValue: 30);

            migrationBuilder.DeleteData(
                table: "MedalDefinitions",
                keyColumn: "Id",
                keyValue: 31);

            migrationBuilder.DeleteData(
                table: "MedalDefinitions",
                keyColumn: "Id",
                keyValue: 32);

            migrationBuilder.DeleteData(
                table: "MedalDefinitions",
                keyColumn: "Id",
                keyValue: 33);

            migrationBuilder.DeleteData(
                table: "MedalDefinitions",
                keyColumn: "Id",
                keyValue: 34);

            migrationBuilder.DeleteData(
                table: "MedalDefinitions",
                keyColumn: "Id",
                keyValue: 35);

            migrationBuilder.DeleteData(
                table: "MedalDefinitions",
                keyColumn: "Id",
                keyValue: 36);

            migrationBuilder.DeleteData(
                table: "MedalDefinitions",
                keyColumn: "Id",
                keyValue: 37);

            migrationBuilder.DeleteData(
                table: "MedalDefinitions",
                keyColumn: "Id",
                keyValue: 38);

            migrationBuilder.DeleteData(
                table: "MedalDefinitions",
                keyColumn: "Id",
                keyValue: 39);

            migrationBuilder.DeleteData(
                table: "MedalDefinitions",
                keyColumn: "Id",
                keyValue: 40);

            migrationBuilder.DeleteData(
                table: "MedalDefinitions",
                keyColumn: "Id",
                keyValue: 41);

            migrationBuilder.DeleteData(
                table: "MedalDefinitions",
                keyColumn: "Id",
                keyValue: 42);

            migrationBuilder.DeleteData(
                table: "MedalDefinitions",
                keyColumn: "Id",
                keyValue: 43);

            migrationBuilder.DeleteData(
                table: "MedalDefinitions",
                keyColumn: "Id",
                keyValue: 44);

            migrationBuilder.DeleteData(
                table: "MedalDefinitions",
                keyColumn: "Id",
                keyValue: 45);

            migrationBuilder.DeleteData(
                table: "MedalDefinitions",
                keyColumn: "Id",
                keyValue: 46);

            migrationBuilder.DeleteData(
                table: "MedalDefinitions",
                keyColumn: "Id",
                keyValue: 47);

            migrationBuilder.DeleteData(
                table: "MedalDefinitions",
                keyColumn: "Id",
                keyValue: 48);

            migrationBuilder.DeleteData(
                table: "MedalDefinitions",
                keyColumn: "Id",
                keyValue: 49);

            migrationBuilder.DeleteData(
                table: "MedalDefinitions",
                keyColumn: "Id",
                keyValue: 50);

            migrationBuilder.DeleteData(
                table: "MedalDefinitions",
                keyColumn: "Id",
                keyValue: 51);

            migrationBuilder.DeleteData(
                table: "MedalDefinitions",
                keyColumn: "Id",
                keyValue: 52);

            migrationBuilder.UpdateData(
                table: "MedalDefinitions",
                keyColumn: "Id",
                keyValue: 1,
                column: "Description",
                value: "Completaste una carrera oficial.");

            migrationBuilder.UpdateData(
                table: "MedalDefinitions",
                keyColumn: "Id",
                keyValue: 2,
                column: "Description",
                value: "Platinaste un videojuego al 100%.");

            migrationBuilder.UpdateData(
                table: "MedalDefinitions",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "Description", "UnlockHint" },
                values: new object[] { "Terminaste un libro de principio a fin.", "Marca un libro como completado al leer todas sus páginas." });

            migrationBuilder.UpdateData(
                table: "MedalDefinitions",
                keyColumn: "Id",
                keyValue: 5,
                column: "Description",
                value: "Completaste todas las sesiones de un curso.");

            migrationBuilder.UpdateData(
                table: "MedalDefinitions",
                keyColumn: "Id",
                keyValue: 6,
                column: "Description",
                value: "Completaste un rompecabezas.");

            migrationBuilder.UpdateData(
                table: "MedalDefinitions",
                keyColumn: "Id",
                keyValue: 7,
                column: "Description",
                value: "Terminaste una serie o película.");
        }
    }
}
