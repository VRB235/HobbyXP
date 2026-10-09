using HobbyXP.Models.Enums;

namespace HobbyXP.Helpers;

/// <summary>
/// Nombres sugeridos para medallas creadas por el usuario (editables).
/// </summary>
public static class MedalNameSuggestions
{
    public static IReadOnlyList<string> Suggest(MilestoneSourceType hobby, int xpThreshold)
    {
        var hobbyLabel = HobbyProgressCatalog.GetDisplayName(hobby);
        var tier = ResolveTierLabel(xpThreshold);
        var themed = GetHobbyThemes(hobby);

        var suggestions = new List<string>(16)
        {
            $"{tier} de {hobbyLabel}",
            $"Insignia {tier}",
            $"Trofeo {hobbyLabel}",
            $"{themed[0]} · {tier}",
            $"{themed[1]}",
            $"{themed[2]}",
            $"Hito {xpThreshold:N0} XP",
            $"Umbral {tier}",
            $"Leyenda {hobbyLabel}",
            $"{themed[0]} Dorado"
        };

        return suggestions
            .Distinct(StringComparer.CurrentCultureIgnoreCase)
            .Take(12)
            .ToList();
    }

    private static string ResolveTierLabel(int xpThreshold) => xpThreshold switch
    {
        < 100 => "Novato",
        < 250 => "Iniciado",
        < 500 => "Aprendiz",
        < 1000 => "Competente",
        < 2500 => "Veterano",
        < 5000 => "Élite",
        < 10000 => "Maestro",
        _ => "Leyenda"
    };

    private static string[] GetHobbyThemes(MilestoneSourceType hobby) => hobby switch
    {
        MilestoneSourceType.Running => ["Ritmo Constante", "Alma de Asfalto", "Zancada Firme"],
        MilestoneSourceType.Gym => ["Forja de Hierro", "Fuerza Templada", "Coloso en Marcha"],
        MilestoneSourceType.OfficialRace => ["Chip de Honor", "Meta Conquistada", "Podio Interior"],
        MilestoneSourceType.Puzzle => ["Pieza Maestra", "Mente Clara", "Tablero Resuelto"],
        MilestoneSourceType.Media => ["Maratón Cultural", "Pantalla Completa", "Crítico Persistente"],
        MilestoneSourceType.VideoGame => ["Platino Interior", "Save Legendario", "Modo Historia"],
        MilestoneSourceType.Book => ["Lector Voraz", "Página a Página", "Biblioteca Viva"],
        MilestoneSourceType.Course => ["Graduado Activo", "Sesión a Sesión", "Aula Constante"],
        MilestoneSourceType.Diet => ["Plato en Plan", "Disciplina Diaria", "Equilibrio Firme"],
        _ => ["Logro Personal", "Hito Propio", "Insignia Custom"]
    };
}
