using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GestLog.Modules.GestionEquiposInformaticos.Interfaces.Data;
using GestLog.Modules.GestionEquiposInformaticos.Models.Dtos;
using GestLog.Modules.GestionEquiposInformaticos.Services.Utilities;
using GestLog.Services.Core.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace GestLog.Modules.GestionEquiposInformaticos.ViewModels.Mantenimiento
{
    /// <summary>
    /// ViewModel para la ventana de completar un mantenimiento correctivo
    /// Permite registrar el costo final y observaciones de cierre
    /// </summary>
    public partial class CompletarMantenimientoViewModel : ObservableObject
    {
        private readonly IMantenimientoCorrectivoService _mantenimientoService;
        private readonly IGestLogLogger _logger;

        /// <summary>
        /// Evento que se dispara cuando la operación fue exitosa
        /// </summary>
        public event EventHandler? OnExito;

        /// <summary>
        /// Mantenimiento a completar
        /// </summary>
        [ObservableProperty]
        private MantenimientoCorrectivoDto? mantenimiento;

        /// <summary>
        /// Costo total de la reparación
        /// </summary>
        [ObservableProperty]
        private decimal? costoReparacion;

        /// <summary>
        /// Observaciones adicionales (de cierre)
        /// </summary>
        [ObservableProperty]
        private string? observaciones = string.Empty;

        /// <summary>
        /// Período de garantía en días
        /// </summary>
        [ObservableProperty]
        private int? periodoGarantia;

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
        private string? errorMessage;

        public CompletarMantenimientoViewModel(
            IMantenimientoCorrectivoService mantenimientoService,
            IGestLogLogger logger)
        {
            _mantenimientoService = mantenimientoService ?? throw new ArgumentNullException(nameof(mantenimientoService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }        /// <summary>
        /// Inicializa el ViewModel con los datos del mantenimiento a completar
        /// </summary>
        public void InitializarMantenimiento(MantenimientoCorrectivoDto mantenimiento)
        {
            Mantenimiento = mantenimiento;
            CostoReparacion = mantenimiento.CostoReparacion;
            PeriodoGarantia = mantenimiento.PeriodoGarantia;
            
            // Las observaciones previas se muestran como contexto (solo lectura)
            // El usuario puede agregar nuevas observaciones que se acumularán
            Observaciones = string.Empty;
            ErrorMessage = null;
        }/// <summary>
        /// Completa el mantenimiento correctivo con el costo y observaciones
        /// </summary>
        [RelayCommand]
        public async Task CompletarMantenimientoAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                // Inicio de operación - DEBUG para reducir ruido en logs de entorno normal
                _logger.LogDebug("🔵 [INICIO] CompletarMantenimientoAsync");

                // Validar datos básicos
                if (Mantenimiento?.Id == null)
                {
                    ErrorMessage = "Datos del mantenimiento inválidos";
                    _logger.LogWarning("⚠️ Validación fallida: Mantenimiento ID nulo");
                    return;
                }                IsLoading = true;
                ErrorMessage = null;

                // Llamadas y datos no críticos se registran en DEBUG para evitar spam
                _logger.LogDebug($"📤 Llamando servicio: ID={Mantenimiento.Id}, Costo={CostoReparacion:C}");

                // Acumular observaciones con timestamp
                var observacionesPrevias = Mantenimiento.Observaciones ?? string.Empty;
                var observacionesAcumuladas = ObservacionesConTimestampService.AgregarObservacionCompletado(
                    observacionesPrevias,
                    CostoReparacion,
                    PeriodoGarantia,
                    Observaciones);

                // Solo loggear observaciones si no están vacías y en DEBUG
                if (!string.IsNullOrWhiteSpace(observacionesAcumuladas))
                {
                    _logger.LogDebug("📝 Observaciones con timestamp: {Observaciones}", observacionesAcumuladas);
                }

                // Llamar al servicio para completar el mantenimiento
                var resultado = await _mantenimientoService.CompletarAsync(
                    Mantenimiento.Id.Value,
                    CostoReparacion,
                    observacionesAcumuladas,
                    PeriodoGarantia,
                    cancellationToken);// Resultado del servicio no crítico: Debug. Mantener manejo de error si false.
                _logger.LogDebug($"📋 Servicio retornó: resultado={resultado}");                if (resultado)
                {
                    // Exito: registrar en DEBUG para reducir log spam de operaciones exitosas frecuentes
                    _logger.LogDebug($"✅ [EXITO] Mantenimiento {Mantenimiento.Id} completado");
                    _logger.LogDebug("🔔 Disparando evento OnExito (silencioso)");
                    OnExito?.Invoke(this, EventArgs.Empty);
                    _logger.LogDebug("✅ [FIN] Evento OnExito disparado");
                }
                else
                {
                    ErrorMessage = "No fue posible completar el mantenimiento. Intente nuevamente.";
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
                _logger.LogError(ex, "❌ [EXCEPCION] Error en CompletarMantenimientoAsync");
                ErrorMessage = $"Error: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
                _logger.LogDebug("🔴 [FIN] CompletarMantenimientoAsync - IsLoading=false");
            }
        }

        /// <summary>
        /// Completa el mantenimiento y da de baja el equipo/periférico
        /// </summary>
        [RelayCommand]
        public async Task CompletarYDarDeBajaAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("🔵 [INICIO] CompletarYDarDeBajaAsync");

                // Validar datos básicos
                if (Mantenimiento?.Id == null)
                {
                    ErrorMessage = "Datos del mantenimiento inválidos";
                    _logger.LogWarning("⚠️ Validación fallida: Mantenimiento ID nulo");
                    return;
                }

                IsLoading = true;
                ErrorMessage = null;

                _logger.LogInformation($"📤 Llamando servicio: ID={Mantenimiento.Id} - Dar de Baja");

                // Acumular observaciones con nota de "No reparable"
                var observacionesPrevias = Mantenimiento.Observaciones ?? string.Empty;
                var observacionesAcumuladas = observacionesPrevias;
                var motivoDarDeBaja = "⚠️ NO REPARABLE - Equipo/Periférico dado de baja por no ser reparable";
                
                if (!string.IsNullOrWhiteSpace(Observaciones))
                {
                    motivoDarDeBaja += " | " + Observaciones;
                }

                if (!string.IsNullOrWhiteSpace(observacionesPrevias))
                {
                    observacionesAcumuladas = observacionesPrevias + Environment.NewLine + "• " + motivoDarDeBaja;
                }
                else
                {
                    observacionesAcumuladas = "• " + motivoDarDeBaja;
                }

                _logger.LogInformation($"📝 Observaciones acumuladas: {observacionesAcumuladas}");

                // Dar de baja el mantenimiento
                var resultado = await _mantenimientoService.DarDeBajaAsync(
                    Mantenimiento.Id.Value,
                    cancellationToken);

                _logger.LogInformation($"📋 Servicio DarDeBajaAsync retornó: resultado={resultado}");

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
                _logger.LogError(ex, "❌ [EXCEPCION] Error en CompletarYDarDeBajaAsync");
                ErrorMessage = $"Error: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
                _logger.LogInformation("🔴 [FIN] CompletarYDarDeBajaAsync - IsLoading=false");
            }
        }
    }
}
