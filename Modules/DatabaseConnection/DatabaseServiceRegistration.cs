using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using GestLog.Modules.DatabaseConnection;

namespace GestLog.Modules.DatabaseConnection
{    public static class DatabaseServiceRegistration
    {        public static IServiceCollection AddGestLogDatabase(this IServiceCollection services, string connectionString)
        {
            services.AddDbContext<GestLogDbContext>(options =>
            {
                options.UseSqlServer(connectionString, sqlOptions =>
                {
                    // Configurar resiliencia para errores transitorios
                    sqlOptions.EnableRetryOnFailure(
                        maxRetryCount: 5,
                        maxRetryDelay: TimeSpan.FromSeconds(30),
                        errorNumbersToAdd: new int[] { 2, 20, 64, 233, 10053, 10054, 10060, 40197, 40501, 40613 });
                      // Timeout balanceado: rápido pero permisivo para SSL handshake
                    sqlOptions.CommandTimeout(15);
                })
                .EnableSensitiveDataLogging(false) // Para producción
                .EnableDetailedErrors(false); // Para producción
            });
            return services;
        }
    }
}
