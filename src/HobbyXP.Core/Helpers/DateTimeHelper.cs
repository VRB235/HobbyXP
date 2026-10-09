namespace HobbyXP.Helpers;

public static class DateTimeHelper
{
    public static DateTime ToUtcFromLocalDate(DateTime localDate) =>
        DateTime.SpecifyKind(localDate.Date, DateTimeKind.Local).ToUniversalTime();

    public static DateTime? ToUtcFromLocalDate(DateTime? localDate) =>
        localDate.HasValue ? ToUtcFromLocalDate(localDate.Value) : null;

    /// <summary>
    /// Convierte un instante almacenado (UTC o Unspecified-as-UTC) a fecha local de calendario
    /// (<see cref="DateTimeKind.Unspecified"/>) para resaltar días en DatePicker.
    /// </summary>
    public static DateTime ToLocalCalendarDate(DateTime value)
    {
        var local = value.Kind switch
        {
            DateTimeKind.Utc => value.ToLocalTime(),
            DateTimeKind.Unspecified => DateTime.SpecifyKind(value, DateTimeKind.Utc).ToLocalTime(),
            _ => value
        };
        return DateTime.SpecifyKind(local.Date, DateTimeKind.Unspecified);
    }

    public static IReadOnlySet<DateTime> ToLocalCalendarDateSet(IEnumerable<DateTime> values) =>
        values.Select(ToLocalCalendarDate).ToHashSet();

    public static IReadOnlySet<DateTime> ToLocalCalendarDateSet(IEnumerable<DateTime?> values) =>
        values.Where(v => v.HasValue).Select(v => ToLocalCalendarDate(v!.Value)).ToHashSet();
}
