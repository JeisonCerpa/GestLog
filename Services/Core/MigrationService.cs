using Microsoft.EntityFrameworkCore;
using GestLog.Modules.DatabaseConnection;
using GestLog.Services.Core.Logging;
using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace GestLog.Services.Core
{
    /// <summary>
    /// Interfaz para servicio de migraciones de base de datos
    /// </summary>
    public interface IMigrationService
    {
        /// <summary>
        /// Asegura que todas las migraciones pendientes se apliquen a la base de datos automáticamente
        /// </summary>
        Task EnsureDatabaseUpdatedAsync();
    }

    /// <summary>
    /// Implementación del servicio de migraciones
    /// Aplica automáticamente todas las migraciones pendientes al iniciar la aplicación
    /// </summary>
    public class MigrationService : IMigrationService
    {
        private readonly IDbContextFactory<GestLogDbContext> _contextFactory;
        private readonly IGestLogLogger _logger;

        public MigrationService(
            IDbContextFactory<GestLogDbContext> contextFactory,
            IGestLogLogger logger)
        {
            _contextFactory = contextFactory ?? throw new ArgumentNullException(nameof(contextFactory));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Aplica automáticamente todas las migraciones pendientes
        /// </summary>
        public async Task EnsureDatabaseUpdatedAsync()
        {
            try
            {
                _logger.Logger.LogInformation("🔄 Iniciando verificación de migraciones de base de datos...");

                await using var context = _contextFactory.CreateDbContext();

                // Verificar que la conexión es válida
                var canConnect = await context.Database.CanConnectAsync();
                if (!canConnect)
                {
                    _logger.Logger.LogError("❌ No se puede establecer conexión con la base de datos.");
                    throw new InvalidOperationException("No se puede conectar a la base de datos. Verifique las credenciales y la disponibilidad del servidor.");
                }

                _logger.Logger.LogInformation("🔗 Conexión a la base de datos verificada correctamente.");

                // Obtener migraciones pendientes
                var pendingMigrations = await context.Database.GetPendingMigrationsAsync();
                var pendingMigrationsList = pendingMigrations.ToList();

                if (pendingMigrationsList.Any())
                {
                    _logger.Logger.LogWarning("⏳ Se encontraron {MigrationCount} migración(es) pendiente(s):", pendingMigrationsList.Count);
                    foreach (var migration in pendingMigrationsList)
                    {
                        _logger.Logger.LogWarning("   - {Migration}", migration);
                    }

                    // Aplicar migraciones pendientes
                    _logger.Logger.LogInformation("📦 Aplicando migraciones pendientes a la base de datos...");
                    await context.Database.MigrateAsync();

                    _logger.Logger.LogInformation("✅ ¡Todas las migraciones se aplicaron exitosamente!");
                }
                else
                {
                    _logger.Logger.LogInformation("✅ Base de datos actualizada. No hay migraciones pendientes.");
                }

                // Obtener migraciones aplicadas para confirmación
                var appliedMigrations = await context.Database.GetAppliedMigrationsAsync();
                var appliedList = appliedMigrations.ToList();
                _logger.Logger.LogInformation("📊 Total de migraciones aplicadas: {MigrationCount}", appliedList.Count);
            }
            catch (Exception ex)
            {
                _logger.Logger.LogError(ex, "❌ Error al aplicar migraciones: {ErrorMessage}", ex.Message);
                throw;
            }
        }
    }
}
