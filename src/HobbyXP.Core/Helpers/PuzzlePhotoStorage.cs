using System.IO;
using HobbyXP.Data;

namespace HobbyXP.Helpers;

public static class PuzzlePhotoStorage
{
    private const string FolderName = "PuzzlePhotos";

    public static IReadOnlyList<string> Deserialize(string? value) =>
        RelativePhotoPathStorage.Deserialize(value);

    public static string? Serialize(IReadOnlyList<string> absolutePaths) =>
        RelativePhotoPathStorage.Serialize(absolutePaths);

    public static IReadOnlyList<string> SavePhotos(int puzzleId, IEnumerable<string> sourcePaths)
    {
        var destinationDirectory = Path.Combine(
            DatabaseConstants.GetDatabaseDirectory(),
            FolderName,
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
            FolderName,
            puzzleId.ToString());

        if (Directory.Exists(directory))
            Directory.Delete(directory, recursive: true);
    }
}
