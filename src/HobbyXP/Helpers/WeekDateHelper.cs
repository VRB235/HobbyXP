namespace HobbyXP.Helpers;

/// <summary>
/// Semana laboral ISO-like: lunes 00:00 – domingo 23:59:59 en hora local.
/// </summary>
public static class WeekDateHelper
{
    public static DateTime GetWeekStartLocal(DateTime localDate)
    {
        var date = localDate.Date;
        var offset = ((int)date.DayOfWeek - (int)DayOfWeek.Monday + 7) % 7;
        return date.AddDays(-offset);
    }

    public static DateTime GetWeekStartUtc(DateTime localDate) =>
        DateTimeHelper.ToUtcFromLocalDate(GetWeekStartLocal(localDate));

    public static DateTime GetWeekEndExclusiveUtc(DateTime weekStartUtc) =>
        weekStartUtc.AddDays(7);

    public static bool IsClosedWeek(DateTime weekStartLocal, DateTime todayLocal) =>
        GetWeekStartLocal(todayLocal) > weekStartLocal.Date;

    public static IEnumerable<DateTime> EnumerateClosedWeekStartsLocal(DateTime fromLocal, DateTime todayLocal)
    {
        var currentWeek = GetWeekStartLocal(todayLocal);
        var cursor = GetWeekStartLocal(fromLocal);
        while (cursor < currentWeek)
        {
            yield return cursor;
            cursor = cursor.AddDays(7);
        }
    }
}
