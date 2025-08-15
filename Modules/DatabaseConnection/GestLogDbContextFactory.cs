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
            string server = dbSection["Server"] ?? "localhost";
            string database = dbSection["Database"] ?? "GestLog";
            string user = dbSection["Username"] ?? "sa";
            string password = dbSection["Password"] ?? "";
            bool integrated = bool.TryParse(dbSection["UseIntegratedSecurity"], out var integ) ? integ : false;
            bool trustCert = bool.TryParse(dbSection["TrustServerCertificate"], out var trust) ? trust : true;

            string connectionString = integrated
                ? $"Server={server};Database={database};Integrated Security=True;TrustServerCertificate={trustCert};"
                : $"Server={server};Database={database};User Id={user};Password={password};TrustServerCertificate={trustCert};";

            var optionsBuilder = new DbContextOptionsBuilder<GestLogDbContext>();
            optionsBuilder.UseSqlServer(connectionString);

            return new GestLogDbContext(optionsBuilder.Options);
        }
    }
}
