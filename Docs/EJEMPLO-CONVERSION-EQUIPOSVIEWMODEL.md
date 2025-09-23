// EJEMPLO: Conversión de EquiposInformaticosViewModel al patrón auto-refresh
// Archivo: ViewModels/Tools/GestionEquipos/EquiposInformaticosViewModel.cs

using System.Collections.ObjectModel;
using GestLog.Modules.GestionEquiposInformaticos.Models.Entities;
using System.Linq;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GestLog.Modules.DatabaseConnection;
using GestLog.Views.Tools.GestionEquipos;
using System.Windows;
using GestLog.Modules.Usuarios.Interfaces;
using GestLog.Modules.Usuarios.Models.Authentication;
using System.ComponentModel;
using System.Windows.Data;
using Microsoft.Win32;
using System.Globalization;
using Microsoft.EntityFrameworkCore;
using GestLog.Services.Equipos;
using GestLog.ViewModels.Base;           // ✅ NUEVO: Clase base
using GestLog.Services.Interfaces;       // ✅ NUEVO: Para IDatabaseConnectionService
using GestLog.Services.Core.Logging;    // ✅ NUEVO: Para IGestLogLogger

namespace GestLog.ViewModels.Tools.GestionEquipos
{
    // ✅ CAMBIO PRINCIPAL: Heredar de DatabaseAwareViewModel en lugar de ObservableObject
    public partial class EquiposInformaticosViewModel : DatabaseAwareViewModel
    {
        // ✅ CAMBIO: Usar IDbContextFactory en lugar de DbContext directo
        private readonly IDbContextFactory<GestLogDbContext> _dbContextFactory;
        private readonly ICurrentUserService _currentUserService;
        private CurrentUserInfo _currentUser;

        public ObservableCollection<EquipoInformaticoEntity> ListaEquiposInformaticos { get; set; } = new();

        [ObservableProperty]
        private bool canCrearEquipo;
        [ObservableProperty]
        private bool canEditarEquipo;
        [ObservableProperty]
        private bool canDarDeBajaEquipo;
        [ObservableProperty]
        private bool canVerHistorial;
        [ObservableProperty]
        private bool canExportarDatos;

        [ObservableProperty]
        private string filtroEquipo = string.Empty;

        [ObservableProperty]
        private ICollectionView? equiposView;

        // ✅ CONSTRUCTOR ACTUALIZADO: Agregar parámetros requeridos y usar base()
        public EquiposInformaticosViewModel(
            IDbContextFactory<GestLogDbContext> dbContextFactory, 
            ICurrentUserService currentUserService,
            IDatabaseConnectionService databaseService,
            IGestLogLogger logger)
            : base(databaseService, logger) // ✅ NUEVO: Llamar constructor base
        {
            _dbContextFactory = dbContextFactory ?? throw new ArgumentNullException(nameof(dbContextFactory));
            _currentUserService = currentUserService;
            _currentUser = _currentUserService.Current ?? new CurrentUserInfo { Username = string.Empty, FullName = string.Empty };
            
            RecalcularPermisos();
            
            // ✅ NUEVO: Inicialización automática (opcional)
            _ = InicializarAsync();
        }

        // ✅ MÉTODO REQUERIDO: Implementar RefreshDataAsync abstracto
        protected override async Task RefreshDataAsync()
        {
            try
            {
                _logger.LogInformation("[EquiposInformaticosViewModel] Refrescando datos automáticamente");
                await CargarEquiposAsync();
                _logger.LogInformation("[EquiposInformaticosViewModel] Datos refrescados exitosamente");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[EquiposInformaticosViewModel] Error al refrescar datos");
                throw; // Re-lanzar para que la clase base maneje el error
            }
        }

        // ✅ OPCIONAL: Personalizar mensaje de pérdida de conexión
        protected override void OnConnectionLost()
        {
            StatusMessage = "Sin conexión - Gestión de equipos no disponible";
        }

