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
using GestLog.Services.Core.Logging; // ✅ NUEVO: Para IGestLogLogger
using GestLog.Views.Usuarios;
using GestLog.Modules.DatabaseConnection;
using Microsoft.EntityFrameworkCore;
using GestLog.Modules.GestionMantenimientos.Interfaces.Data;
using GestLog.Modules.GestionMantenimientos.Services;
using GestLog.Modules.GestionMantenimientos.Services.Data;
using GestLog.Modules.GestionMantenimientos.Services.Autocomplete;
using GestLog.Modules.GestionMantenimientos.Services.Cache;
using GestLog.Modules.GestionMantenimientos.ViewModels;
using GestLog.Modules.GestionEquiposInformaticos.Interfaces;
using GestLog.Modules.GestionEquiposInformaticos.Services;

namespace GestLog
{    public static class StartupUsuariosPersonas
    {        
        public static void ConfigureUsuariosPersonasServices(IServiceCollection services, IConfiguration configuration)
        {
            Console.WriteLine("🔧 Configurando servicios de Usuarios y Personas...");        // 🔐 CONFIGURACIÓN SMTP PARA RESETEO DE CONTRASEÑA
            var emailSection = configuration.GetSection(GestLog.Services.Configuration.PasswordResetEmailOptions.SectionName);
            services.Configure<GestLog.Services.Configuration.PasswordResetEmailOptions>(options =>
            {
                emailSection.Bind(options);
            });
            
            // Servicios y repositorios de Usuarios
            services.AddScoped<IUsuarioService, UsuarioService>();
            services.AddScoped<IUsuarioRepository, UsuarioRepository>();
            
            // 🔐 SERVICIOS DE AUTENTICACIÓN
            services.AddScoped<IAuthenticationService, AuthenticationService>();
            services.AddSingleton<ICurrentUserService, CurrentUserService>();
            
            // 🔐 SERVICIOS DE GESTIÓN DE CONTRASEÑA
            services.AddTransient<GestLog.Services.Interfaces.IPasswordResetEmailService, GestLog.Services.Core.PasswordResetEmailService>();
            services.AddScoped<GestLog.Services.Interfaces.IPasswordManagementService, GestLog.Services.Core.PasswordManagementService>();
            
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
            services.AddScoped<IAuditoriaRepository, AuditoriaRepository>();              // ViewModels de Usuarios
            
            services.AddSingleton<global::Modules.Usuarios.ViewModels.UsuarioManagementViewModel>();
            
            services.AddSingleton<RolManagementViewModel>();
            services.AddSingleton<AuditoriaManagementViewModel>();
            services.AddTransient<GestLog.Modules.Usuarios.ViewModels.LoginViewModel>(); // LoginViewModel puede seguir siendo transient ya que se crea por sesión
            services.AddSingleton<GestLog.Modules.Usuarios.ViewModels.IdentidadCatalogosHomeViewModel>();
            services.AddSingleton<CatalogosManagementViewModel>();
            services.AddSingleton<GestionPermisosRolViewModel>();
              // 🔐 ViewModels para gestión de contraseña
            services.AddTransient<GestLog.Modules.Usuarios.ViewModels.ChangePasswordViewModel>(sp =>
            {
                var passwordManagementService = sp.GetRequiredService<GestLog.Services.Interfaces.IPasswordManagementService>();
                var logger = sp.GetRequiredService<IGestLogLogger>();
                var currentUserService = sp.GetRequiredService<GestLog.Modules.Usuarios.Interfaces.ICurrentUserService>();
                return new GestLog.Modules.Usuarios.ViewModels.ChangePasswordViewModel(passwordManagementService, logger, currentUserService);
            });
            services.AddTransient<GestLog.Modules.Usuarios.ViewModels.ForgotPasswordViewModel>(sp =>
            {
                var passwordManagementService = sp.GetRequiredService<GestLog.Services.Interfaces.IPasswordManagementService>();
                var logger = sp.GetRequiredService<IGestLogLogger>();
                return new GestLog.Modules.Usuarios.ViewModels.ForgotPasswordViewModel(passwordManagementService, logger);
            });// Servicios y ViewModels para Gestión de Mantenimientos
            services.AddScoped<IMantenimientoService, MaintenanceService>();
              // Servicios para Gestión de Equipos Informáticos
            services.AddScoped<GestLog.Modules.GestionEquiposInformaticos.Interfaces.IPlanCronogramaService, GestLog.Modules.GestionEquiposInformaticos.Services.PlanCronogramaService>();
            services.AddScoped<GestLog.Modules.GestionEquiposInformaticos.Interfaces.IEquipoInformaticoService, GestLog.Modules.GestionEquiposInformaticos.Services.EquipoInformaticoService>();
            services.AddScoped<GestLog.Modules.GestionEquiposInformaticos.Interfaces.IRegistroMantenimientoEquipoDialogService, GestLog.Modules.GestionEquiposInformaticos.Services.RegistroMantenimientoEquipoDialogService>();
            services.AddScoped<GestLog.Modules.GestionEquiposInformaticos.Interfaces.IRegistroEjecucionPlanDialogService, GestLog.Modules.GestionEquiposInformaticos.Services.RegistroEjecucionPlanDialogService>();
            // Servicio del módulo GestionEquiposInformaticos para desactivar planes y eliminar seguimientos futuros al dar de baja un equipo
            services.AddScoped<GestLog.Modules.GestionEquiposInformaticos.Interfaces.IGestionEquiposInformaticosSeguimientoCronogramaService, GestLog.Modules.GestionEquiposInformaticos.Services.GestionEquiposInformaticosSeguimientoCronogramaService>();
              // Servicios de autocompletado para periféricos
            services.AddScoped<GestLog.Modules.GestionEquiposInformaticos.Services.DispositivoAutocompletadoService>();            services.AddScoped<GestLog.Modules.GestionEquiposInformaticos.Services.MarcaAutocompletadoService>();            // Servicios de autocompletado para Equipos (Clasificacion, CompradoA, Marca)
            services.AddScoped<GestLog.Modules.GestionMantenimientos.Services.Autocomplete.ClasificacionAutocompletadoService>();
            services.AddScoped<GestLog.Modules.GestionMantenimientos.Services.Autocomplete.CompradoAAutocompletadoService>();
            services.AddScoped<GestLog.Modules.GestionMantenimientos.Services.Autocomplete.MarcaAutocompletadoService>();            // Registrar ViewModels: CronogramaDiario como Transient (instancia por vista), el registrador es transient (modal)
            services.AddTransient<GestLog.Modules.GestionEquiposInformaticos.ViewModels.Cronograma.CronogramaDiarioViewModel>(sp =>
            {
                var cronogramaService = sp.GetRequiredService<GestLog.Modules.GestionMantenimientos.Interfaces.Data.ICronogramaService>();
                var planService = sp.GetRequiredService<GestLog.Modules.GestionEquiposInformaticos.Interfaces.IPlanCronogramaService>();
                var equipoService = sp.GetRequiredService<GestLog.Modules.GestionEquiposInformaticos.Interfaces.IEquipoInformaticoService>();
                var usuarioService = sp.GetRequiredService<IUsuarioService>();
                var seguimientoService = sp.GetRequiredService<GestLog.Modules.GestionMantenimientos.Interfaces.Data.ISeguimientoService>();
                var currentUserService = sp.GetRequiredService<GestLog.Modules.Usuarios.Interfaces.ICurrentUserService>();
                var databaseService = sp.GetRequiredService<GestLog.Services.Interfaces.IDatabaseConnectionService>();
                var logger = sp.GetRequiredService<IGestLogLogger>();
                var registroDialogService = sp.GetRequiredService<GestLog.Modules.GestionEquiposInformaticos.Interfaces.IRegistroMantenimientoEquipoDialogService>();
                var registroEjecucionService = sp.GetService<GestLog.Modules.GestionEquiposInformaticos.Interfaces.IRegistroEjecucionPlanDialogService>();
                return new GestLog.Modules.GestionEquiposInformaticos.ViewModels.Cronograma.CronogramaDiarioViewModel(
                    cronogramaService, planService, equipoService, usuarioService, seguimientoService, 
                    currentUserService, databaseService, logger, registroDialogService, registroEjecucionService);
            });
            
            // HistorialEjecucionesViewModel - ✅ ACTUALIZADO: Agregadas dependencias para DatabaseAwareViewModel  
            services.AddTransient<GestLog.Modules.GestionEquiposInformaticos.ViewModels.Mantenimiento.HistorialEjecucionesViewModel>(sp =>
            {
                var planService = sp.GetRequiredService<GestLog.Modules.GestionEquiposInformaticos.Interfaces.IPlanCronogramaService>();
                var equipoService = sp.GetRequiredService<GestLog.Modules.GestionEquiposInformaticos.Interfaces.IEquipoInformaticoService>();
                var databaseService = sp.GetRequiredService<GestLog.Services.Interfaces.IDatabaseConnectionService>();
                var logger = sp.GetRequiredService<IGestLogLogger>();
                return new GestLog.Modules.GestionEquiposInformaticos.ViewModels.Mantenimiento.HistorialEjecucionesViewModel(planService, equipoService, databaseService, logger);
            });
            
            // PerifericosViewModel - ✅ ACTUALIZADO: Agregadas dependencias para DatabaseAwareViewModel
            services.AddTransient<GestLog.Modules.GestionEquiposInformaticos.ViewModels.Perifericos.PerifericosViewModel>(sp =>
            {
                var logger = sp.GetRequiredService<IGestLogLogger>();
                var dbContextFactory = sp.GetRequiredService<IDbContextFactory<GestLogDbContext>>();
                var databaseService = sp.GetRequiredService<GestLog.Services.Interfaces.IDatabaseConnectionService>();
                return new GestLog.Modules.GestionEquiposInformaticos.ViewModels.Perifericos.PerifericosViewModel(logger, dbContextFactory, databaseService);
            });
              // RegistrarMantenimientoViewModel - ✅ ACTUALIZADO: Agregadas dependencias para DatabaseAwareViewModel
            services.AddTransient<GestLog.Modules.GestionMantenimientos.ViewModels.Seguimiento.RegistrarMantenimientoViewModel>(sp =>
            {
                var mantenimientoService = sp.GetRequiredService<IMantenimientoService>();
                var databaseService = sp.GetRequiredService<GestLog.Services.Interfaces.IDatabaseConnectionService>();
                var logger = sp.GetRequiredService<IGestLogLogger>();
                return new GestLog.Modules.GestionMantenimientos.ViewModels.Seguimiento.RegistrarMantenimientoViewModel(mantenimientoService, databaseService, logger);
            });
            // Registrar ViewModel contenedor para GestionEquipos
            services.AddTransient<GestLog.Modules.GestionEquiposInformaticos.ViewModels.Equipos.GestionEquiposHomeViewModel>(sp =>
            {
                var cronogramaVm = sp.GetRequiredService<GestLog.Modules.GestionEquiposInformaticos.ViewModels.Cronograma.CronogramaDiarioViewModel>();
                var historialVm = sp.GetRequiredService<GestLog.Modules.GestionEquiposInformaticos.ViewModels.Mantenimiento.HistorialEjecucionesViewModel>();
                var perifericosVm = sp.GetRequiredService<GestLog.Modules.GestionEquiposInformaticos.ViewModels.Perifericos.PerifericosViewModel>();
                return new GestLog.Modules.GestionEquiposInformaticos.ViewModels.Equipos.GestionEquiposHomeViewModel(cronogramaVm, historialVm, perifericosVm);
            });

            // Servicios y repositorios de Personas
            services.AddScoped<IPersonaService, PersonaService>();
            services.AddScoped<IPersonaRepository, PersonaRepository>(provider =>
            {
                var dbContextFactory = provider.GetRequiredService<IDbContextFactory<GestLogDbContext>>();
                return new PersonaRepository(dbContextFactory);
            });            // Registrar proveedor seguro de configuración de base de datos
            services.AddSingleton<IDatabaseConfigurationProvider, SecureDatabaseConfigurationService>();
            services.AddScoped<ITipoDocumentoRepository, TipoDocumentoRepository>();
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
