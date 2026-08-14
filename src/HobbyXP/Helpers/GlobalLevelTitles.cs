namespace HobbyXP.Helpers;

/// <summary>
/// Títulos alegóricos del nivel global (aventurero polímata / maestro de hobbies).
/// </summary>
public static class GlobalLevelTitles
{
    private static readonly string[] Titles =
    [
        "Novato de la bitácora",
        "Aprendiz de pasiones",
        "Cronista de hábitos",
        "Explorador de oficios",
        "Cartógrafo de hobbies",
        "Alquimista del progreso",
        "Señor de las disciplinas",
        "Arquitecto de la constancia",
        "Oráculo del equilibrio",
        "Titán de la bitácora viva",
        "Guardián del multihobby",
        "Leyenda del HobbyXP"
    ];

    public static string GetTitle(int level)
    {
        var safeLevel = Math.Max(1, level);
        if (safeLevel <= Titles.Length)
            return Titles[safeLevel - 1];

        var extra = safeLevel - Titles.Length;
        return $"{Titles[^1]} · Ascenso {ToRoman(extra)}";
    }

    public static string FormatLevelLabel(int level) =>
        $"Nv. {Math.Max(1, level)} · {GetTitle(level)}";

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
