using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using System;
using System.IO;

namespace GestLog.Modules.DatabaseConnection
{
    public class GestLogDbContextFactory : IDesignTimeDbContextFactory<GestLogDbContext>
    {        public GestLogDbContext CreateDbContext(string[] args)
        {            // Detectar entorno automáticamente
            var environment = Environment.GetEnvironmentVariable("GESTLOG_ENVIRONMENT") ?? "Production";
            var isDevelopment = environment.Equals("Development", StringComparison.OrdinalIgnoreCase);
            var configFile = environment.ToLower() switch
            {
                "development" => "config/database-development.json",
                "testing" => "config/database-testing.json",
                _ => "config/database-production.json"
            };

            // Verificar que el archivo existe, si no usar production como fallback
            if (!File.Exists(configFile))
            {
                configFile = "config/database-production.json";
            }

            // Cargar configuración desde el archivo detectado
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile(configFile)
                .Build();

            var dbSection = configuration.GetSection("Database");
            string server = isDevelopment
                ? (dbSection["Server"] ?? "localhost")
                : (Environment.GetEnvironmentVariable("GESTLOG_DB_SERVER") ?? dbSection["Server"] ?? "localhost");
            string database = isDevelopment
                ? (dbSection["Database"] ?? "GestLog")
                : (Environment.GetEnvironmentVariable("GESTLOG_DB_NAME") ?? dbSection["Database"] ?? "GestLog");
            string user = isDevelopment
                ? (string.IsNullOrWhiteSpace(dbSection["Username"])
                    ? (Environment.GetEnvironmentVariable("GESTLOG_DB_USER") ?? "sa")
                    : (dbSection["Username"] ?? "sa"))
                : (Environment.GetEnvironmentVariable("GESTLOG_DB_USER") ?? dbSection["Username"] ?? "sa");
            string password = isDevelopment
                ? (string.IsNullOrWhiteSpace(dbSection["Password"])
                    ? (Environment.GetEnvironmentVariable("GESTLOG_DB_PASSWORD") ?? "")
                    : (dbSection["Password"] ?? ""))
                : (Environment.GetEnvironmentVariable("GESTLOG_DB_PASSWORD") ?? dbSection["Password"] ?? "");
            bool integrated = isDevelopment
                ? (bool.TryParse(dbSection["UseIntegratedSecurity"], out var integFromConfigDev) && integFromConfigDev)
                : (bool.TryParse(Environment.GetEnvironmentVariable("GESTLOG_DB_INTEGRATED_SECURITY"), out var integFromEnv)
                    ? integFromEnv
                    : (bool.TryParse(dbSection["UseIntegratedSecurity"], out var integFromConfigFallback) && integFromConfigFallback));
            bool trustCert = bool.TryParse(dbSection["TrustServerCertificate"], out var trust) ? trust : true;

            string connectionString = integrated
                ? $"Server={server};Database={database};Integrated Security=True;TrustServerCertificate={trustCert};"
                : $"Server={server};Database={database};User Id={user};Password={password};TrustServerCertificate={trustCert};";            var optionsBuilder = new DbContextOptionsBuilder<GestLogDbContext>();
            optionsBuilder.UseSqlServer(connectionString, sqlOptions =>
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

            return new GestLogDbContext(optionsBuilder.Options);
        }
    }
}
