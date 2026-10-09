using System.IO;
using HobbyXP.Data;

namespace HobbyXP.Helpers;

/// <summary>
/// Almacén de imagen de portada (una por entidad) relativo al directorio de datos.
/// </summary>
public static class HobbyCoverPhotoStorage
{
    public static class Folders
    {
        public const string MediaEntries = "MediaEntryCovers";
        public const string MediaSeries = "MediaSeriesCovers";
        public const string VideoGames = "VideoGameCovers";
        public const string Books = "BookCovers";
        public const string Courses = "CourseCovers";
    }

    private const string StagingFolderName = "_staging";

    public static string? ImportToStaging(string folderName, string sourcePath)
    {
        if (string.IsNullOrWhiteSpace(sourcePath) || !File.Exists(sourcePath))
            return null;

        if (IsManagedPath(folderName, sourcePath))
            return Path.GetFullPath(ResolveAbsolutePath(sourcePath) ?? sourcePath);

        var stagingDirectory = Path.Combine(
            DatabaseConstants.GetDatabaseDirectory(),
            folderName,
            StagingFolderName);
        Directory.CreateDirectory(stagingDirectory);

        var extension = NormalizeExtension(sourcePath);
        var destinationPath = Path.Combine(stagingDirectory, $"{Guid.NewGuid():N}{extension}");
        File.Copy(sourcePath, destinationPath, overwrite: true);
        return destinationPath;
    }

    public static string? SaveFromSource(string folderName, int entityId, string sourcePath)
    {
        if (string.IsNullOrWhiteSpace(sourcePath) || !File.Exists(sourcePath))
            return null;

        var destinationDirectory = GetEntityDirectory(folderName, entityId);
        Directory.CreateDirectory(destinationDirectory);

        foreach (var existing in Directory.EnumerateFiles(destinationDirectory))
            File.Delete(existing);

        var extension = NormalizeExtension(sourcePath);
        var destinationPath = Path.Combine(destinationDirectory, $"cover{extension}");
        File.Copy(sourcePath, destinationPath, overwrite: true);

        TryDeleteIfStaging(folderName, sourcePath);

        return ToStoredPath(destinationPath);
    }

    public static string? EnsureManaged(string folderName, int entityId, string? storedPath)
    {
        if (string.IsNullOrWhiteSpace(storedPath))
            return null;

        if (IsManagedForEntity(folderName, entityId, storedPath))
        {
            var managedAbsolute = ResolveAbsolutePath(storedPath);
            return managedAbsolute is null ? storedPath : ToStoredPath(managedAbsolute);
        }

        var absolute = ResolveAbsolutePath(storedPath);
        if (absolute is null && Path.IsPathRooted(storedPath) && File.Exists(storedPath))
            absolute = Path.GetFullPath(storedPath);

        return absolute is null ? null : SaveFromSource(folderName, entityId, absolute);
    }

    public static string? ResolveAbsolutePath(string? storedPath)
    {
        if (string.IsNullOrWhiteSpace(storedPath))
            return null;

        var absolute = Path.IsPathRooted(storedPath)
            ? storedPath
            : Path.Combine(DatabaseConstants.GetDatabaseDirectory(), storedPath);

        absolute = Path.GetFullPath(absolute);
        return File.Exists(absolute) ? absolute : null;
    }

    public static bool IsManagedPath(string folderName, string? storedPath)
    {
        if (string.IsNullOrWhiteSpace(storedPath))
            return false;

        var resolved = ResolveAbsolutePath(storedPath)
            ?? (Path.IsPathRooted(storedPath) ? Path.GetFullPath(storedPath) : null);
        if (resolved is null)
            return false;

        var managedRoot = Path.GetFullPath(
            Path.Combine(DatabaseConstants.GetDatabaseDirectory(), folderName));
        return resolved.StartsWith(managedRoot, StringComparison.OrdinalIgnoreCase);
    }

    public static bool IsManagedForEntity(string folderName, int entityId, string? storedPath)
    {
        if (!IsManagedPath(folderName, storedPath))
            return false;

        var resolved = ResolveAbsolutePath(storedPath);
        if (resolved is null)
            return false;

        var entityDir = Path.GetFullPath(GetEntityDirectory(folderName, entityId));
        return resolved.StartsWith(entityDir + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)
            || string.Equals(Path.GetDirectoryName(resolved), entityDir, StringComparison.OrdinalIgnoreCase);
    }

    public static void DeleteStoredPhoto(string folderName, int entityId, string? storedPath)
    {
        var absolute = ResolveAbsolutePath(storedPath);
        if (absolute is not null && File.Exists(absolute) && IsManagedForEntity(folderName, entityId, absolute))
            File.Delete(absolute);

        var directory = GetEntityDirectory(folderName, entityId);
        if (Directory.Exists(directory) && !Directory.EnumerateFileSystemEntries(directory).Any())
            Directory.Delete(directory, recursive: false);
    }

    public static void DeleteEntityFolder(string folderName, int entityId)
    {
        var directory = GetEntityDirectory(folderName, entityId);
        if (Directory.Exists(directory))
            Directory.Delete(directory, recursive: true);
    }

    public static void DeleteStagingFile(string folderName, string? absoluteOrStoredPath) =>
        TryDeleteIfStaging(folderName, absoluteOrStoredPath);

    private static void TryDeleteIfStaging(string folderName, string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return;

        var absolute = ResolveAbsolutePath(path)
            ?? (Path.IsPathRooted(path) && File.Exists(path) ? Path.GetFullPath(path) : null);
        if (absolute is null)
            return;

        var stagingRoot = Path.GetFullPath(
            Path.Combine(DatabaseConstants.GetDatabaseDirectory(), folderName, StagingFolderName));
        if (!absolute.StartsWith(stagingRoot, StringComparison.OrdinalIgnoreCase))
            return;

        try
        {
            if (File.Exists(absolute))
                File.Delete(absolute);
        }
        catch
        {
            // Mejor esfuerzo.
        }
    }

    private static string GetEntityDirectory(string folderName, int entityId) =>
        Path.Combine(DatabaseConstants.GetDatabaseDirectory(), folderName, entityId.ToString());

    private static string NormalizeExtension(string sourcePath)
    {
        var extension = Path.GetExtension(sourcePath);
        return string.IsNullOrWhiteSpace(extension) ? ".png" : extension;
    }

    private static string ToStoredPath(string absolutePath) =>
        Path.GetRelativePath(DatabaseConstants.GetDatabaseDirectory(), Path.GetFullPath(absolutePath));
}
