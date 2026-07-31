using System.IO;

namespace HobbyXP.Data;

public static class DatabaseConstants
{
    public const string FileName = "hobbyxp.db";

    /// <summary>
    /// Carpeta de datos bajo LocalAppData.
    /// Debug → HobbyXP-Dev; Release (producción) → HobbyXP.
    /// Override opcional: variable de entorno HOBBYXP_DATA_DIR
    /// (nombre relativo bajo LocalAppData, o ruta absoluta).
    /// </summary>
    public static string GetDatabaseDirectory()
    {
        var directory = ResolveDataDirectory();
        Directory.CreateDirectory(directory);
        return directory;
    }

    public static string GetDatabasePath() =>
        Path.Combine(GetDatabaseDirectory(), FileName);

    public static string GetConnectionString() =>
        $"Data Source={GetDatabasePath()}";

    private static string ResolveDataDirectory()
    {
        var overridePath = Environment.GetEnvironmentVariable("HOBBYXP_DATA_DIR");
        if (!string.IsNullOrWhiteSpace(overridePath))
        {
            return Path.IsPathRooted(overridePath)
                ? overridePath
                : Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    overridePath);
        }

#if DEBUG
        const string appFolder = "HobbyXP-Dev";
#else
        const string appFolder = "HobbyXP";
#endif

        return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            appFolder);
    }
}
