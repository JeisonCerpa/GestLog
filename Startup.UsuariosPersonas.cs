using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Modules.Usuarios.Interfaces;
using Modules.Usuarios.Services;
using Modules.Usuarios.ViewModels;
using Modules.Personas.Interfaces;
using Modules.Personas.Services;
using GestLog.Modules.Usuarios.ViewModels;
using GestLog.Services.Interfaces;
using GestLog.Services;
using GestLog.Views.Usuarios;
using GestLog.Modules.DatabaseConnection;
using Microsoft.EntityFrameworkCore;

namespace GestLog
{
    public static class StartupUsuariosPersonas
    {
        public static void ConfigureUsuariosPersonasServices(IServiceCollection services)
        {
            // Servicios y repositorios de Usuarios
            services.AddScoped<IUsuarioService, UsuarioService>();
            services.AddScoped<IUsuarioRepository, UsuarioRepository>();
            services.AddScoped<ICargoService, CargoService>();
            services.AddScoped<ICargoRepository, CargoRepository>();
            services.AddScoped<IRolService, RolService>();
            services.AddScoped<IRolRepository>(provider =>
            {
                var dbContextFactory = provider.GetRequiredService<IDbContextFactory<GestLogDbContext>>();
                return new RolRepository(dbContextFactory);
            });
            services.AddScoped<IPermisoService, PermisoService>();
            services.AddScoped<IPermisoRepository, PermisoRepository>();
            services.AddScoped<IAuditoriaService, AuditoriaService>();
            services.AddScoped<IAuditoriaRepository, AuditoriaRepository>();

            // ViewModels de Usuarios
            services.AddTransient<UsuarioManagementViewModel>();
            services.AddTransient<RolManagementViewModel>();
            services.AddTransient<PermisoManagementViewModel>();
            services.AddTransient<AuditoriaManagementViewModel>();
            services.AddTransient<GestLog.Modules.Usuarios.ViewModels.IdentidadCatalogosHomeViewModel>();
            services.AddTransient<CatalogosManagementViewModel>();

            // Servicios y repositorios de Personas
            services.AddScoped<IPersonaService, PersonaService>();
            services.AddScoped<IPersonaRepository, PersonaRepository>(provider =>
            {
                var dbContextFactory = provider.GetRequiredService<IDbContextFactory<GestLogDbContext>>();
                return new PersonaRepository(dbContextFactory);
            });
            // Registrar proveedor seguro de configuración de base de datos
            services.AddSingleton<IDatabaseConfigurationProvider, SecureDatabaseConfigurationService>();
            services.AddScoped<ITipoDocumentoRepository>(provider =>
            {
                var dbConfigProvider = provider.GetRequiredService<IDatabaseConfigurationProvider>();
                var connectionString = dbConfigProvider.GetConnectionStringAsync().GetAwaiter().GetResult();
                return new TipoDocumentoRepository(connectionString);
            });
            // ViewModels de Personas (agregar cuando existan)
            services.AddTransient<PersonaManagementViewModel>();
            services.AddSingleton<IModalService, ModalService>();
        }
    }
}
