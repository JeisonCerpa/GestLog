using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using GestLog.Modules.DatabaseConnection;

namespace GestLog
{
    public static class Startup
    {
        public static void ConfigureDatabase(IServiceCollection services, IConfiguration configuration)
        {
            var dbSection = configuration.GetSection("Database");
            string connectionString = BuildConnectionString(dbSection);
            services.AddDbContextFactory<GestLogDbContext>(options =>
            {
                options.UseSqlServer(connectionString);
            });
        }

        private static string BuildConnectionString(IConfiguration dbSection)
        {
            // Aquí puedes construir la cadena usando variables de entorno o fallback
            var server = Environment.GetEnvironmentVariable("GESTLOG_DB_SERVER") ?? dbSection["FallbackServer"];
            var database = Environment.GetEnvironmentVariable("GESTLOG_DB_NAME") ?? dbSection["FallbackDatabase"];
            var user = Environment.GetEnvironmentVariable("GESTLOG_DB_USER") ?? dbSection["FallbackUsername"];
            var password = Environment.GetEnvironmentVariable("GESTLOG_DB_PASSWORD") ?? dbSection["FallbackPassword"];
            var integrated = bool.TryParse(Environment.GetEnvironmentVariable("GESTLOG_DB_INTEGRATED_SECURITY"), out var integ) ? integ : bool.Parse(dbSection["FallbackUseIntegratedSecurity"] ?? "false");
            var trustCert = bool.TryParse(dbSection["FallbackTrustServerCertificate"], out var trust) ? trust : true;

            if (integrated)
            {
                return $"Server={server};Database={database};Integrated Security=True;TrustServerCertificate={trustCert};";
            }
            else
            {
                return $"Server={server};Database={database};User Id={user};Password={password};TrustServerCertificate={trustCert};";
            }
        }
    }
}
