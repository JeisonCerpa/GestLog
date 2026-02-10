using Microsoft.Extensions.DependencyInjection;
using GestLog.Modules.GestionVehiculos.ViewModels.Vehicles;
using GestLog.Modules.GestionVehiculos.Interfaces.Data;
using GestLog.Modules.GestionVehiculos.Interfaces;
using GestLog.Modules.GestionVehiculos.Services.Data;
using GestLog.Modules.GestionVehiculos.Views.Vehicles;

namespace GestLog.Modules.GestionVehiculos.Services
{
    public static class ServiceCollectionExtensions
    {        
        public static IServiceCollection AddGestionVehiculosModule(this IServiceCollection services)
        {
            // ✅ Data Services
            services.AddScoped<IVehicleService, VehicleService>();
            services.AddScoped<IVehicleDocumentService, VehicleDocumentService>();

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
            services.AddTransient<VehicleDocumentDialogModel>();

            // Photo storage
            services.AddSingleton<Interfaces.IPhotoStorageService, NetworkFileStorageService>();

            return services;
        }
    }
}
