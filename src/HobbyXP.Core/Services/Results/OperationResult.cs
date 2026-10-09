namespace HobbyXP.Services.Results;

/// <summary>
/// Resultado estándar de operaciones de servicio con eventos de logro asociados.
/// </summary>
public sealed record OperationResult<T>(
    T Value,
    IReadOnlyList<AchievementEvent> Events)
{
    public static OperationResult<T> Empty(T value) =>
        new(value, Array.Empty<AchievementEvent>());

    public static OperationResult<T> WithEvents(T value, params AchievementEvent[] events) =>
        new(value, events);
}
