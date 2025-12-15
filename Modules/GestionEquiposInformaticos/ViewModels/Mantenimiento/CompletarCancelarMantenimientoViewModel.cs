using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GestLog.Modules.GestionEquiposInformaticos.Interfaces.Data;
using GestLog.Modules.GestionEquiposInformaticos.Models.Dtos;
using GestLog.Modules.GestionEquiposInformaticos.Models.Enums;
using GestLog.Modules.Usuarios.Models.Authentication;
using GestLog.Services.Core.Logging;

namespace GestLog.Modules.GestionEquiposInformaticos.ViewModels.Mantenimiento
{    /// <summary>
    /// ViewModel para el diálogo de completar/cancelar mantenimiento correctivo
    /// </summary>
    public partial class CompletarCancelarMantenimientoViewModel : ObservableObject
    {
        public event EventHandler? OnMantenimientoProcesado;

        private readonly IMantenimientoCorrectivoService _service;
        private readonly IGestLogLogger _logger;
        private readonly CurrentUserInfo _currentUser;

        [ObservableProperty]
        private MantenimientoCorrectivoDto? mantenimientoSeleccionado;

        [ObservableProperty]
        private string? costoReparacion;

        [ObservableProperty]
        private string? observacionesCompletacion = string.Empty;

        [ObservableProperty]
        private bool esCompletar = true;

        [ObservableProperty]
        private bool esCancelar = false;

        [ObservableProperty]
        private bool isLoading = false;

        [ObservableProperty]
        private string? errorMessage;

        public CompletarCancelarMantenimientoViewModel(
            IMantenimientoCorrectivoService service,
            IGestLogLogger logger,
            CurrentUserInfo currentUser)
        {
            _service = service ?? throw new ArgumentNullException(nameof(service));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _currentUser = currentUser ?? throw new ArgumentNullException(nameof(currentUser));

            PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(EsCompletar))
                {
                    EsCancelar = !EsCompletar;
                }
                else if (e.PropertyName == nameof(EsCancelar))
                {
                    EsCompletar = !EsCancelar;
                }
            };
        }

        public void SetMantenimiento(MantenimientoCorrectivoDto mantenimiento)
        {
            MantenimientoSeleccionado = mantenimiento;
            CostoReparacion = string.Empty;
            ObservacionesCompletacion = string.Empty;
            EsCompletar = true;
            EsCancelar = false;
            ErrorMessage = null;
        }

        [RelayCommand]
        public async Task ProcesarAsync()
        {
            try
            {
                IsLoading = true;
                ErrorMessage = null;

                if (MantenimientoSeleccionado == null)
                {
                    ErrorMessage = "No hay mantenimiento seleccionado";
                    return;
                }

                if (EsCompletar)
                {
                    if (string.IsNullOrWhiteSpace(CostoReparacion))
                    {
                        ErrorMessage = "Ingrese el costo de la reparación";
                        return;
                    }

                    await _service.CompletarAsync(
                        MantenimientoSeleccionado.Id ?? 0,
                        ObservacionesCompletacion ?? string.Empty
                    );
                }
                else if (EsCancelar)
                {                    await _service.CancelarAsync(
                        MantenimientoSeleccionado.Id ?? 0,
                        ObservacionesCompletacion ?? string.Empty
                    );
                }

                LimpiarFormulario();
                OnMantenimientoProcesado?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error al procesar: {ex.Message}";
                _logger.LogError(ex, "Error procesando mantenimiento correctivo");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void LimpiarFormulario()
        {
            CostoReparacion = string.Empty;
            ObservacionesCompletacion = string.Empty;
            EsCompletar = true;
            EsCancelar = false;
            ErrorMessage = null;
        }

        public override string ToString()
        {
            return $"CompletarCancelarMantenimientoViewModel - Mantenimiento: {MantenimientoSeleccionado?.Id}";
        }
    }
}
