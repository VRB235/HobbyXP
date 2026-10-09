using Microsoft.EntityFrameworkCore;

namespace HobbyXP.Data;

/// <summary>
/// Las medallas ya no se siembran: el usuario las crea (umbral de XP por hobby).
/// Se conserva el tipo para no romper el historial de migraciones EF.
/// </summary>
public static class MedalCatalog
{
    public static void Seed(ModelBuilder modelBuilder)
    {
        // Intencionalmente vacío: catálogo de medallas definido en runtime por el usuario.
    }
}
