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
using GestLog.ViewModels.Base;           // ✅ NUEVO: Clase base auto-refresh
using GestLog.Services.Interfaces;       // ✅ NUEVO: IDatabaseConnectionService
using GestLog.Services.Core.Logging;    // ✅ NUEVO: IGestLogLogger
using System;
using System.Threading.Tasks;
using System.Threading;

namespace GestLog.ViewModels.Tools.GestionEquipos
{
    public partial class EquiposInformaticosViewModel : DatabaseAwareViewModel
    {
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
        private bool showDadoDeBaja = false;

        [ObservableProperty]
        private ICollectionView? equiposView;

        public EquiposInformaticosViewModel(
            IDbContextFactory<GestLogDbContext> dbContextFactory,
            ICurrentUserService currentUserService,
            IDatabaseConnectionService databaseService,
            IGestLogLogger logger)
            : base(databaseService, logger)
        {
            _dbContextFactory = dbContextFactory ?? throw new ArgumentNullException(nameof(dbContextFactory));
            _currentUserService = currentUserService;
            _currentUser = _currentUserService.Current ?? new CurrentUserInfo { Username = string.Empty, FullName = string.Empty };
            
            RecalcularPermisos();
            _currentUserService.CurrentUserChanged += OnCurrentUserChanged;
            EquiposView = CollectionViewSource.GetDefaultView(ListaEquiposInformaticos);
            if (EquiposView != null)
                EquiposView.Filter = new Predicate<object>(FiltrarEquipo);
                
            // Inicialización asíncrona
            _ = InicializarAsync();
        }

