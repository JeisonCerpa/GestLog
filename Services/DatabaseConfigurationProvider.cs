using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using GestLog.Services.Interfaces;

namespace GestLog.Services
{
    /// <summary>
    /// Implementación que lee la configuración de config/database-development.json
    /// </summary>
    public class DatabaseConfigurationProvider : IDatabaseConfigurationProvider
    {
        private readonly IConfigurationRoot _configuration;

        public DatabaseConfigurationProvider()
        {
            _configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("config/database-development.json", optional: false)
                .Build();
        }

        public Task<string> GetConnectionStringAsync()
        {
            var dbSection = _configuration.GetSection("Database");
            string server = dbSection["Server"] ?? "localhost";
            string database = dbSection["Database"] ?? "GestLog";
            string user = dbSection["Username"] ?? "sa";
            string password = dbSection["Password"] ?? "";
            bool integrated = bool.TryParse(dbSection["UseIntegratedSecurity"], out var integ) ? integ : false;
            bool trustCert = bool.TryParse(dbSection["TrustServerCertificate"], out var trust) ? trust : true;

            string connectionString = integrated
                ? $"Server={server};Database={database};Integrated Security=True;TrustServerCertificate={trustCert};"
                : $"Server={server};Database={database};User Id={user};Password={password};TrustServerCertificate={trustCert};";

            return Task.FromResult(connectionString);
        }
    }
}
