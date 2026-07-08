using System.IO;

namespace HobbyXP.Data;

public static class DatabaseConstants
{
    public const string FileName = "hobbyxp.db";

    public static string GetDatabaseDirectory()
    {
        var directory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "HobbyXP");

        Directory.CreateDirectory(directory);
        return directory;
    }

    public static string GetDatabasePath() =>
        Path.Combine(GetDatabaseDirectory(), FileName);

    public static string GetConnectionString() =>
        $"Data Source={GetDatabasePath()}";
}
