using System.Globalization;
using System.Text;

namespace HobbyXP.Helpers;

public static class TextSearchFilter
{
    public static bool Matches(string? haystack, string? needle)
    {
        if (string.IsNullOrWhiteSpace(needle))
            return true;

        var trimmed = needle.Trim();
        var source = haystack ?? string.Empty;
        if (source.Contains(trimmed, StringComparison.OrdinalIgnoreCase))
            return true;

        return StripDiacritics(source).Contains(StripDiacritics(trimmed), StringComparison.OrdinalIgnoreCase);
    }

    public static bool MatchesAny(string? needle, params string?[] haystacks) =>
        haystacks.Any(h => Matches(h, needle));

    private static string StripDiacritics(string text)
    {
        var formD = text.Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(formD.Length);
        foreach (var ch in formD)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(ch) != UnicodeCategory.NonSpacingMark)
                builder.Append(ch);
        }

        return builder.ToString().Normalize(NormalizationForm.FormC);
    }
}
