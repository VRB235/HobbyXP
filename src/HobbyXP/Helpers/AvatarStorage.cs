using System.IO;
using HobbyXP.Data;

namespace HobbyXP.Helpers;

public static class AvatarStorage
{
    private const string AvatarFolder = "Avatar";
    private const string AvatarBaseName = "profile";

    public static string? SaveFromSource(string sourcePath)
    {
        if (string.IsNullOrWhiteSpace(sourcePath) || !File.Exists(sourcePath))
            return null;

        var directory = Path.Combine(DatabaseConstants.GetDatabaseDirectory(), AvatarFolder);
        Directory.CreateDirectory(directory);

        foreach (var existing in Directory.EnumerateFiles(directory))
            File.Delete(existing);

        var extension = Path.GetExtension(sourcePath);
        if (string.IsNullOrWhiteSpace(extension))
            extension = ".png";

        var destinationPath = Path.Combine(directory, AvatarBaseName + extension);
        File.Copy(sourcePath, destinationPath, overwrite: true);

        return ToStoredPath(destinationPath);
    }

    public static string ResolvePath(string? storedPath)
    {
        if (string.IsNullOrWhiteSpace(storedPath))
            return string.Empty;

        return Path.IsPathRooted(storedPath)
            ? storedPath
            : Path.Combine(DatabaseConstants.GetDatabaseDirectory(), storedPath);
    }

    public static bool Exists(string? storedPath) =>
        !string.IsNullOrWhiteSpace(storedPath) && File.Exists(ResolvePath(storedPath));

    public static bool IsManagedPath(string? storedPath)
    {
        if (string.IsNullOrWhiteSpace(storedPath))
            return false;

        var resolved = Path.GetFullPath(ResolvePath(storedPath));
        var managedDirectory = Path.GetFullPath(Path.Combine(DatabaseConstants.GetDatabaseDirectory(), AvatarFolder));
        return resolved.StartsWith(managedDirectory, StringComparison.OrdinalIgnoreCase);
    }

    public static string? MigrateExternalIfNeeded(string? storedPath)
    {
        if (string.IsNullOrWhiteSpace(storedPath) || IsManagedPath(storedPath))
            return storedPath;

        var sourcePath = ResolvePath(storedPath);
        return File.Exists(sourcePath) ? SaveFromSource(sourcePath) : null;
    }

    private static string ToStoredPath(string absolutePath) =>
        Path.GetRelativePath(DatabaseConstants.GetDatabaseDirectory(), absolutePath);
}
