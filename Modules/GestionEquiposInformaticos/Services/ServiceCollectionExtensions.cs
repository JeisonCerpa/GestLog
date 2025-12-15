using Microsoft.Extensions.DependencyInjection;
using GestLog.Modules.GestionEquiposInformaticos.Interfaces.Data;
using GestLog.Modules.GestionEquiposInformaticos.Interfaces.Autocomplete;
using GestLog.Modules.GestionEquiposInformaticos.Interfaces.Dialog;
using GestLog.Modules.GestionEquiposInformaticos.Services.Data;
using GestLog.Modules.GestionEquiposInformaticos.Services.Autocomplete;
using GestLog.Modules.GestionEquiposInformaticos.Services.Dialog;

namespace GestLog.Modules.GestionEquiposInformaticos.Services
{
    /// <summary>
    /// Extensión de IServiceCollection para registrar todos los servicios del módulo GestionEquiposInformaticos
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Agrega todos los servicios del módulo GestionEquiposInformaticos al contenedor de DI
        /// </summary>
        public static IServiceCollection AddGestionEquiposInformaticosServices(this IServiceCollection services)
        {
            // Data Services - CRUD y operaciones de negocio
            services.AddScoped<IEquipoInformaticoService, EquipoInformaticoService>();
            services.AddScoped<IGestionEquiposInformaticosSeguimientoCronogramaService, GestionEquiposInformaticosSeguimientoCronogramaService>();
            services.AddScoped<IPlanCronogramaService, PlanCronogramaService>();
            services.AddScoped<IMantenimientoCorrectivoService, MantenimientoCorrectivoService>();

            // Autocomplete Services - Servicios de autocompletado
            services.AddScoped<IDispositivoAutocompletadoService, DispositivoAutocompletadoService>();
            services.AddScoped<IMarcaAutocompletadoService, MarcaAutocompletadoService>();

            // Dialog Services - Servicios de presentación (diálogos)
            services.AddTransient<IRegistroEjecucionPlanDialogService, RegistroEjecucionPlanDialogService>();
            services.AddTransient<IRegistroMantenimientoEquipoDialogService, RegistroMantenimientoEquipoDialogService>();

            return services;
        }
    }
}
