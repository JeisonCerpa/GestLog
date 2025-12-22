using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GestLog.Modules.DatabaseConnection;
using GestLog.Modules.GestionEquiposInformaticos.Interfaces.Data;
using GestLog.Modules.GestionEquiposInformaticos.Models.Dtos;
using GestLog.Modules.GestionEquiposInformaticos.Models.Enums;
using GestLog.Services.Core.Logging;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace GestLog.Modules.GestionEquiposInformaticos.ViewModels.Mantenimiento
{
    /// <summary>
    /// ViewModel para la ventana de enviar un mantenimiento a reparación
    /// Permite asignar un proveedor y establecer detalles del envío
    /// </summary>
    public partial class EnviarAReparacionViewModel : ObservableObject
    {
        private readonly IMantenimientoCorrectivoService _mantenimientoService;
        private readonly IDbContextFactory<GestLogDbContext> _dbContextFactory;
        private readonly IGestLogLogger _logger;
        
        // Lista completa de proveedores cargada de la BD
        private List<string> _todosLosProveedores = new();

        /// <summary>
        /// Evento que se dispara cuando la operación fue exitosa
        /// </summary>
        public event EventHandler? OnExito;

        /// <summary>
        /// Mantenimiento a enviar a reparación
        /// </summary>
        [ObservableProperty]
        private MantenimientoCorrectivoDto? mantenimiento;        /// <summary>
        /// Proveedor asignado para la reparación
        /// </summary>
        [ObservableProperty]
        private string? proveedorAsignado = string.Empty;        partial void OnProveedorAsignadoChanged(string? value)
        {
            ActualizarProveedoresFiltrados();
        }

        /// <summary>
        /// Lista de proveedores filtrados para el ComboBox
        /// </summary>
        [ObservableProperty]
        private ObservableCollection<string> proveedoresFiltrados = new();

        /// <summary>
        /// Fecha de inicio de la reparación
        /// </summary>
        [ObservableProperty]
        private DateTime? fechaInicio;

        /// <summary>
        /// Observaciones adicionales
        /// </summary>
        [ObservableProperty]
        private string? observaciones = string.Empty;

        /// <summary>
        /// Marcar si el equipo/periférico no es reparable y debe darse de baja
        /// </summary>
        [ObservableProperty]
        private bool incluirDarDeBaja = false;

        /// <summary>
        /// Indica si se está procesando la solicitud
        /// </summary>
        [ObservableProperty]
        private bool isLoading;

        /// <summary>
        /// Mensaje de error para mostrar al usuario
        /// </summary>
        [ObservableProperty]
        private string? errorMessage;        public EnviarAReparacionViewModel(
            IMantenimientoCorrectivoService mantenimientoService,
            IDbContextFactory<GestLogDbContext> dbContextFactory,
            IGestLogLogger logger)
        {
            _mantenimientoService = mantenimientoService ?? throw new ArgumentNullException(nameof(mantenimientoService));
            _dbContextFactory = dbContextFactory ?? throw new ArgumentNullException(nameof(dbContextFactory));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }        /// <summary>
        /// Inicializa el ViewModel con los datos del mantenimiento a enviar
        /// </summary>
        public void InitializarMantenimiento(MantenimientoCorrectivoDto mantenimiento)
        {
            Mantenimiento = mantenimiento;
            ProveedorAsignado = mantenimiento.ProveedorAsignado ?? string.Empty;
            FechaInicio = mantenimiento.FechaInicio ?? DateTime.Now;
            Observaciones = mantenimiento.Observaciones ?? string.Empty;
            ErrorMessage = null;
            
            // Cargar proveedores desde BD
            CargarProveedoresDesdeBaseDatos();
            ActualizarProveedoresFiltrados();
        }

        /// <summary>
        /// Carga los proveedores únicos desde la base de datos
        /// </summary>
        private void CargarProveedoresDesdeBaseDatos()
        {
            try
            {
                using (var context = _dbContextFactory.CreateDbContext())
                {
                    // Obtener proveedores únicos de mantenimientos anteriores
                    var proveedoresDb = context.MantenimientosCorrectivos
                        .AsNoTracking()
                        .Where(m => !string.IsNullOrWhiteSpace(m.ProveedorAsignado))
                        .Select(m => m.ProveedorAsignado!)
                        .Distinct()
                        .OrderBy(p => p)
                        .ToList();

                    _todosLosProveedores = proveedoresDb;
                    
                    if (_todosLosProveedores.Count == 0)
                    {
                        _logger.LogDebug("No hay proveedores en la BD, usando lista vacía");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cargando proveedores desde BD");
                _todosLosProveedores = new List<string>();
            }
        }        /// <summary>
        /// Actualiza la lista de proveedores filtrados según el texto de búsqueda en ProveedorAsignado
        /// </summary>
        private void ActualizarProveedoresFiltrados()
        {
            try
            {
                if (string.IsNullOrWhiteSpace(ProveedorAsignado))
                {
                    ProveedoresFiltrados = new ObservableCollection<string>(_todosLosProveedores);
                }
                else
                {
                    var filtro = ProveedorAsignado.Trim().ToLowerInvariant();
                    var filtrados = _todosLosProveedores
                        .Where(p => p.ToLowerInvariant().Contains(filtro))
                        .ToList();
                    ProveedoresFiltrados = new ObservableCollection<string>(filtrados);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error actualizando proveedores filtrados");
            }
        }        /// <summary>
        /// Envía el mantenimiento a reparación con los datos ingresados
        /// </summary>
        [RelayCommand]
        public async Task EnviarAReparacionAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("🔵 [INICIO] EnviarAReparacionAsync");
                
                // Validar que el proveedor esté informado
                if (string.IsNullOrWhiteSpace(ProveedorAsignado))
                {
                    ErrorMessage = "Debe especificar un proveedor para enviar a reparación";
                    _logger.LogWarning("⚠️ Validación fallida: Proveedor vacío");
                    return;
                }

                if (Mantenimiento?.Id == null)
                {
                    ErrorMessage = "Datos del mantenimiento inválidos";
                    _logger.LogWarning("⚠️ Validación fallida: Mantenimiento ID nulo");
                    return;
                }

                IsLoading = true;
                ErrorMessage = null;                _logger.LogInformation($"📤 Llamando servicio: ID={Mantenimiento.Id}, Proveedor='{ProveedorAsignado}'");

                // Formatear observaciones con viñeta si existen
                var observacionesFormateadas = Observaciones;
                if (!string.IsNullOrWhiteSpace(Observaciones))
                {
                    observacionesFormateadas = "• " + Observaciones;
                }

                _logger.LogInformation($"📝 Observaciones formateadas: {observacionesFormateadas}");

                // Llamar al servicio para actualizar el mantenimiento
                var resultado = await _mantenimientoService.EnviarAReparacionAsync(
                    Mantenimiento.Id.Value,
                    ProveedorAsignado,
                    FechaInicio ?? DateTime.Now,
                    observacionesFormateadas,
                    cancellationToken);

                _logger.LogInformation($"📋 Servicio retornó: resultado={resultado}");

                if (resultado)
                {
                    _logger.LogInformation($"✅ [EXITO] Mantenimiento {Mantenimiento.Id} enviado a reparación");
                    _logger.LogInformation("🔔 Disparando evento OnExito");
                    OnExito?.Invoke(this, EventArgs.Empty);
                    _logger.LogInformation("✅ [FIN] Evento OnExito disparado");
                }                else
                {
                    ErrorMessage = "No fue posible enviar el mantenimiento a reparación. Intente nuevamente.";
                    _logger.LogWarning("❌ El servicio retornó false");
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("⏸️ Operación cancelada");
                ErrorMessage = "Operación cancelada.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ [EXCEPCION] Error en EnviarAReparacionAsync");
                ErrorMessage = $"Error: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
                _logger.LogInformation("🔴 [FIN] EnviarAReparacionAsync - IsLoading=false");
            }
        }

        /// <summary>
        /// Da de baja el mantenimiento directamente sin enviar a reparación
        /// </summary>
        [RelayCommand]
        public async Task DarDeBajaAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("🔵 [INICIO] DarDeBajaAsync");

                if (Mantenimiento?.Id == null)
                {
                    ErrorMessage = "Datos del mantenimiento inválidos";
                    _logger.LogWarning("⚠️ Validación fallida: Mantenimiento ID nulo");
                    return;
                }

                IsLoading = true;
                ErrorMessage = null;

                _logger.LogInformation($"📤 Llamando servicio: ID={Mantenimiento.Id} - Dar de Baja");

                // Formatear razón de baja
                var razonBaja = "⚠️ NO REPARABLE - Equipo/Periférico dado de baja al enviar a reparación";
                if (!string.IsNullOrWhiteSpace(Observaciones))
                {
                    razonBaja += " | " + Observaciones;
                }

                _logger.LogInformation($"📝 Razón de baja: {razonBaja}");

                // Llamar al servicio para dar de baja
                var resultado = await _mantenimientoService.CancelarAsync(
                    Mantenimiento.Id.Value,
                    razonBaja,
                    cancellationToken);

                _logger.LogInformation($"📋 Servicio retornó: resultado={resultado}");

                if (resultado)
                {
                    _logger.LogInformation($"✅ [EXITO] Mantenimiento {Mantenimiento.Id} dado de baja");
                    _logger.LogInformation("🔔 Disparando evento OnExito");
                    OnExito?.Invoke(this, EventArgs.Empty);
                    _logger.LogInformation("✅ [FIN] Evento OnExito disparado");
                }
                else
                {
                    ErrorMessage = "No fue posible dar de baja el mantenimiento. Intente nuevamente.";
                    _logger.LogWarning("❌ El servicio retornó false");
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("⏸️ Operación cancelada");
                ErrorMessage = "Operación cancelada.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ [EXCEPCION] Error en DarDeBajaAsync");
                ErrorMessage = $"Error: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
                _logger.LogInformation("🔴 [FIN] DarDeBajaAsync - IsLoading=false");
            }
        }
    }
}
