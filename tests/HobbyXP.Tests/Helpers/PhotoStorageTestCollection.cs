namespace HobbyXP.Tests.Helpers;

/// <summary>
/// Serializa tests que mutan HOBBYXP_DATA_DIR (no son seguros en paralelo).
/// </summary>
[CollectionDefinition(Name)]
public sealed class PhotoStorageTestCollection : ICollectionFixture<object>
{
    public const string Name = "PhotoStorageEnv";
}
