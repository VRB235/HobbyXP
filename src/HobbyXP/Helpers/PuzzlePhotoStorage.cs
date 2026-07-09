using System.IO;
using System.Text.Json;
using HobbyXP.Data;

namespace HobbyXP.Helpers;

public static class PuzzlePhotoStorage
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

    public static IReadOnlyList<string> SavePhotos(int puzzleId, IEnumerable<string> sourcePaths)
    {
        var destinationDirectory = Path.Combine(
            DatabaseConstants.GetDatabaseDirectory(),
            "PuzzlePhotos",
            puzzleId.ToString());

        Directory.CreateDirectory(destinationDirectory);

        var savedPaths = new List<string>();
        foreach (var sourcePath in sourcePaths.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            if (!File.Exists(sourcePath))
                continue;

            var extension = Path.GetExtension(sourcePath);
            var destinationPath = Path.Combine(destinationDirectory, $"{Guid.NewGuid():N}{extension}");
            File.Copy(sourcePath, destinationPath, overwrite: true);
            savedPaths.Add(destinationPath);
        }

        return savedPaths;
    }

    public static void DeleteStoredPhotos(int puzzleId, string? photoPathValue)
    {
        foreach (var path in Deserialize(photoPathValue))
        {
            if (File.Exists(path))
                File.Delete(path);
        }

        var directory = Path.Combine(
            DatabaseConstants.GetDatabaseDirectory(),
            "PuzzlePhotos",
            puzzleId.ToString());

        if (Directory.Exists(directory))
            Directory.Delete(directory, recursive: true);
    }

    private static string ToAbsolutePath(string storedPath)
    {
        if (Path.IsPathRooted(storedPath))
            return storedPath;

        return Path.Combine(DatabaseConstants.GetDatabaseDirectory(), storedPath);
    }
}
