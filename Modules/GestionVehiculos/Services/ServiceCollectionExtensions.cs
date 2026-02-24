using Microsoft.Extensions.DependencyInjection;
using GestLog.Modules.GestionVehiculos.ViewModels.Vehicles;
using GestLog.Modules.GestionVehiculos.ViewModels.Mantenimientos;
using GestLog.Modules.GestionVehiculos.Interfaces.Data;
using GestLog.Modules.GestionVehiculos.Interfaces.Dialog;
using GestLog.Modules.GestionVehiculos.Interfaces.Storage;
using GestLog.Modules.GestionVehiculos.Services.Data;
using GestLog.Modules.GestionVehiculos.Services.Dialog;
using GestLog.Modules.GestionVehiculos.Views.Vehicles;

namespace GestLog.Modules.GestionVehiculos.Services
{
    public static class ServiceCollectionExtensions
    {          public static IServiceCollection AddGestionVehiculosModule(this IServiceCollection services)
        {
            // ✅ Data Services - Vehículos
            services.AddScoped<IVehicleService, VehicleService>();
            services.AddScoped<IVehicleDocumentService, VehicleDocumentService>();

            // ✅ Data Services - Mantenimientos
            services.AddScoped<IPlantillaMantenimientoService, PlantillaMantenimientoService>();
            services.AddScoped<IPlanMantenimientoVehiculoService, PlanMantenimientoVehiculoService>();
            services.AddScoped<IEjecucionMantenimientoService, EjecucionMantenimientoService>();

            // ✅ Dialog Services
            services.AddTransient<IVehicleDocumentDialogService, VehicleDocumentDialogService>();
            services.AddSingleton<IAppDialogService, AppDialogService>();

            // ✅ Storage Services
            services.AddSingleton<IPhotoStorageService, NetworkFileStorageService>();

            // ✅ Views
            services.AddTransient<GestionVehiculosHomeView>();
            services.AddTransient<VehicleFormDialog>();
            services.AddTransient<VehicleDocumentsView>();
            services.AddTransient<VehicleDocumentDialog>();

            // ✅ ViewModels
            services.AddTransient<GestionVehiculosHomeViewModel>();
            services.AddTransient<VehicleFormViewModel>();
            services.AddTransient<VehicleDetailsViewModel>();
            services.AddTransient<VehicleDocumentsViewModel>();
            services.AddTransient<VehicleDocumentDialogViewModel>();
            services.AddTransient<PlantillasMantenimientoViewModel>();
            services.AddTransient<PlanesMantenimientoViewModel>();
            services.AddTransient<EjecucionesMantenimientoViewModel>();

            return services;
        }
    }
}
