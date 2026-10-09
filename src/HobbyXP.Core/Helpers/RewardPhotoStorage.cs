using System.IO;
using HobbyXP.Data;

namespace HobbyXP.Helpers;

public static class RewardPhotoStorage
{
    private const string FolderName = "RewardPhotos";
    private const string StagingFolderName = "_staging";

    /// <summary>
    /// Copia inmediata a staging bajo el directorio de datos.
    /// Así el original puede borrarse sin perder la imagen pendiente de guardar.
    /// </summary>
    public static string? ImportToStaging(string sourcePath)
    {
        if (string.IsNullOrWhiteSpace(sourcePath) || !File.Exists(sourcePath))
            return null;

        if (IsManagedPath(sourcePath))
            return Path.GetFullPath(ResolveAbsolutePath(sourcePath) ?? sourcePath);

        var stagingDirectory = Path.Combine(
            DatabaseConstants.GetDatabaseDirectory(),
            FolderName,
            StagingFolderName);
        Directory.CreateDirectory(stagingDirectory);

        var extension = NormalizeExtension(sourcePath);
        var destinationPath = Path.Combine(stagingDirectory, $"{Guid.NewGuid():N}{extension}");
        File.Copy(sourcePath, destinationPath, overwrite: true);
        return destinationPath;
    }

    public static string? SaveFromSource(int rewardId, string sourcePath)
    {
        if (string.IsNullOrWhiteSpace(sourcePath) || !File.Exists(sourcePath))
            return null;

        var destinationDirectory = GetRewardDirectory(rewardId);
        Directory.CreateDirectory(destinationDirectory);

        foreach (var existing in Directory.EnumerateFiles(destinationDirectory))
            File.Delete(existing);

        var extension = NormalizeExtension(sourcePath);
        var destinationPath = Path.Combine(destinationDirectory, $"reward{extension}");
        File.Copy(sourcePath, destinationPath, overwrite: true);

        TryDeleteIfStaging(sourcePath);

        return ToStoredPath(destinationPath);
    }

    /// <summary>
    /// Si la ruta apunta fuera del almacén de la app, la copia dentro y devuelve la ruta relativa.
    /// </summary>
    public static string? EnsureManaged(int rewardId, string? storedPath)
    {
        if (string.IsNullOrWhiteSpace(storedPath))
            return null;

        if (IsManagedForReward(rewardId, storedPath))
        {
            var managedAbsolute = ResolveAbsolutePath(storedPath);
            return managedAbsolute is null ? storedPath : ToStoredPath(managedAbsolute);
        }

        var absolute = ResolveAbsolutePath(storedPath);
        if (absolute is null && Path.IsPathRooted(storedPath) && File.Exists(storedPath))
            absolute = Path.GetFullPath(storedPath);

        return absolute is null ? null : SaveFromSource(rewardId, absolute);
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

    public static bool IsManagedPath(string? storedPath)
    {
        if (string.IsNullOrWhiteSpace(storedPath))
            return false;

        var resolved = ResolveAbsolutePath(storedPath)
            ?? (Path.IsPathRooted(storedPath) ? Path.GetFullPath(storedPath) : null);
        if (resolved is null)
            return false;

        var managedRoot = Path.GetFullPath(
            Path.Combine(DatabaseConstants.GetDatabaseDirectory(), FolderName));
        return resolved.StartsWith(managedRoot, StringComparison.OrdinalIgnoreCase);
    }

    public static bool IsManagedForReward(int rewardId, string? storedPath)
    {
        if (!IsManagedPath(storedPath))
            return false;

        var resolved = ResolveAbsolutePath(storedPath);
        if (resolved is null)
            return false;

        var rewardDir = Path.GetFullPath(GetRewardDirectory(rewardId));
        return resolved.StartsWith(rewardDir + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)
            || string.Equals(Path.GetDirectoryName(resolved), rewardDir, StringComparison.OrdinalIgnoreCase);
    }

    public static void DeleteStoredPhoto(int rewardId, string? storedPath)
    {
        var absolute = ResolveAbsolutePath(storedPath);
        if (absolute is not null && File.Exists(absolute) && IsManagedForReward(rewardId, absolute))
            File.Delete(absolute);

        var directory = GetRewardDirectory(rewardId);
        if (Directory.Exists(directory) && !Directory.EnumerateFileSystemEntries(directory).Any())
            Directory.Delete(directory, recursive: false);
    }

    public static void DeleteRewardFolder(int rewardId)
    {
        var directory = GetRewardDirectory(rewardId);
        if (Directory.Exists(directory))
            Directory.Delete(directory, recursive: true);
    }

    public static void DeleteStagingFile(string? absoluteOrStoredPath)
    {
        TryDeleteIfStaging(absoluteOrStoredPath);
    }

    private static void TryDeleteIfStaging(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return;

        var absolute = ResolveAbsolutePath(path)
            ?? (Path.IsPathRooted(path) && File.Exists(path) ? Path.GetFullPath(path) : null);
        if (absolute is null)
            return;

        var stagingRoot = Path.GetFullPath(
            Path.Combine(DatabaseConstants.GetDatabaseDirectory(), FolderName, StagingFolderName));
        if (!absolute.StartsWith(stagingRoot, StringComparison.OrdinalIgnoreCase))
            return;

        try
        {
            if (File.Exists(absolute))
                File.Delete(absolute);
        }
        catch
        {
            // Mejor esfuerzo: no bloquear el flujo por un staging residual.
        }
    }

    private static string GetRewardDirectory(int rewardId) =>
        Path.Combine(DatabaseConstants.GetDatabaseDirectory(), FolderName, rewardId.ToString());

    private static string NormalizeExtension(string sourcePath)
    {
        var extension = Path.GetExtension(sourcePath);
        return string.IsNullOrWhiteSpace(extension) ? ".png" : extension;
    }

    private static string ToStoredPath(string absolutePath) =>
        Path.GetRelativePath(DatabaseConstants.GetDatabaseDirectory(), Path.GetFullPath(absolutePath));
}
