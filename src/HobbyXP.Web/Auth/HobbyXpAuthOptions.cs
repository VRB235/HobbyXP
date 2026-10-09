namespace HobbyXP.Web.Auth;

public sealed class HobbyXpAuthOptions
{
    public const string SectionName = "HobbyXp:Auth";

    public string Username { get; set; } = "admin";

    public string Password { get; set; } = "changeme";
}
