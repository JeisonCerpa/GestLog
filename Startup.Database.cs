using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using GestLog.Modules.DatabaseConnection;
using GestLog.Services;
using GestLog.Services.Core;
using System;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace GestLog
{    public static class Startup
    {        public static void ConfigureDatabase(IServiceCollection services, IConfiguration configuration)
        {
            // Registrar servicio de sincronización de variables de entorno
            services.AddSingleton<IEnvironmentVariableService, EnvironmentVariableService>();

            var dbSection = configuration.GetSection("Database");
            string connectionString = BuildConnectionString(dbSection);
            services.AddDbContextFactory<GestLogDbContext>(options =>
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
                .EnableDetailedErrors(false)
                // Configurar comportamiento de advertencias según entorno
                .ConfigureWarnings(w =>
                {
                    var env = Environment.GetEnvironmentVariable("GESTLOG_ENVIRONMENT") ?? string.Empty;
                    if (env.Equals("Development", StringComparison.OrdinalIgnoreCase))
                    {
                        // En desarrollo, convertir la advertencia en excepción para identificar la consulta exacta
                        w.Throw(RelationalEventId.MultipleCollectionIncludeWarning);
                    }
                    else
                    {
                        // En otros entornos, ignorar para evitar ruido en logs (comportamiento previo)
                        w.Ignore(RelationalEventId.MultipleCollectionIncludeWarning);
                    }
                });
            });

            // Registrar servicio de migraciones para aplicar automáticamente en startup
            services.AddScoped<IMigrationService, MigrationService>();
        }

        private static string BuildConnectionString(IConfiguration dbSection)
        {
            var environment = Environment.GetEnvironmentVariable("GESTLOG_ENVIRONMENT") ?? "Production";
            var isDevelopment = environment.Equals("Development", StringComparison.OrdinalIgnoreCase);

            string? server;
            string? database;
            string? user;
            string? password;
            bool integrated;

            if (isDevelopment)
            {
                // En desarrollo usar config local y no variables globales de producción.
                server = dbSection["Server"] ?? dbSection["FallbackServer"];
                database = dbSection["Database"] ?? dbSection["FallbackDatabase"];
                user = dbSection["Username"];
                if (string.IsNullOrWhiteSpace(user))
                {
                    user = Environment.GetEnvironmentVariable("GESTLOG_DB_USER") ?? dbSection["FallbackUsername"];
                }

                password = dbSection["Password"];
                if (string.IsNullOrWhiteSpace(password))
                {
                    password = Environment.GetEnvironmentVariable("GESTLOG_DB_PASSWORD") ?? dbSection["FallbackPassword"];
                }

                integrated = bool.TryParse(dbSection["UseIntegratedSecurity"], out var integFromConfig)
                    ? integFromConfig
                    : bool.Parse(dbSection["FallbackUseIntegratedSecurity"] ?? "false");
            }
            else
            {
                // Producción/Testing: permitir variables de entorno como prioridad.
                server = Environment.GetEnvironmentVariable("GESTLOG_DB_SERVER") ?? dbSection["FallbackServer"];
                database = Environment.GetEnvironmentVariable("GESTLOG_DB_NAME") ?? dbSection["FallbackDatabase"];
                user = Environment.GetEnvironmentVariable("GESTLOG_DB_USER") ?? dbSection["FallbackUsername"];
                password = Environment.GetEnvironmentVariable("GESTLOG_DB_PASSWORD") ?? dbSection["FallbackPassword"];
                integrated = bool.TryParse(Environment.GetEnvironmentVariable("GESTLOG_DB_INTEGRATED_SECURITY"), out var integFromEnv)
                    ? integFromEnv
                    : bool.Parse(dbSection["FallbackUseIntegratedSecurity"] ?? "false");
            }

            var trustCert = bool.TryParse(dbSection["TrustServerCertificate"], out var trustFromConfig)
                ? trustFromConfig
                : (bool.TryParse(dbSection["FallbackTrustServerCertificate"], out var trustFromFallback) ? trustFromFallback : true);

            if (integrated)
            {                return $"Server={server};Database={database};Integrated Security=True;TrustServerCertificate={trustCert};" +
                       "Connection Timeout=15;Command Timeout=15;Max Pool Size=100;Min Pool Size=5;Pooling=true;";
            }
            else
            {                return $"Server={server};Database={database};User Id={user};Password={password};TrustServerCertificate={trustCert};" +
                       "Connection Timeout=15;Command Timeout=15;Max Pool Size=100;Min Pool Size=5;Pooling=true;";
            }
        }
    }
}
