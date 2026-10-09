namespace HobbyXP.Services.Abstractions;

public interface IDatabaseMaintenanceService
{
    Task ExportDatabaseAsync(string destinationPath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Borra historial de actividades, XP, niveles y medallas ganadas.
    /// Conserva el catálogo de ejercicios, reglas de XP, definiciones de medallas,
    /// premios (revirtiendo canjes) y la personalización del perfil (nombre/avatar/XP base).
    /// </summary>
    Task ResetApplicationDataAsync(CancellationToken cancellationToken = default);
}