        /// <summary>
        /// Implementación del método abstracto para auto-refresh automático
        /// </summary>
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
                throw;
            }
        }

        /// <summary>
        /// Override para manejar cuando se pierde la conexión específicamente para equipos
        /// </summary>
        protected override void OnConnectionLost()
        {
            StatusMessage = "Sin conexión - Gestión de equipos no disponible";
        }

        /// <summary>
        /// Método de inicialización asíncrona con timeout ultrarrápido
        /// </summary>
        public async Task InicializarAsync()
        {
            try
            {
                IsLoading = true;
                StatusMessage = "Cargando equipos...";
                
                await CargarEquiposAsync();
                
                StatusMessage = $"Cargados {ListaEquiposInformaticos.Count} equipos";
            }
            catch (OperationCanceledException)
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

        // Comando público para recargar manualmente la lista de equipos desde la vista
        [RelayCommand]
        public async Task CargarEquipos()
        {
            try
            {
                IsLoading = true;
                await CargarEquiposAsync();
                StatusMessage = $"Cargados {ListaEquiposInformaticos.Count} equipos";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[EquiposInformaticosViewModel] Error al cargar equipos manualmente");
                StatusMessage = "Error al actualizar equipos";
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void OnCurrentUserChanged(object? sender, CurrentUserInfo? user)
        {
            _currentUser = user ?? new CurrentUserInfo { Username = string.Empty, FullName = string.Empty };
            RecalcularPermisos();
        }

        private void RecalcularPermisos()
        {
            CanCrearEquipo = _currentUser.HasPermission("EquiposInformaticos.CrearEquipo");
            CanEditarEquipo = _currentUser.HasPermission("EquiposInformaticos.EditarEquipo");
            CanDarDeBajaEquipo = _currentUser.HasPermission("EquiposInformaticos.DarDeBajaEquipo");
            CanVerHistorial = _currentUser.HasPermission("EquiposInformaticos.VerHistorial");
            CanExportarDatos = _currentUser.HasPermission("EquiposInformaticos.ExportarDatos");
        }

        partial void OnFiltroEquipoChanged(string value)
        {
            System.Windows.Application.Current?.Dispatcher.Invoke(() => EquiposView?.Refresh());
        }

        partial void OnShowDadoDeBajaChanged(bool value)
        {
            System.Windows.Application.Current?.Dispatcher.Invoke(() => EquiposView?.Refresh());
        }

        private bool FiltrarEquipo(object obj)
        {
            if (obj is not EquipoInformaticoEntity eq) return false;
            // Filtrar por estado DadoDeBaja según toggle
            if (!ShowDadoDeBaja && (eq.Estado?.Trim().ToLowerInvariant() == "dadodebaja" || eq.Estado?.Trim().ToLowerInvariant() == "dado de baja"))
                return false;
            if (string.IsNullOrWhiteSpace(FiltroEquipo)) return true;
            
            var terminos = FiltroEquipo.Split(';')
                .Select(t => RemoverTildes(t.Trim()).ToLowerInvariant())
                .Where(t => !string.IsNullOrWhiteSpace(t))
                .ToArray();
                
            var campos = new[]
            {
                RemoverTildes(eq.Codigo ?? "").ToLowerInvariant(),
                RemoverTildes(eq.UsuarioAsignado ?? "").ToLowerInvariant(),
                RemoverTildes(eq.NombreEquipo ?? "").ToLowerInvariant(),
                RemoverTildes(eq.Marca ?? "").ToLowerInvariant(),
                RemoverTildes(eq.Sede ?? "").ToLowerInvariant(),
            };
            
            return terminos.All(term => campos.Any(campo => campo.Contains(term)));
        }

        private string RemoverTildes(string texto)
        {
            return texto
                .Replace("á", "a").Replace("é", "e").Replace("í", "i")
                .Replace("ó", "o").Replace("ú", "u").Replace("ü", "u")
                .Replace("Á", "A").Replace("É", "E").Replace("Í", "I")
                .Replace("Ó", "O").Replace("Ú", "U").Replace("Ü", "U")
                .Replace("ñ", "n").Replace("Ñ", "N");
        }

        /// <summary>
        /// Carga todos los equipos desde la base de datos con timeout ultrarrápido
        /// </summary>
        private async Task CargarEquiposAsync()
        {
            try
            {                using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(15));
                using var dbContext = _dbContextFactory.CreateDbContext();
                
                // ✅ TIMEOUT BALANCEADO: Suficiente tiempo para SSL handshake
                dbContext.Database.SetCommandTimeout(15);
                
                // Usar AsNoTracking para evitar devolver instancias ya rastreadas y con datos stale
                var equipos = await dbContext.EquiposInformaticos
                    .AsNoTracking()
                    .OrderBy(e => e.Codigo)
                    .ToListAsync(timeoutCts.Token);

                // Actualizar en hilo UI
                System.Windows.Application.Current?.Dispatcher.Invoke(() =>
                {
                    ListaEquiposInformaticos.Clear();
                    foreach (var eq in equipos)
                        ListaEquiposInformaticos.Add(eq);

                    EquiposView?.Refresh();
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
            catch (Exception ex)
            {
                _logger.LogError(ex, "[EquiposInformaticosViewModel] Error al cargar equipos");
                // No romper UI si falla la carga
                System.Windows.Application.Current?.Dispatcher.Invoke(() =>
                {
                    System.Windows.MessageBox.Show($"Error al cargar equipos: {ex.Message}", "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                });
            }
        }

        [RelayCommand]
        private async Task VerDetalles(EquipoInformaticoEntity equipo)
        {
            if (equipo == null) return;

            try
            {
                using var dbContext = _dbContextFactory.CreateDbContext();
                dbContext.Database.SetCommandTimeout(15);

                // SIEMPRE recargar desde BD - no confiar en la instancia de la lista que puede estar desactualizada
                var detalle = await dbContext.EquiposInformaticos
                    .AsNoTracking() // Usar AsNoTracking para obtener datos frescos sin tracking
                    .Include(e => e.SlotsRam)
                    .Include(e => e.Discos)
                    .Include(e => e.Conexiones)
                    .FirstOrDefaultAsync(e => e.Codigo == equipo.Codigo);

                if (detalle == null)
                {
                    System.Windows.MessageBox.Show("No se encontró el equipo en la base de datos.", "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                    return;
                }

                // Crear ventana de detalles (necesitaría adaptar para usar factory en lugar de DbContext directo)
                var ventana = new GestLog.Views.Tools.GestionEquipos.DetallesEquipoInformaticoView(detalle, null);
                var owner = System.Windows.Application.Current?.Windows.Count > 0 ? System.Windows.Application.Current.Windows[0] : null;
                if (owner != null) ventana.Owner = owner;
                
                // Mostrar ventana de detalles y esperar a que se cierre
                var result = ventana.ShowDialog();
                
                // CRÍTICO: Después de cerrar detalles, recargar la lista principal para reflejar cambios
                await CargarEquiposAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[EquiposInformaticosViewModel] Error al obtener detalles del equipo");
                System.Windows.MessageBox.Show($"Error al obtener detalles del equipo: {ex.Message}", "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        [RelayCommand(CanExecute = nameof(CanCrearEquipo))]
        private async Task AgregarEquipo()
        {
            var ventana = new AgregarEquipoInformaticoView();
            var resultado = ventana.ShowDialog();
            if (resultado == true)
            {
                // Recargar la lista para mostrar el nuevo equipo
                await CargarEquiposAsync();
            }
        }

        [RelayCommand(CanExecute = nameof(CanExportarDatos))]
        private void ExportarEquipos()
        {
            var dialog = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "Archivos Excel (*.xlsx)|*.xlsx",
                DefaultExt = ".xlsx",
                Title = "Exportar equipos informáticos a Excel",
                FileName = $"EquiposInformaticos_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx"
            };
            
            if (dialog.ShowDialog() == true)
            {
                try
                {
                    // Implementación simplificada de exportación
                    // TODO: Implementar exportación completa si se necesita
                    System.Windows.MessageBox.Show($"Funcionalidad de exportación disponible.\nImplementar según necesidades específicas.", "Exportación", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "[EquiposInformaticosViewModel] Error al exportar");
                    System.Windows.MessageBox.Show($"Error al exportar: {ex.Message}", "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                }
            }
        }

        /// <summary>
        /// Override Dispose si hay recursos adicionales que limpiar
        /// </summary>
        public override void Dispose()
        {
            try
            {
                // Desuscribirse de eventos específicos
                if (_currentUserService != null)
                {
                    _currentUserService.CurrentUserChanged -= OnCurrentUserChanged;
                }
                
                // Limpiar vista
                EquiposView = null;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "[EquiposInformaticosViewModel] Error durante dispose específico");
            }
            finally
            {
                // Llamar al dispose de la clase base
                base.Dispose();
            }
        }
    }
}