        // ✅ NUEVO: Método de inicialización
        public async Task InicializarAsync()
        {
            try
            {
                IsLoading = true;
                StatusMessage = "Cargando equipos...";
                
                await CargarEquiposAsync();
                
                StatusMessage = $"Cargados {ListaEquiposInformaticos.Count} equipos";
            }
            catch (OperationCanceledException) when (/* timeout check */)
            {
                _logger.LogInformation("[EquiposInformaticosViewModel] Timeout - sin conexión BD");
                StatusMessage = "Sin conexión - Módulo no disponible";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[EquiposInformaticosViewModel] Error al inicializar");
                StatusMessage = "Error al cargar equipos";
            }
            finally
            {
                IsLoading = false;
            }
        }

        // ✅ MÉTODO ACTUALIZADO: Usar DbContextFactory con timeout
        public async Task CargarEquiposAsync()
        {
            try
            {
                using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(1));
                using var dbContext = _dbContextFactory.CreateDbContext();
                
                // ✅ TIMEOUT ULTRARRÁPIDO
                dbContext.Database.SetCommandTimeout(1);
                
                var equipos = await dbContext.EquiposInformaticos
                    .AsNoTracking()
                    .OrderBy(e => e.NombreEquipo)
                    .ToListAsync(timeoutCts.Token);

                // Actualizar en hilo UI
                System.Windows.Application.Current?.Dispatcher.Invoke(() =>
                {
                    ListaEquiposInformaticos.Clear();
                    foreach (var equipo in equipos)
                    {
                        ListaEquiposInformaticos.Add(equipo);
                    }
                    
                    // Actualizar vista filtrada
                    EquiposView = CollectionViewSource.GetDefaultView(ListaEquiposInformaticos);
                    AplicarFiltro();
                });
            }
            catch (Microsoft.Data.SqlClient.SqlException ex) when (ex.Number == -1 || ex.Number == 26 || ex.Number == 10060)
            {
                _logger.LogInformation("[EquiposInformaticosViewModel] Sin conexión BD (Error {Number})", ex.Number);
                // No lanzar excepción - manejar silenciosamente
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("[EquiposInformaticosViewModel] Timeout al cargar equipos");
                // No lanzar excepción - manejar silenciosamente  
            }
        }

        // ✅ RESTO DE MÉTODOS SE MANTIENEN IGUAL...
        // Los métodos existentes como RecalcularPermisos(), AplicarFiltro(), etc.
        // se mantienen exactamente igual - solo cambia la infraestructura de datos
        
        private void RecalcularPermisos()
        {
            // Lógica existente sin cambios...
        }

        private void AplicarFiltro()
        {
            // Lógica existente sin cambios...
        }

        // ✅ OPCIONAL: Override Dispose si hay recursos adicionales
        public override void Dispose()
        {
            // Limpiar recursos específicos si los hay
            EquiposView = null;
            
            // Llamar al dispose de la clase base
            base.Dispose();
        }
    }
}

/*
🎯 RESUMEN DE CAMBIOS NECESARIOS:

1. ✅ Herencia: ObservableObject → DatabaseAwareViewModel
2. ✅ Constructor: Agregar IDatabaseConnectionService + IGestLogLogger, usar base()  
3. ✅ DbContext: GestLogDbContext directo → IDbContextFactory<GestLogDbContext>
4. ✅ Implementar: RefreshDataAsync() método abstracto requerido
5. ✅ Opcional: OnConnectionLost() personalizado
6. ✅ Timeout: Aplicar timeout de 1 segundo en consultas
7. ✅ Manejo de errores: Silencioso para errores de conexión conocidos

🚀 BENEFICIOS INMEDIATOS:
- Auto-refresh cuando vuelve la conexión
- Timeout ultrarrápido (1 segundo)  
- Experiencia fluida sin bloqueos
- Código más limpio y mantenible
- Consistencia con otros módulos
*/
