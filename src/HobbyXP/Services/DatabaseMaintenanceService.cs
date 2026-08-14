using System.IO;
using HobbyXP.Data;
using HobbyXP.Helpers;
using HobbyXP.Services.Abstractions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace HobbyXP.Services;

public sealed class DatabaseMaintenanceService : IDatabaseMaintenanceService
{
    private readonly IDbContextFactory<HobbyXpDbContext> _dbContextFactory;

    public DatabaseMaintenanceService(IDbContextFactory<HobbyXpDbContext> dbContextFactory)
    {
        _dbContextFactory = dbContextFactory;
    }

    public async Task ExportDatabaseAsync(string destinationPath, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(destinationPath))
            throw new ArgumentException("Indique la ruta de destino.", nameof(destinationPath));

        var sourcePath = DatabaseConstants.GetDatabasePath();
        if (!File.Exists(sourcePath))
            throw new InvalidOperationException("No se encontró la base de datos local.");

        var directory = Path.GetDirectoryName(destinationPath);
        if (!string.IsNullOrWhiteSpace(directory))
            Directory.CreateDirectory(directory);

        if (File.Exists(destinationPath))
            File.Delete(destinationPath);

        await using var sourceConnection = new SqliteConnection($"Data Source={sourcePath}");
        await sourceConnection.OpenAsync(cancellationToken);

        await using var destinationConnection = new SqliteConnection($"Data Source={destinationPath}");
        await destinationConnection.OpenAsync(cancellationToken);

        sourceConnection.BackupDatabase(destinationConnection);
    }

    public async Task ResetApplicationDataAsync(CancellationToken cancellationToken = default)
    {
        await using (var db = await _dbContextFactory.CreateDbContextAsync(cancellationToken))
        {
            await db.Database.EnsureDeletedAsync(cancellationToken);
            await db.Database.MigrateAsync(cancellationToken);
            await HobbyXpDatabaseInitializer.EnsurePlayerProfileAsync(db, cancellationToken);
            await HobbyXpDatabaseInitializer.EnsureGeometricLevelScaleAsync(db, cancellationToken);
            await HobbyXpDatabaseInitializer.EnsureHobbyProgressRowsAsync(db, cancellationToken);
            await HobbyXpDatabaseInitializer.EnsureHobbyXpBackfillAsync(db, cancellationToken);
            await HobbyXpDatabaseInitializer.EnsureSpendableLedgerAsync(db, cancellationToken);
        }

        ClearUserDataFolders();
    }

    private static void ClearUserDataFolders()
    {
        ClearDirectoryContents(Path.Combine(DatabaseConstants.GetDatabaseDirectory(), "Avatar"));
        ClearDirectoryContents(Path.Combine(DatabaseConstants.GetDatabaseDirectory(), "PuzzlePhotos"));
    }

    private static void ClearDirectoryContents(string directoryPath)
    {
        if (!Directory.Exists(directoryPath))
            return;

        Directory.Delete(directoryPath, recursive: true);
    }
}
