using HobbyXP.Models.Achievements;
using HobbyXP.Models.Enums;
using HobbyXP.Helpers;
using Microsoft.EntityFrameworkCore;

namespace HobbyXP.Data;

internal sealed record MedalCatalogEntry(
    int Id,
    MedalCode Code,
    MedalMilestoneTrack Track,
    int Threshold,
    string Name,
    string Description,
    string UnlockHint,
    string IconPath);

/// <summary>
/// Catálogo único de medallas: primer logro (Ids 1–7) + hitos acumulativos.
/// </summary>
internal static class MedalCatalog
{
    private static readonly DateTime SeedTimestamp = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    public static IReadOnlyList<MedalCatalogEntry> Entries { get; } = BuildEntries();

    public static IEnumerable<MedalCatalogEntry> ForTrack(MedalMilestoneTrack track) =>
        Entries.Where(e => e.Track == track);

    public static void Seed(ModelBuilder modelBuilder)
    {
        foreach (var entry in Entries)
        {
            modelBuilder.Entity<MedalDefinition>().HasData(new MedalDefinition
            {
                Id = entry.Id,
                Code = entry.Code,
                Name = entry.Name,
                Description = entry.Description,
                UnlockHint = entry.UnlockHint,
                IconPath = entry.IconPath,
                CreatedAt = SeedTimestamp
            });
        }
    }

