using System.IO;
using HobbyXP.Data;
using HobbyXP.Models.Enums;
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
        await using var db = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        // Historial de actividades (orden respeta FKs Restrict/Cascade).
        await db.GymWorkoutEntries.ExecuteDeleteAsync(cancellationToken);
        await db.GymWorkouts.ExecuteDeleteAsync(cancellationToken);
        await db.RunningSessions.ExecuteDeleteAsync(cancellationToken);
        await db.OfficialRaces.ExecuteDeleteAsync(cancellationToken);
        await db.DietDayLogs.ExecuteDeleteAsync(cancellationToken);
        await db.MediaSeriesChapterLogs.ExecuteDeleteAsync(cancellationToken);
        await db.MediaSeries.ExecuteDeleteAsync(cancellationToken);
        await db.MediaEntries.ExecuteDeleteAsync(cancellationToken);
        await db.VideoGameProgressLogs.ExecuteDeleteAsync(cancellationToken);
        await db.VideoGames.ExecuteDeleteAsync(cancellationToken);
        await db.BookReadingLogs.ExecuteDeleteAsync(cancellationToken);
        await db.Books.ExecuteDeleteAsync(cancellationToken);
        await db.CourseSessionLogs.ExecuteDeleteAsync(cancellationToken);
        await db.Courses.ExecuteDeleteAsync(cancellationToken);
        await db.Puzzles.ExecuteDeleteAsync(cancellationToken);

        // Progresión / auditoría / medallas ganadas.
        await db.EarnedMedals.ExecuteDeleteAsync(cancellationToken);
        await db.XpTransactions.ExecuteDeleteAsync(cancellationToken);
        await db.Milestones.ExecuteDeleteAsync(cancellationToken);
        await db.WeeklyQuotaEvaluations.ExecuteDeleteAsync(cancellationToken);

        // Catálogos conservados: Exercises, MedalDefinitions, AchievementRules.
        // Premios: se mantienen las definiciones; se revierten canjes.
        var redeemedRewards = await db.Rewards
            .Where(r => r.Status == RewardStatus.Redeemed)
            .ToListAsync(cancellationToken);
        foreach (var reward in redeemedRewards)
        {
            reward.Status = RewardStatus.Available;
            reward.RedeemedAt = null;
            reward.RedeemedCostInPoints = null;
            reward.UpdatedAt = DateTime.UtcNow;
        }

        var now = DateTime.UtcNow;
        var profiles = await db.PlayerProfiles
            .Include(p => p.HobbyProgresses)
            .ToListAsync(cancellationToken);

        foreach (var profile in profiles)
        {
            profile.CurrentLevel = 1;
            profile.TotalXp = 0;
            profile.SpendableXp = 0;
            profile.WeeklyQuotaTrackingStartedAtUtc = null;
            profile.HonorTitle = null;
            profile.EquippedRewardId = null;
            profile.DisciplineImmunityUntilUtc = null;
            profile.LastSeenEarnedMedalCount = 0;
            profile.SpendableLedgerInitialized = true;
            profile.SpendableProgressBaselineApplied = true;
            profile.UpdatedAt = now;

            foreach (var hobby in profile.HobbyProgresses)
            {
                hobby.CurrentLevel = 1;
                hobby.TotalXp = 0;
                hobby.UpdatedAt = now;
            }
        }

        await db.SaveChangesAsync(cancellationToken);

        await HobbyXpDatabaseInitializer.EnsureHobbyProgressRowsAsync(db, cancellationToken);

        // Fotos de puzzles son historial; el avatar del perfil se conserva.
        // En tests in-memory no se toca el directorio de datos de la app.
        var connectionString = db.Database.GetConnectionString() ?? string.Empty;
        if (connectionString.Contains(DatabaseConstants.FileName, StringComparison.OrdinalIgnoreCase))
            ClearDirectoryContents(Path.Combine(DatabaseConstants.GetDatabaseDirectory(), "PuzzlePhotos"));
    }

    private static void ClearDirectoryContents(string directoryPath)
    {
        if (!Directory.Exists(directoryPath))
            return;

        Directory.Delete(directoryPath, recursive: true);
    }
}
