namespace HobbyXP.Helpers;

public static class DateTimeHelper
{
    public static DateTime ToUtcFromLocalDate(DateTime localDate) =>
        DateTime.SpecifyKind(localDate.Date, DateTimeKind.Local).ToUniversalTime();

    public static DateTime? ToUtcFromLocalDate(DateTime? localDate) =>
        localDate.HasValue ? ToUtcFromLocalDate(localDate.Value) : null;
}