    private static IReadOnlyList<MedalCatalogEntry> BuildEntries() =>
    [
        // Primer logro por módulo (Ids estables 1–7)
        E(1, MedalCode.GoldRace, MedalMilestoneTrack.OfficialRacesCompleted, 1,
            "Medalla de Oro", "Completaste tu primera carrera oficial.",
            "Marca una carrera oficial como completada."),
        E(2, MedalCode.PlatinumGame, MedalMilestoneTrack.VideoGamesPlatinum, 1,
            "Medalla de Platino", "Platinaste tu primer videojuego al 100%.",
            "Lleva un videojuego al 100% de completitud."),
        E(3, MedalCode.ProgressiveOverload, MedalMilestoneTrack.ProgressiveOverloadPrs, 1,
            "Sobrecarga Progresiva", "Superaste tu récord histórico en gimnasio.",
            "Mejora peso o tiempo respecto a tu máximo anterior."),
        E(4, MedalCode.BookCompleted, MedalMilestoneTrack.BooksCompleted, 1,
            "Lector Voraz", "Terminaste tu primer libro de principio a fin.",
            "Marca un libro como completado."),
        E(5, MedalCode.CourseCompleted, MedalMilestoneTrack.CoursesCompleted, 1,
            "Graduado", "Completaste tu primer curso.",
            "Finaliza un curso registrando todas sus sesiones."),
        E(6, MedalCode.PuzzleMaster, MedalMilestoneTrack.PuzzlesCompleted, 1,
            "Maestro del Puzzle", "Completaste tu primer rompecabezas.",
            "Registra un rompecabezas como terminado."),
        E(7, MedalCode.MediaMarathon, MedalMilestoneTrack.MediaCompleted, 1,
            "Maratón Cultural", "Terminaste tu primera serie o película.",
            "Registra una obra de entretenimiento como completada."),

        // Running / carreras
        E(8, MedalCode.RacesCompleted3, MedalMilestoneTrack.OfficialRacesCompleted, 3,
            "Podio en Entrenamiento", "Tres carreras oficiales en tu historial.",
            "Completa 3 carreras oficiales."),
        E(9, MedalCode.RacesCompleted5, MedalMilestoneTrack.OfficialRacesCompleted, 5,
            "Corredor Constante", "Cinco carreras oficiales conquistadas.",
            "Completa 5 carreras oficiales."),
        E(10, MedalCode.RacesCompleted10, MedalMilestoneTrack.OfficialRacesCompleted, 10,
            "Veterano del Asfalto", "Diez carreras oficiales en tu palmarés.",
            "Completa 10 carreras oficiales."),
        E(11, MedalCode.RacesCompleted25, MedalMilestoneTrack.OfficialRacesCompleted, 25,
            "Leyenda del Chip", "Veinticinco carreras oficiales. Eres imparable.",
            "Completa 25 carreras oficiales."),
        E(12, MedalCode.RunningSessions10, MedalMilestoneTrack.RunningSessions, 10,
            "Ritmo de Reloj", "Diez sesiones de running registradas.",
            "Registra 10 sesiones de running."),
        E(13, MedalCode.RunningSessions50, MedalMilestoneTrack.RunningSessions, 50,
            "Motor Cardíaco", "Cincuenta salidas al asfalto.",
            "Registra 50 sesiones de running."),
        E(14, MedalCode.RunningSessions100, MedalMilestoneTrack.RunningSessions, 100,
            "Máquina de Correr", "Cien sesiones. El GPS te conoce por nombre.",
            "Registra 100 sesiones de running."),
        E(15, MedalCode.RunningKm100, MedalMilestoneTrack.RunningKilometers, 100,
            "Centurión del Kilómetro", "Acumulaste 100 km corriendo.",
            "Corre un total de 100 km."),
        E(16, MedalCode.RunningKm500, MedalMilestoneTrack.RunningKilometers, 500,
            "Conquistador del Asfalto", "500 km en tus piernas.",
            "Corre un total de 500 km."),
        E(17, MedalCode.RunningKm1000, MedalMilestoneTrack.RunningKilometers, 1000,
            "Ultra Alma", "1.000 km. Distancia de leyenda.",
            "Corre un total de 1.000 km."),

        // Gimnasio
        E(18, MedalCode.ProgressiveOverload5, MedalMilestoneTrack.ProgressiveOverloadPrs, 5,
            "Forja de Hierro", "Cinco récords personales en el gym.",
            "Logra 5 sobrecargas progresivas."),
        E(19, MedalCode.ProgressiveOverload10, MedalMilestoneTrack.ProgressiveOverloadPrs, 10,
            "Titán en Evolución", "Diez PRs. Cada sesión te hace más fuerte.",
            "Logra 10 sobrecargas progresivas."),
        E(20, MedalCode.ProgressiveOverload25, MedalMilestoneTrack.ProgressiveOverloadPrs, 25,
            "Coloso del Hierro", "Veinticinco récords rotos. El gym tiembla.",
            "Logra 25 sobrecargas progresivas."),
        E(21, MedalCode.GymWorkouts10, MedalMilestoneTrack.GymWorkouts, 10,
            "Hierro Temprano", "Diez entrenamientos guardados.",
            "Registra 10 sesiones de gimnasio."),
        E(22, MedalCode.GymWorkouts50, MedalMilestoneTrack.GymWorkouts, 50,
            "Forja Personal", "Cincuenta sesiones de gimnasio.",
            "Registra 50 sesiones de gimnasio."),
        E(23, MedalCode.GymWorkouts100, MedalMilestoneTrack.GymWorkouts, 100,
            "Titán del Gym", "Cien entrenamientos. Disciplina pura.",
            "Registra 100 sesiones de gimnasio."),
        E(24, MedalCode.GymWorkouts250, MedalMilestoneTrack.GymWorkouts, 250,
            "Monolito Humano", "Doscientos cincuenta sesiones. Eres una institución.",
            "Registra 250 sesiones de gimnasio."),

        // Videojuegos
        E(25, MedalCode.PlatinumGames3, MedalMilestoneTrack.VideoGamesPlatinum, 3,
            "Coleccionista Platino", "Tres juegos al 100%.",
            "Platina 3 videojuegos."),
        E(26, MedalCode.PlatinumGames5, MedalMilestoneTrack.VideoGamesPlatinum, 5,
            "Salón Digital", "Cinco platinos en la estantería virtual.",
            "Platina 5 videojuegos."),
        E(27, MedalCode.PlatinumGames10, MedalMilestoneTrack.VideoGamesPlatinum, 10,
            "Meta Absoluta", "Diez juegos al 100%. Completionista nato.",
            "Platina 10 videojuegos."),

        // Libros
        E(28, MedalCode.BooksCompleted5, MedalMilestoneTrack.BooksCompleted, 5,
            "Club del Capítulo Cinco", "Cinco libros terminados.",
            "Completa 5 libros."),
        E(29, MedalCode.BooksCompleted10, MedalMilestoneTrack.BooksCompleted, 10,
            "Bibliófilo de Garra", "Diez libros en tu historial.",
            "Completa 10 libros."),
        E(30, MedalCode.BooksCompleted25, MedalMilestoneTrack.BooksCompleted, 25,
            "Estantería Legendaria", "Veinticinco libros conquistados.",
            "Completa 25 libros."),
        E(31, MedalCode.BooksCompleted50, MedalMilestoneTrack.BooksCompleted, 50,
            "Faros del Conocimiento", "Cincuenta libros. Tu mente es un faro.",
            "Completa 50 libros."),
        E(32, MedalCode.BooksCompleted100, MedalMilestoneTrack.BooksCompleted, 100,
            "Archivo del Sabio", "Cien libros. Biblioteca personal de élite.",
            "Completa 100 libros."),
        E(33, MedalCode.BookPages1000, MedalMilestoneTrack.BookPagesRead, 1000,
            "Mil Páginas de Gloria", "Leíste 1.000 páginas en total.",
            "Acumula 1.000 páginas leídas."),
        E(34, MedalCode.BookPages5000, MedalMilestoneTrack.BookPagesRead, 5000,
            "Crónica Infinita", "5.000 páginas devoradas.",
            "Acumula 5.000 páginas leídas."),
        E(35, MedalCode.BookPages10000, MedalMilestoneTrack.BookPagesRead, 10000,
            "Biblioteca Ambulante", "10.000 páginas. Llevas una biblioteca encima.",
            "Acumula 10.000 páginas leídas."),

        // Cursos
        E(36, MedalCode.CoursesCompleted3, MedalMilestoneTrack.CoursesCompleted, 3,
            "Trilogía Académica", "Tres cursos terminados.",
            "Completa 3 cursos."),
        E(37, MedalCode.CoursesCompleted5, MedalMilestoneTrack.CoursesCompleted, 5,
            "Aprobado con Honores", "Cinco cursos en tu currículo.",
            "Completa 5 cursos."),
        E(38, MedalCode.CoursesCompleted10, MedalMilestoneTrack.CoursesCompleted, 10,
            "Multidisciplinar", "Diez cursos. Aprendiz perpetuo.",
            "Completa 10 cursos."),
        E(39, MedalCode.CoursesCompleted25, MedalMilestoneTrack.CoursesCompleted, 25,
            "Erudito Empírico", "Veinticinco cursos. Academia propia.",
            "Completa 25 cursos."),
        E(40, MedalCode.CourseSessions25, MedalMilestoneTrack.CourseSessions, 25,
            "Asistencia Ejemplar", "Veinticinco sesiones de curso registradas.",
            "Registra 25 sesiones de curso."),
        E(41, MedalCode.CourseSessions50, MedalMilestoneTrack.CourseSessions, 50,
            "Beca de Disciplina", "Cincuenta sesiones de estudio.",
            "Registra 50 sesiones de curso."),
        E(42, MedalCode.CourseSessions100, MedalMilestoneTrack.CourseSessions, 100,
            "Doctor Honoris Causa", "Cien sesiones. El conocimiento te persigue.",
            "Registra 100 sesiones de curso."),

        // Rompecabezas
        E(43, MedalCode.PuzzlesCompleted5, MedalMilestoneTrack.PuzzlesCompleted, 5,
            "Pieza a Pieza", "Cinco rompecabezas resueltos.",
            "Completa 5 rompecabezas."),
        E(44, MedalCode.PuzzlesCompleted10, MedalMilestoneTrack.PuzzlesCompleted, 10,
            "Arquitecto del Caos", "Diez puzzles. Orden desde el caos.",
            "Completa 10 rompecabezas."),
        E(45, MedalCode.PuzzlesCompleted25, MedalMilestoneTrack.PuzzlesCompleted, 25,
            "Visión Escher", "Veinticinco rompecabezas dominados.",
            "Completa 25 rompecabezas."),
        E(46, MedalCode.PuzzlesCompleted50, MedalMilestoneTrack.PuzzlesCompleted, 50,
            "Maestro del Encaje", "Cincuenta puzzles. Ninguna pieza te resiste.",
            "Completa 50 rompecabezas."),
        E(47, MedalCode.PuzzlesCompleted100, MedalMilestoneTrack.PuzzlesCompleted, 100,
            "Leyenda del Tablero", "Cien rompecabezas. Tu mesa es un templo.",
            "Completa 100 rompecabezas."),

        // Media
        E(48, MedalCode.MediaCompleted5, MedalMilestoneTrack.MediaCompleted, 5,
            "Noche de Palomitas", "Cinco obras terminadas.",
            "Completa 5 series o películas."),
        E(49, MedalCode.MediaCompleted10, MedalMilestoneTrack.MediaCompleted, 10,
            "Binge Profesional", "Diez maratones en el sofá.",
            "Completa 10 series o películas."),
        E(50, MedalCode.MediaCompleted25, MedalMilestoneTrack.MediaCompleted, 25,
            "Cinéfilo Serial", "Veinticinco obras en tu historial.",
            "Completa 25 series o películas."),
        E(51, MedalCode.MediaCompleted50, MedalMilestoneTrack.MediaCompleted, 50,
            "Sofá Olímpico", "Cincuenta obras. El control remoto es tuyo.",
            "Completa 50 series o películas."),
        E(52, MedalCode.MediaCompleted100, MedalMilestoneTrack.MediaCompleted, 100,
            "Palmarés del Streaming", "Cien obras. Algoritmo personalizado.",
            "Completa 100 series o películas."),
    ];

    private static MedalCatalogEntry E(
        int id,
        MedalCode code,
        MedalMilestoneTrack track,
        int threshold,
        string name,
        string description,
        string unlockHint) =>
        new(id, code, track, threshold, name, description, unlockHint, MedalIconPaths.ForTrack(track));
}
