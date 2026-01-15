using Microsoft.Extensions.DependencyInjection;
using GestLog.Modules.GestionMantenimientos.Services.Data;
using GestLog.Modules.GestionMantenimientos.Services.Export;
using GestLog.Modules.GestionMantenimientos.Services.Autocomplete;
using GestLog.Modules.GestionMantenimientos.Services.Cache;
using GestLog.Modules.GestionMantenimientos.Interfaces.Data;
using GestLog.Modules.GestionMantenimientos.Interfaces.Export;
using GestLog.Modules.GestionMantenimientos.Interfaces.Cache;
using GestLog.Services.Core.Logging;
using GestLog.Modules.DatabaseConnection;
using Microsoft.EntityFrameworkCore;

namespace GestLog.Modules.GestionMantenimientos.Services
{
    /// <summary>
    /// Extensiones de DI para registrar todos los servicios del módulo GestionMantenimientos.
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Registra todos los servicios del módulo GestionMantenimientos en el contenedor DI.
        /// </summary>
        public static IServiceCollection AddGestionMantenimientosServices(this IServiceCollection services)
        {
            // ===== Data Services =====
            services.AddScoped<ICronogramaService, CronogramaService>();
            
            // SeguimientoService con factory para inyectar dependencias
            services.AddScoped<ISeguimientoService>(sp =>
            {
                var logger = sp.GetRequiredService<IGestLogLogger>();
                var dbContextFactory = sp.GetRequiredService<IDbContextFactory<GestLogDbContext>>();
                var cronogramaService = sp.GetRequiredService<ICronogramaService>();
                return new SeguimientoService(logger, dbContextFactory, cronogramaService);
            });
            
            services.AddScoped<IEquipoService, EquipoService>();
            services.AddScoped<IMantenimientoService, MaintenanceService>();            // ===== Export Services =====
            services.AddTransient<ICronogramaExportService, CronogramaExportService>();
            services.AddTransient<ISeguimientosExportService, SeguimientosExportService>();
            services.AddTransient<HojaVidaExportService>();

            // ===== Autocomplete Services =====
            services.AddScoped<ClasificacionAutocompletadoService>();
            services.AddScoped<CompradoAAutocompletadoService>();
            services.AddScoped<MarcaAutocompletadoService>();

            // ===== Cache Services =====
            services.AddSingleton<IEquipoCacheService, EquipoCacheService>();

            return services;
        }
    }
}
