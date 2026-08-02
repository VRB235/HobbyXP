using HobbyXP.Models.Enums;

namespace HobbyXP.Helpers;

/// <summary>
/// Títulos alegóricos por nivel dentro de cada hobby (RPG ligero).
/// </summary>
public static class HobbyLevelTitles
{
    private static readonly IReadOnlyDictionary<MilestoneSourceType, string[]> TitlesByHobby =
        new Dictionary<MilestoneSourceType, string[]>
        {
            [MilestoneSourceType.Running] =
            [
                "Aprendiz del asfalto",
                "Trotador de aliento",
                "Peregrino del kilómetro",
                "Cazador de ritmos",
                "Herrero de zancadas",
                "Centinela del pace",
                "Brujo del negative split",
                "Arquitecto de rutas",
                "Señor de la media",
                "Titán del asfalto eterno",
                "Oráculo del VO2",
                "Leyenda del horizonte"
            ],
            [MilestoneSourceType.Gym] =
            [
                "Novato del rack",
                "Aprendiz de las placas",
                "Forjador de repeticiones",
                "Guerrero de las series",
                "Alquimista del PR",
                "Titán del overload",
                "Dominador del banco",
                "Arquitecto muscular",
                "Señor del volumen",
                "Coloso del hierro",
                "Oráculo de la hipertrofia",
                "Leyenda del Olimpo gym"
            ],
            [MilestoneSourceType.OfficialRace] =
            [
                "Debutante del dorsal",
                "Aspirante al chip",
                "Cazador de medallas",
                "Estratega del pacing",
                "Gladiador del km oficial",
                "Heredero del podio",
                "Señor de la meta",
                "Conquistador de dorsales",
                "Titán del finish line",
                "Campeón alegórico",
                "Oráculo de la carrera",
                "Leyenda del circuito"
            ],
            [MilestoneSourceType.Puzzle] =
            [
                "Pieza perdida",
                "Buscador de bordes",
                "Empalmador de cielos",
                "Detective del patrón",
                "Arquitecto del mosaico",
                "Maestro del encaje",
                "Señor de las mil piezas",
                "Oráculo del cartón",
                "Titán de la mesa",
                "Guardián del último hueco",
                "Sabio del puzzle table",
                "Leyenda del click final"
            ],
            [MilestoneSourceType.Media] =
            [
                "Espectador novato",
                "Maratonista de sillón",
                "Rastreador de tramas",
                "Crítico de madrugada",
                "Archivista de finales",
                "Señor del binge",
                "Oráculo del cliffhanger",
                "Titán de la filmoteca",
                "Guardián del canon",
                "Cronista de temporadas",
                "Sabio del spoiler-free",
                "Leyenda de la pantalla"
            ],
            [MilestoneSourceType.VideoGame] =
            [
                "Noob con honor",
                "Grinder en ciernes",
                "Cazatesoros",
                "Cazador de platinos",
                "Tank de la narrativa",
                "Speedrunner espiritual",
                "Señor del achievement",
                "Oráculo del savefile",
                "Titán del 100%",
                "Guardián del New Game+",
                "Sabio del side quest",
                "Leyenda del platinum eterno"
            ],
            [MilestoneSourceType.Book] =
            [
                "Lector de umbral",
                "Viajero de páginas",
                "Devorador de capítulos",
                "Cartógrafo de sagas",
                "Bibliotecario errante",
                "Señor del subrayado",
                "Oráculo del epílogo",
                "Titán del estante",
                "Guardián de las solapas",
                "Cronista de márgenes",
                "Sabio del índice",
                "Leyenda del colofón"
            ],
            [MilestoneSourceType.Course] =
            [
                "Alumno del primer módulo",
                "Tomador de apuntes",
                "Forjador de hábitos",
                "Completador de lecciones",
                "Arquitecto del syllabus",
                "Señor del certificado",
                "Oráculo de la rúbrica",
                "Titán del aprendizaje",
                "Mentor en potencia",
                "Guardián del portafolio",
                "Sabio del campus",
                "Leyenda del saber infinito"
            ]
        };

    public static string GetTitle(MilestoneSourceType sourceType, int level)
    {
        var safeLevel = Math.Max(1, level);
        if (!TitlesByHobby.TryGetValue(sourceType, out var titles) || titles.Length == 0)
            return $"Rango {safeLevel}";

        if (safeLevel <= titles.Length)
            return titles[safeLevel - 1];

        var baseTitle = titles[^1];
        var extra = safeLevel - titles.Length;
        return $"{baseTitle} · Ascenso {ToRoman(extra)}";
    }

    public static string FormatLevelLabel(MilestoneSourceType sourceType, int level) =>
        $"Nv. {Math.Max(1, level)} · {GetTitle(sourceType, level)}";

    private static string ToRoman(int number)
    {
        if (number <= 0)
            return "I";

        (int Value, string Symbol)[] map =
        [
            (100, "C"), (90, "XC"), (50, "L"), (40, "XL"),
            (10, "X"), (9, "IX"), (5, "V"), (4, "IV"), (1, "I")
        ];

        var remaining = Math.Min(number, 399);
        var result = string.Empty;
        foreach (var (value, symbol) in map)
        {
            while (remaining >= value)
            {
                result += symbol;
                remaining -= value;
            }
        }

        return result;
    }
}
