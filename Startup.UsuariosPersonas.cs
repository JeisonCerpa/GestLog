using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Modules.Usuarios.Interfaces;
using Modules.Usuarios.Services;
using Modules.Usuarios.ViewModels;
using Modules.Personas.Interfaces;
using Modules.Personas.Services;
using GestLog.Modules.Usuarios.ViewModels;
using GestLog.Modules.Usuarios.Interfaces; // Para autenticación
using GestLog.Modules.Usuarios.Services; // Para servicios de autenticación
using GestLog.Services.Interfaces;
using GestLog.Services;
using GestLog.Views.Usuarios;
using GestLog.Modules.DatabaseConnection;
using Microsoft.EntityFrameworkCore;
using GestLog.Modules.GestionMantenimientos.Interfaces;
using GestLog.Modules.GestionMantenimientos.Services;
using GestLog.Modules.GestionMantenimientos.ViewModels;
using GestLog.Modules.GestionEquiposInformaticos.Interfaces;
using GestLog.Modules.GestionEquiposInformaticos.Services;

namespace GestLog
{
    public static class StartupUsuariosPersonas
    {        
        public static void ConfigureUsuariosPersonasServices(IServiceCollection services)
        {
            Console.WriteLine("🔧 Configurando servicios de Usuarios y Personas...");
            
            // Servicios y repositorios de Usuarios
            services.AddScoped<IUsuarioService, UsuarioService>();
            services.AddScoped<IUsuarioRepository, UsuarioRepository>();
            
            // 🔐 SERVICIOS DE AUTENTICACIÓN
            services.AddScoped<IAuthenticationService, AuthenticationService>();
            services.AddSingleton<ICurrentUserService, CurrentUserService>();
            
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
            services.AddScoped<IRolPermisoRepository, RolPermisoRepository>();            
            services.AddScoped<IAuditoriaService, AuditoriaService>();
            services.AddScoped<IAuditoriaRepository, AuditoriaRepository>();            
            // ViewModels de Usuarios
            
            services.AddSingleton<global::Modules.Usuarios.ViewModels.UsuarioManagementViewModel>();
            
            services.AddSingleton<RolManagementViewModel>();
            services.AddSingleton<AuditoriaManagementViewModel>();
            services.AddTransient<GestLog.Modules.Usuarios.ViewModels.LoginViewModel>(); // LoginViewModel puede seguir siendo transient ya que se crea por sesión
            services.AddSingleton<GestLog.Modules.Usuarios.ViewModels.IdentidadCatalogosHomeViewModel>();
            services.AddSingleton<CatalogosManagementViewModel>();
            services.AddSingleton<GestionPermisosRolViewModel>();            // Servicios y ViewModels para Gestión de Mantenimientos
            services.AddScoped<IMantenimientoService, MaintenanceService>();
            
            // Servicios para Gestión de Equipos Informáticos
            services.AddScoped<GestLog.Modules.GestionEquiposInformaticos.Interfaces.IPlanCronogramaService, GestLog.Modules.GestionEquiposInformaticos.Services.PlanCronogramaService>();
              // Registrar ViewModels: CronogramaDiario como Transient (instancia por vista), el registrador es transient (modal)
            services.AddTransient<GestLog.Modules.GestionEquiposInformaticos.ViewModels.CronogramaDiarioViewModel>();
            services.AddTransient<GestLog.Modules.GestionMantenimientos.ViewModels.RegistrarMantenimientoViewModel>();
            // Registrar ViewModel contenedor para GestionEquipos
            services.AddTransient<GestLog.ViewModels.Tools.GestionEquipos.GestionEquiposHomeViewModel>();

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
            services.AddSingleton<PersonaManagementViewModel>();
            services.AddSingleton<IModalService, ModalService>();
            
            // Registrar CurrentUserInfo como servicio Scoped
            services.AddScoped<GestLog.Modules.Usuarios.Models.Authentication.CurrentUserInfo>(provider =>
            {
                var currentUserService = provider.GetRequiredService<GestLog.Modules.Usuarios.Interfaces.ICurrentUserService>();
                return currentUserService.Current ?? new GestLog.Modules.Usuarios.Models.Authentication.CurrentUserInfo
                {
                    UserId = Guid.Empty,
                    Username = "",
                    FullName = "",
                    Email = "",
                    LoginTime = DateTime.UtcNow,
                    LastActivity = DateTime.UtcNow,
                    Roles = new(),
                    Permissions = new()
                };
            });
        }
    }
}
