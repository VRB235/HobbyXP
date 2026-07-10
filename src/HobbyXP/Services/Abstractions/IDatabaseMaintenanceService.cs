namespace HobbyXP.Services.Abstractions;

public interface IDatabaseMaintenanceService
{
    Task ExportDatabaseAsync(string destinationPath, CancellationToken cancellationToken = default);

    Task ResetApplicationDataAsync(CancellationToken cancellationToken = default);
}
