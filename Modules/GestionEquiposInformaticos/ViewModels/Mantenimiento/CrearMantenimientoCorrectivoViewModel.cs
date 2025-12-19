using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using GestLog.Modules.GestionEquiposInformaticos.Interfaces.Data;
using GestLog.Modules.GestionEquiposInformaticos.Models.Dtos;
using GestLog.Modules.GestionEquiposInformaticos.Messages;
using GestLog.Services.Core.Logging;
using GestLog.Modules.Usuarios.Interfaces;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Threading;
using GestLog.Modules.GestionEquiposInformaticos.Interfaces.Autocomplete;
using System.Linq;

namespace GestLog.Modules.GestionEquiposInformaticos.ViewModels.Mantenimiento
{    public partial class CrearMantenimientoCorrectivoViewModel : ObservableObject
    {
        private readonly IMantenimientoCorrectivoService _mantenimientoService;
        private readonly IGestLogLogger _logger;
        private readonly ICurrentUserService _currentUserService;
        private readonly IDispositivoAutocompletadoService _dispositivoAutocompletadoService;
        private readonly IEquipoInformaticoService _equipoInformaticoService;
        private readonly IMessenger _messenger;

        // Debounce para autocompletado de código
        private CancellationTokenSource? _codigoDebounceCts;
        private const int CodigoDebounceMs = 300;
        // Bandera para suprimir reacciones al restaurar el texto del código
        private bool _suppressCodigoChanged = false;

        public event EventHandler? OnExito;

        protected virtual void AlGuardarExitoso()
        {
            OnExito?.Invoke(this, EventArgs.Empty);
        }

        [ObservableProperty]
        private ObservableCollection<string> entidadTipos = new(new[] { "Equipo", "Periférico" });

        [ObservableProperty]
        private string tipoEntidad = "Equipo";

        [ObservableProperty]
        private string codigoEntidad = string.Empty;

        partial void OnCodigoEntidadChanged(string value)
        {
            // Si estamos restaurando el texto manualmente, no disparamos la búsqueda
            if (_suppressCodigoChanged) return;

            // Lanzar búsqueda con debounce para autocompletado
            _ = DebounceBuscarCodigosAsync(value);
        }

        // Tipo simple para representar una sugerencia (código + nombre de equipo/periférico)
        public record CodigoSuggestion(string Code, string Name)
        {
            // Cuando el ComboBox intenta mostrar el SelectedItem en el TextBox, evita el ToString por defecto
            // que muestra 'CodigoSuggestion { Code = ..., Name = ... }'. Devolvemos sólo el código para que
            // el texto editable muestre el código seleccionado.
            public override string ToString() => Code ?? string.Empty;
        } 

        // Colección disponible (todas las sugerencias obtenidas de servicio)
        private ObservableCollection<CodigoSuggestion> codigoSugerenciasDisponibles = new();

        // Colección filtrada enlazada al ComboBox (ItemsSource)
        [ObservableProperty]
        private ObservableCollection<CodigoSuggestion> codigoSugerencias = new();

        [ObservableProperty]
        private CodigoSuggestion? selectedCodigoSuggestion;

        partial void OnSelectedCodigoSuggestionChanged(CodigoSuggestion? value)
        {
            if (value == null) return;

            try
            {
                _suppressCodigoChanged = true;
                CodigoEntidad = value.Code ?? string.Empty;
            }
            finally
            {
                _suppressCodigoChanged = false;
            }
        }

        [ObservableProperty]
        private DateTime fechaFalla = DateTime.Now;

        [ObservableProperty]
        private string descripcionFalla = string.Empty;

        [ObservableProperty]
        private bool canSave;        public CrearMantenimientoCorrectivoViewModel(IMantenimientoCorrectivoService mantenimientoService, IGestLogLogger logger, ICurrentUserService currentUserService, IDispositivoAutocompletadoService dispositivoAutocompletadoService, IEquipoInformaticoService equipoInformaticoService, IMessenger messenger)
        {
            _mantenimientoService = mantenimientoService;
            _logger = logger;
            _currentUserService = currentUserService;
            _dispositivoAutocompletadoService = dispositivoAutocompletadoService;
            _equipoInformaticoService = equipoInformaticoService;
            _messenger = messenger;

            // Re-evaluar si se puede guardar al cambiar campos críticos
            PropertyChanged += (s, e) =>
            {
                CanSave = !string.IsNullOrWhiteSpace(CodigoEntidad) && !string.IsNullOrWhiteSpace(DescripcionFalla);
            };            // Inicializar sugerencias según el tipo por defecto
            _ = LoadInitialCodigoSuggestionsAsync();
        }

        partial void OnTipoEntidadChanged(string value)
        {
            // Limpiar código y recargar sugerencias al cambiar tipo
            CodigoEntidad = string.Empty;
            _ = LoadInitialCodigoSuggestionsAsync();
        }

        private async Task LoadInitialCodigoSuggestionsAsync()
        {
            try
            {
                // Preservar el texto actual
                var textoPreservado = CodigoEntidad;

                // Cargar todas las sugerencias disponibles en la colección de disponibles
                codigoSugerenciasDisponibles.Clear();
                if (TipoEntidad == "Periférico")
                {
                    var items = await _dispositivoAutocompletadoService.BuscarConCodigoAsync(string.Empty);
                    foreach (var it in items)
                        codigoSugerenciasDisponibles.Add(new CodigoSuggestion(it.Code, it.Dispositivo));
                }
                else
                {
                    var equipos = await _equipoInformaticoService.GetAllAsync();
                    foreach (var e in equipos)
                    {
                        if (!string.IsNullOrWhiteSpace(e.Codigo))
                            codigoSugerenciasDisponibles.Add(new CodigoSuggestion(e.Codigo, e.NombreEquipo ?? string.Empty));
                    }
                }

                // Aplicar filtro inicial (vacío -> mostrar todos) a la colección enlazada al ComboBox
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    try
                    {
                        CodigoSugerencias.Clear();
                        foreach (var it in codigoSugerenciasDisponibles)
                            CodigoSugerencias.Add(it);

                        // Restaurar texto sin disparar búsqueda
                        _suppressCodigoChanged = true;
                        CodigoEntidad = textoPreservado;

                        // Si el texto coincide exactamente con una sugerencia, seleccionarla
                        SelectedCodigoSuggestion = CodigoSugerencias.FirstOrDefault(s => s.Code == CodigoEntidad);
                    }
                    finally { _suppressCodigoChanged = false; }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cargando sugerencias de código");
            }
        }

        private async Task DebounceBuscarCodigosAsync(string filtro)
        {
            try
            {
                _codigoDebounceCts?.Cancel();
                _codigoDebounceCts?.Dispose();
                _codigoDebounceCts = new CancellationTokenSource();
                var token = _codigoDebounceCts.Token;
                await Task.Delay(CodigoDebounceMs, token);
                await BuscarCodigosAsync(filtro, token);
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en debounce de búsqueda de códigos");
            }
        }

        private async Task BuscarCodigosAsync(string filtro, CancellationToken cancellationToken = default)
        {
            try
            {
                var textoPreservado = CodigoEntidad;

                if (TipoEntidad == "Periférico")
                {
                    var resultados = await _dispositivoAutocompletadoService.BuscarConCodigoAsync(filtro ?? string.Empty);
                    // Reemplazar disponibles por los resultados
                    codigoSugerenciasDisponibles.Clear();
                    foreach (var r in resultados)
                        codigoSugerenciasDisponibles.Add(new CodigoSuggestion(r.Code, r.Dispositivo));
                }
                else
                {
                    // Para equipos mantenemos la lista de disponibles (cargada inicialmente) y filtramos localmente
                    var term = (filtro ?? string.Empty).Trim().ToLowerInvariant();
                    var filtered = string.IsNullOrWhiteSpace(term)
                        ? codigoSugerenciasDisponibles.ToList()
                        : codigoSugerenciasDisponibles.Where(e => !string.IsNullOrWhiteSpace(e.Code) && e.Code.ToLowerInvariant().Contains(term)).ToList();

                    // Actualizar la colección enlazada al ComboBox con los filtrados
                    System.Windows.Application.Current.Dispatcher.Invoke(() =>
                    {
                        CodigoSugerencias.Clear();
                        foreach (var it in filtered) CodigoSugerencias.Add(it);
                    });
                }

                // Si el tipo es Periférico actualizamos la colección enlazada también
                if (TipoEntidad == "Periférico")
                {
                    System.Windows.Application.Current.Dispatcher.Invoke(() =>
                    {
                        CodigoSugerencias.Clear();
                        foreach (var it in codigoSugerenciasDisponibles) CodigoSugerencias.Add(it);
                    });
                }

                // Restaurar el texto preservado sin disparar el debounce y seleccionar sugerencia exacta si existe
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    try
                    {
                        _suppressCodigoChanged = true;
                        CodigoEntidad = textoPreservado;
                        SelectedCodigoSuggestion = CodigoSugerencias.FirstOrDefault(s => s.Code == CodigoEntidad);
                    }
                    finally { _suppressCodigoChanged = false; }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error buscando códigos");
            }
        }

        [RelayCommand]
        public async Task SaveAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                CanSave = false;                
                // Preferir la sugerencia seleccionada (si existe) para obtener el código exacto
                var codigoSeleccionado = SelectedCodigoSuggestion?.Code ?? CodigoEntidad?.Trim();            var dto = new MantenimientoCorrectivoDto
                {
                    TipoEntidad = TipoEntidad,
                    Codigo = codigoSeleccionado,
                    ProveedorAsignado = null, // Se asigna al transicionar a "En Reparación"
                    FechaFalla = FechaFalla,
                    CostoReparacion = null,
                    DescripcionFalla = DescripcionFalla,
                    Observaciones = null
                };

                // Intentar obtener el ID numérico del usuario que registra (si está disponible)
                int usuarioRegistroId = 0; // valor por defecto cuando no hay mapeo directo a int
                try
                {
                    var current = _currentUserService?.Current;
                    if (current != null)
                    {
                        // El sistema usa Guid para identidad de usuario; la tabla de mantenimientos espera un int.
                        // Como aproximación reproducible (temporal), convertimos los primeros 4 bytes del GUID a int.
                        var guidBytes = current.UserId.ToByteArray();
                        usuarioRegistroId = BitConverter.ToInt32(guidBytes, 0);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogDebug("No fue posible mapear usuario actual a Id numérico, usando 0: {Message}", ex.Message);
                    usuarioRegistroId = 0;
                }                var resultId = await _mantenimientoService.CrearAsync(dto, usuarioRegistroId, cancellationToken);
                if (resultId <= 0) throw new Exception("No fue posible crear el mantenimiento");                // Close window should be handled by view via message or dialog result; we can expose success flag
                _logger.LogDebug("Mantenimiento creado correctamente");
                
                // Enviar mensaje para notificar a otros ViewModels que refresquen
                _messenger.Send(new MantenimientosCorrectivosActualizadosMessage(null));
                
                // Disparar evento de éxito para que la ventana modal se cierre
                AlGuardarExitoso();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creando mantenimiento");
                throw;
            }
            finally
            {
                CanSave = true;
            }
        }

        [RelayCommand]
        public void Cancel()
        {
            // View should close on command; expose action if needed
        }
    }
}
