using HobbyXP.Models.Achievements;
using HobbyXP.Models.Enums;
using HobbyXP.Services.Results;

namespace HobbyXP.Services;

internal static class LevelUpSuggestionBuilder
{
    public static IReadOnlyList<LevelUpSuggestion> Build(
        int xpRemaining,
        IReadOnlyList<HobbyDistributionSlice> distribution,
        IReadOnlyList<AchievementRule> rules)
    {
        if (xpRemaining <= 0)
            return Array.Empty<LevelUpSuggestion>();

        var activeRules = rules.Where(r => r.IsActive).ToList();
        var categories = GetTargetCategories(distribution);
        var suggestions = new List<LevelUpSuggestion>();

        foreach (var category in categories)
        {
            var suggestion = BuildForCategory(category, xpRemaining, activeRules);
            if (suggestion is not null)
                suggestions.Add(suggestion);
        }

        if (suggestions.Count == 0)
        {
            foreach (var category in new[]
                     {
                         MilestoneSourceType.Running,
                         MilestoneSourceType.Gym,
                         MilestoneSourceType.Book
                     })
            {
                var suggestion = BuildForCategory(category, xpRemaining, activeRules);
                if (suggestion is not null)
                    suggestions.Add(suggestion);
            }
        }

        return suggestions.Take(4).ToList();
    }

    private static IEnumerable<MilestoneSourceType> GetTargetCategories(IReadOnlyList<HobbyDistributionSlice> distribution)
    {
        if (distribution.Count == 0)
        {
            return
            [
                MilestoneSourceType.Running,
                MilestoneSourceType.Gym,
                MilestoneSourceType.Book,
                MilestoneSourceType.Media
            ];
        }

        return distribution
            .OrderBy(slice => slice.Percentage)
            .Select(slice => slice.Category)
            .Distinct()
            .Take(4);
    }

    private static LevelUpSuggestion? BuildForCategory(
        MilestoneSourceType category,
        int xpRemaining,
        IReadOnlyList<AchievementRule> rules) =>
        category switch
        {
            MilestoneSourceType.Running => BuildFromRule(
                rules,
                AchievementActionType.RunningKilometer,
                "Running",
                xpRemaining,
                "Registre una sesión con la distancia indicada."),
            MilestoneSourceType.Gym => BuildFromRule(
                rules,
                AchievementActionType.GymWorkoutSaved,
                "Gimnasio",
                xpRemaining,
                "Guarde un entrenamiento completo por cada sesión."),
            MilestoneSourceType.Puzzle => BuildFromRule(
                rules,
                AchievementActionType.PuzzleCompleted,
                "Rompecabezas",
                xpRemaining,
                "Marque un rompecabezas como completado."),
            MilestoneSourceType.Media => BuildMediaSuggestion(xpRemaining, rules),
            MilestoneSourceType.VideoGame => BuildVideoGameSuggestion(xpRemaining, rules),
            MilestoneSourceType.Book => BuildBookSuggestion(xpRemaining, rules),
            MilestoneSourceType.Course => BuildCourseSuggestion(xpRemaining, rules),
            MilestoneSourceType.OfficialRace => BuildFromRule(
                rules,
                AchievementActionType.OfficialRaceCompleted,
                "Carrera oficial",
                xpRemaining,
                "Marque la carrera como completada."),
            _ => null
        };

    private static LevelUpSuggestion? BuildVideoGameSuggestion(int xpRemaining, IReadOnlyList<AchievementRule> rules)
    {
        var percentRule = rules.FirstOrDefault(r => r.ActionType == AchievementActionType.VideoGamePercent);
        var platinumRule = rules.FirstOrDefault(r => r.ActionType == AchievementActionType.VideoGamePlatinum);

        if (percentRule is { PointsPerUnit: > 0 })
        {
            return BuildFromRule(rules, AchievementActionType.VideoGamePercent, "Videojuegos", xpRemaining,
                "Actualice el porcentaje de avance del juego.");
        }

        if (platinumRule?.FlatBonusPoints is > 0)
        {
            var gamesNeeded = (int)Math.Ceiling(xpRemaining / (double)platinumRule.FlatBonusPoints.Value);
            var estimatedXp = gamesNeeded * platinumRule.FlatBonusPoints.Value;

            return new LevelUpSuggestion(
                "Videojuegos",
                FormatUnits(gamesNeeded, platinumRule.UnitLabel),
                estimatedXp,
                "Platine juegos al 100% para obtener el bono completo.");
        }

        return null;
    }

