namespace HobbyXP.Helpers;

internal static class TextSearchFilter
{
    public static bool Matches(string? haystack, string? needle)
    {
        if (string.IsNullOrWhiteSpace(needle))
            return true;

        return (haystack ?? string.Empty)
            .Contains(needle.Trim(), StringComparison.OrdinalIgnoreCase);
    }
}
