using Microsoft.EntityFrameworkCore;
using GestLog.Modules.DatabaseConnection;
using System;

namespace GestLog.Services
{
    /// <summary>
    /// Helper centralizado para configurar DbContext con resiliencia consistente
    /// </summary>
    public static class EntityFrameworkConfigurationHelper
    {
        /// <summary>
        /// Configura un DbContextOptionsBuilder con resiliencia estándar para GestLog
        /// </summary>
        /// <param name="optionsBuilder">El builder a configurar</param>
        /// <param name="connectionString">Cadena de conexión</param>
        /// <param name="commandTimeout">Timeout personalizado para comandos (opcional)</param>
        /// <returns>El mismo builder configurado</returns>
        public static DbContextOptionsBuilder<GestLogDbContext> ConfigureWithResilience(
            this DbContextOptionsBuilder<GestLogDbContext> optionsBuilder, 
            string connectionString,
            int? commandTimeout = null)
        {
            return optionsBuilder.UseSqlServer(connectionString, sqlOptions =>
            {
                // Configurar resiliencia para errores transitorios de red y base de datos
                sqlOptions.EnableRetryOnFailure(
                    maxRetryCount: 5,
                    maxRetryDelay: TimeSpan.FromSeconds(30),
                    errorNumbersToAdd: new int[] { 
                        2,      // Timeout
                        20,     // Instance failure
                        64,     // Connection failed
                        233,    // Connection init error
                        10053,  // Connection reset by peer
                        10054,  // Connection reset by peer
                        10060,  // Connection timeout
                        40197,  // Service busy
                        40501,  // Service busy
                        40613   // Database unavailable
                    });
                
                // Configurar timeout apropiado
                sqlOptions.CommandTimeout(commandTimeout ?? 30);
            })
            .EnableSensitiveDataLogging(false) // Para producción
            .EnableDetailedErrors(false) // Para producción
            .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.MultipleCollectionIncludeWarning));
        }

        /// <summary>
        /// Crea opciones de DbContext estándar para GestLog con resiliencia
        /// </summary>
        /// <param name="connectionString">Cadena de conexión</param>
        /// <param name="commandTimeout">Timeout personalizado para comandos (opcional)</param>
        /// <returns>Opciones configuradas</returns>
        public static DbContextOptions<GestLogDbContext> CreateStandardOptions(string connectionString, int? commandTimeout = null)
        {
            var optionsBuilder = new DbContextOptionsBuilder<GestLogDbContext>();
            return optionsBuilder.ConfigureWithResilience(connectionString, commandTimeout).Options;
        }

        /// <summary>
        /// Mejora la cadena de conexión existente agregando parámetros de pooling si no los tiene
        /// </summary>
        /// <param name="connectionString">Cadena de conexión base</param>
        /// <returns>Cadena de conexión mejorada</returns>
        public static string EnhanceConnectionString(string connectionString)
        {
            if (string.IsNullOrEmpty(connectionString))
                throw new ArgumentException("Connection string cannot be null or empty", nameof(connectionString));

            // Si ya tiene configuración de pooling, no modificar
            if (connectionString.Contains("Max Pool Size") || connectionString.Contains("Pooling"))
                return connectionString;

            // Agregar configuración de pooling si no la tiene
            return connectionString.TrimEnd(';') + 
                   ";Connection Timeout=30;Command Timeout=30;Max Pool Size=100;Min Pool Size=5;Pooling=true;";
        }
    }
}
