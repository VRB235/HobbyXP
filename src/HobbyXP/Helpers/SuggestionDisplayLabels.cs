using HobbyXP.Models.Enums;

namespace HobbyXP.Helpers;

public static class SuggestionDisplayLabels
{
    public static string GetKind(SuggestionKind kind) => kind switch
    {
        SuggestionKind.Improvement => "Mejora",
        SuggestionKind.Bug => "Error",
        _ => kind.ToString()
    };

    public static string GetStatus(SuggestionStatus status) => status switch
    {
        SuggestionStatus.Open => "Pendiente",
        SuggestionStatus.Resolved => "Resuelta",
        _ => status.ToString()
    };
}
