using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using GestLog.Modules.DatabaseConnection;

namespace GestLog.Modules.DatabaseConnection
{
    public static class DatabaseServiceRegistration
    {
        public static IServiceCollection AddGestLogDatabase(this IServiceCollection services, string connectionString)
        {
            services.AddDbContext<GestLogDbContext>(options =>
                options.UseSqlServer(connectionString));
            return services;
        }
    }
}
