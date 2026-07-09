namespace HobbyXP.Helpers;

internal static class DateRangeFilter
{
    public static bool Matches(DateTime valueUtc, DateTime? from, DateTime? to)
    {
        var localDate = valueUtc.Kind == DateTimeKind.Utc
            ? valueUtc.ToLocalTime().Date
            : valueUtc.Date;

        if (from.HasValue && localDate < from.Value.Date)
            return false;

        if (to.HasValue && localDate > to.Value.Date)
            return false;

        return true;
    }
}
