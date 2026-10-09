using System.IO;
using System.Text.Json;
using HobbyXP.Data;

namespace HobbyXP.Helpers;

/// <summary>
/// Serializa listas de rutas de fotos como JSON relativo al directorio de datos.
/// </summary>
public static class RelativePhotoPathStorage
{
    public static IReadOnlyList<string> Deserialize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return [];

        if (value.TrimStart().StartsWith('['))
        {
            try
            {
                var stored = JsonSerializer.Deserialize<List<string>>(value) ?? [];
                return stored.Select(ToAbsolutePath).Where(File.Exists).ToList();
            }
            catch
            {
                return [];
            }
        }

        return File.Exists(value) ? [value] : [];
    }

    public static string? Serialize(IReadOnlyList<string> absolutePaths)
    {
        if (absolutePaths.Count == 0)
            return null;

        var root = DatabaseConstants.GetDatabaseDirectory();
        var relative = absolutePaths
            .Select(path => Path.GetRelativePath(root, path))
            .ToList();

        return JsonSerializer.Serialize(relative);
    }

    private static string ToAbsolutePath(string storedPath)
    {
        if (Path.IsPathRooted(storedPath))
            return storedPath;

        return Path.Combine(DatabaseConstants.GetDatabaseDirectory(), storedPath);
    }
}
