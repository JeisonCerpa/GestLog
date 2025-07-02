using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using System.IO;

namespace GestLog.Modules.DatabaseConnection
{
    public class GestLogDbContextFactory : IDesignTimeDbContextFactory<GestLogDbContext>
    {
        public GestLogDbContext CreateDbContext(string[] args)
        {
            // Cargar configuración desde config/database-development.json
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("config/database-development.json")
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