    private static LevelUpSuggestion? BuildBookSuggestion(int xpRemaining, IReadOnlyList<AchievementRule> rules)
    {
        var pageRule = rules.FirstOrDefault(r => r.ActionType == AchievementActionType.BookPageRead);
        var bookRule = rules.FirstOrDefault(r => r.ActionType == AchievementActionType.BookCompleted);

        if (pageRule is null && bookRule is null)
            return null;

        if (pageRule is { PointsPerUnit: > 0 } && bookRule?.FlatBonusPoints is > 0)
        {
            var pagesNeeded = (int)Math.Ceiling(xpRemaining / (double)pageRule.PointsPerUnit);
            var pagesXp = (int)Math.Round(pageRule.PointsPerUnit * pagesNeeded, MidpointRounding.AwayFromZero);
            var booksNeeded = (int)Math.Ceiling(xpRemaining / (double)bookRule.FlatBonusPoints.Value);
            var booksXp = booksNeeded * bookRule.FlatBonusPoints.Value;

            if (booksXp < pagesXp || (booksXp == pagesXp && booksNeeded < pagesNeeded))
            {
                return new LevelUpSuggestion(
                    "Libros",
                    FormatUnits(booksNeeded, bookRule.UnitLabel),
                    booksXp,
                    "Marque libros como terminados para alcanzar el XP faltante.");
            }

            return new LevelUpSuggestion(
                "Libros",
                FormatUnits(pagesNeeded, pageRule.UnitLabel),
                pagesXp,
                "Registre las páginas leídas en su libro activo.");
        }

        if (pageRule is not null && pageRule.PointsPerUnit > 0)
        {
            return BuildFromRule(rules, AchievementActionType.BookPageRead, "Libros", xpRemaining,
                "Registre las páginas leídas en su libro activo.");
        }

        return bookRule?.FlatBonusPoints is > 0
            ? new LevelUpSuggestion(
                "Libros",
                FormatUnits((int)Math.Ceiling(xpRemaining / (double)bookRule.FlatBonusPoints.Value), bookRule.UnitLabel),
                (int)Math.Ceiling(xpRemaining / (double)bookRule.FlatBonusPoints.Value) * bookRule.FlatBonusPoints.Value,
                "Marque un libro como terminado.")
            : null;
    }

    private static LevelUpSuggestion? BuildMediaSuggestion(int xpRemaining, IReadOnlyList<AchievementRule> rules)
    {
        var chapterRule = rules.FirstOrDefault(r => r.ActionType == AchievementActionType.MediaChapterWatched);
        if (chapterRule is { PointsPerUnit: > 0 })
        {
            return BuildFromRule(rules, AchievementActionType.MediaChapterWatched, "Series/Películas", xpRemaining,
                "Registre capítulos vistos en una serie activa.");
        }

        return BuildFromRule(
            rules,
            AchievementActionType.MediaCompleted,
            "Series/Películas",
            xpRemaining,
            "Finalice una serie o película por cada unidad.");
    }

    private static LevelUpSuggestion? BuildCourseSuggestion(int xpRemaining, IReadOnlyList<AchievementRule> rules)
    {
        var sessionRule = rules.FirstOrDefault(r => r.ActionType == AchievementActionType.CourseSessionCompleted);
        var courseRule = rules.FirstOrDefault(r => r.ActionType == AchievementActionType.CourseCompleted);

        if (sessionRule is { PointsPerUnit: > 0 })
        {
            return BuildFromRule(rules, AchievementActionType.CourseSessionCompleted, "Cursos", xpRemaining,
                "Registre sesiones en un curso activo.");
        }

        return courseRule?.FlatBonusPoints is > 0
            ? new LevelUpSuggestion(
                "Cursos",
                FormatUnits((int)Math.Ceiling(xpRemaining / (double)courseRule.FlatBonusPoints.Value), courseRule.UnitLabel),
                (int)Math.Ceiling(xpRemaining / (double)courseRule.FlatBonusPoints.Value) * courseRule.FlatBonusPoints.Value,
                "Termine un curso para obtener el bono.")
            : null;
    }

    private static LevelUpSuggestion? BuildFromRule(
        IReadOnlyList<AchievementRule> rules,
        AchievementActionType actionType,
        string categoryLabel,
        int xpRemaining,
        string description)
    {
        var rule = rules.FirstOrDefault(r => r.ActionType == actionType);
        if (rule is null)
            return null;

        if (rule.PointsPerUnit > 0)
        {
            var minUnits = (int)Math.Ceiling(xpRemaining / (double)rule.PointsPerUnit);
            var estimatedXp = (int)Math.Round(rule.PointsPerUnit * minUnits, MidpointRounding.AwayFromZero);
            var requirement = FormatUnits(minUnits, rule.UnitLabel);

            return new LevelUpSuggestion(
                categoryLabel,
                requirement,
                estimatedXp,
                description);
        }

        if (rule.FlatBonusPoints is > 0)
        {
            return new LevelUpSuggestion(
                categoryLabel,
                FormatUnits(1, rule.UnitLabel),
                rule.FlatBonusPoints.Value,
                description);
        }

        return null;
    }

    private static string FormatUnits(int count, string unitLabel)
    {
        if (count == 1)
            return $"1 {unitLabel}";

        var plural = unitLabel switch
        {
            "km" => "km",
            "página" => "páginas",
            "sesión" => "sesiones",
            "capítulo" => "capítulos",
            "rompecabezas" => "rompecabezas",
            "obra" => "obras",
            "%" => "%",
            "libro" => "libros",
            "curso" => "cursos",
            "carrera" => "carreras",
            "juego" => "juegos",
            "logro" => "logros",
            _ => $"{unitLabel}s"
        };

        return $"{count} {plural}";
    }
}
