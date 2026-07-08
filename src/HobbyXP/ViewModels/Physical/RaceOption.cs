namespace HobbyXP.ViewModels.Physical;

public sealed class RaceOption
{
    public int? Id { get; init; }

    public string Name { get; init; } = string.Empty;

    public static RaceOption None { get; } = new() { Id = null, Name = "(Sin carrera oficial)" };
}
