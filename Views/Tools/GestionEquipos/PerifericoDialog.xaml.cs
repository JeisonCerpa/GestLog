using GestLog.Modules.GestionEquiposInformaticos.Models.Dtos;
using GestLog.Modules.GestionEquiposInformaticos.Models.Enums;
using GestLog.Modules.GestionEquiposInformaticos.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Globalization;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using GestLog.Modules.DatabaseConnection;
using MessageBox = System.Windows.MessageBox;
using GestLog.Services;
using System.Threading;
using CommunityToolkit.Mvvm.Messaging;
using GestLog.Modules.GestionMantenimientos.Messages;
using GestLog.Services.Core.Logging;

#pragma warning disable CS8602 // Desreferencia de referencia posiblemente NULL (silenciado intencionalmente para esta clase de UI)

namespace GestLog.Views.Tools.GestionEquipos
{
    /// <summary>
    /// ViewModel para el diálogo de periféricos con autocompletado basado en datos existentes
    /// </summary>
    public partial class PerifericoDialogViewModel : ObservableObject
    {
        // Helper para obtener logger desde el contenedor DI del App
        private IGestLogLogger? GetLogger() => ((App)System.Windows.Application.Current).ServiceProvider?.GetService<IGestLogLogger>();

        private readonly IDbContextFactory<GestLogDbContext> _dbContextFactory;
        private readonly DispositivoAutocompletadoService _dispositivoService;
        private readonly MarcaAutocompletadoService _marcaService;

        [ObservableProperty]
        private PerifericoEquipoInformaticoDto perifericoActual = new();

        [ObservableProperty]
        private string tituloDialog = "Agregar Periférico";

        [ObservableProperty]
        private string textoBotonPrincipal = "Guardar";

        // Usuario Asignado
        [ObservableProperty]
        private ObservableCollection<PersonaConEquipoDto> personasConEquipoDisponibles = new();

        [ObservableProperty]
        private PersonaConEquipoDto? personaConEquipoSeleccionada;

        [ObservableProperty]
        private string filtroUsuarioAsignado = string.Empty;

        [ObservableProperty]
        private ObservableCollection<PersonaConEquipoDto> personasConEquipoFiltradas = new();

        // Dispositivos con autocompletado
        [ObservableProperty]
        private ObservableCollection<string> dispositivosDisponibles = new();

        [ObservableProperty]
        private string filtroDispositivo = string.Empty;

        [ObservableProperty]
        private ObservableCollection<string> dispositivosFiltrados = new();

        // Marcas con autocompletado
        [ObservableProperty]
        private ObservableCollection<string> marcasDisponibles = new();

        [ObservableProperty]
        private string filtroMarca = string.Empty;

        [ObservableProperty]
        private ObservableCollection<string> marcasFiltradas = new();

        private bool _suppressFiltroUsuarioChanged = false;
        private bool _suppressFiltroDispositivoChanged = false;
        private bool _suppressFiltroMarcaChanged = false;

        // Nuevos campos para debounce/cancelación
        private CancellationTokenSource? _dispositivoFilterCts;
        private CancellationTokenSource? _marcaFilterCts;

        public List<EstadoPeriferico> EstadosDisponibles { get; } = Enum.GetValues<EstadoPeriferico>().ToList();
        public List<SedePeriferico> SedesDisponibles { get; } = Enum.GetValues<SedePeriferico>().ToList();

        public bool DialogResult { get; private set; }        
        
        [ObservableProperty]
        private bool isEditing = false;

        [ObservableProperty]
        private bool isReadOnlyMode = false;

        [ObservableProperty]
        private bool showDeleteButton = false;        public PerifericoDialogViewModel(
            IDbContextFactory<GestLogDbContext> dbContextFactory,
            DispositivoAutocompletadoService dispositivoService,
            MarcaAutocompletadoService marcaService)
        {
            _dbContextFactory = dbContextFactory ?? throw new ArgumentNullException(nameof(dbContextFactory));
            _dispositivoService = dispositivoService ?? throw new ArgumentNullException(nameof(dispositivoService));
            _marcaService = marcaService ?? throw new ArgumentNullException(nameof(marcaService));
            
            PerifericoActual.FechaCompra = DateTime.Now;
            PerifericoActual.Estado = EstadoPeriferico.EnUso;
            PerifericoActual.Costo = 0;
            
            // NO establecer Sede aquí por defecto. Si es nuevo, se asignará en la llamada CreatePerifericoForNew().
            // Si es edición, ConfigurarParaEdicion() lo establecerá correctamente.
            
            PropertyChanged += OnPropertyChanged;
              _ = Task.Run(CargarDatosAutocompletadoAsync);
        }

        /// <summary>
        /// Configura el ViewModel para crear un nuevo periférico con valores por defecto
        /// </summary>
        public void ConfigurarParaNuevo()
        {
            // Asegurar que se tiene un nuevo DTO
            if (PerifericoActual == null)
                PerifericoActual = new PerifericoEquipoInformaticoDto();

            // Establecer Sede por defecto para nuevos
            PerifericoActual.Sede = SedePeriferico.AdministrativaBarranquilla;
            
            TituloDialog = "Agregar Periférico";
            TextoBotonPrincipal = "Guardar";
            IsEditing = false;
        }

        private void OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(FiltroUsuarioAsignado))
                OnFiltroUsuarioAsignadoChanged();
            else if (e.PropertyName == nameof(PersonaConEquipoSeleccionada))
                OnPersonaConEquipoSeleccionadaChanged();
            else if (e.PropertyName == nameof(FiltroDispositivo))
                OnFiltroDispositivoChanged();
            else if (e.PropertyName == nameof(FiltroMarca))
                OnFiltroMarcaChanged();
        }

        private async Task CargarDatosAutocompletadoAsync()
        {
            try
            {
                var dispositivosTask = _dispositivoService.ObtenerTodosAsync();
                var marcasTask = _marcaService.ObtenerTodosAsync();
                
                await Task.WhenAll(dispositivosTask, marcasTask);
                
                var dispositivos = await dispositivosTask;
                var marcas = await marcasTask;
                
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    DispositivosDisponibles.Clear();
                    DispositivosFiltrados.Clear();
                    foreach (var dispositivo in dispositivos)
                    {
                        DispositivosDisponibles.Add(dispositivo);
                        DispositivosFiltrados.Add(dispositivo);
                    }
                    
                    MarcasDisponibles.Clear();
                    MarcasFiltradas.Clear();
                    foreach (var marca in marcas)
                    {
                        MarcasDisponibles.Add(marca);
                        MarcasFiltradas.Add(marca);
                    }
                });
            }
            catch (Exception)
            {
                // Se removió log de depuración verbose. Mantener silencio o manejar errores críticos si aparecen.
            }
        }

        private async void OnFiltroDispositivoChanged()
        {
            if (_suppressFiltroDispositivoChanged) return;

            PerifericoActual.Dispositivo = FiltroDispositivo?.Trim() ?? string.Empty;

            // Cancelar búsqueda anterior y crear una nueva CTS para debounce
            _dispositivoFilterCts?.Cancel();
            _dispositivoFilterCts?.Dispose();
            _dispositivoFilterCts = new CancellationTokenSource();
            var token = _dispositivoFilterCts.Token;

            try
            {
                // Debounce corto para evitar actualizar ItemsSource en cada tecla
                await Task.Delay(250, token);
                if (token.IsCancellationRequested) return;

                await FiltrarDispositivosAsync(token);
            }
            catch (OperationCanceledException)
            {
                // ignore
            }
            catch (Exception)
            {
                // Se removió log de depuración verbose.
            }
        }

        private async void OnFiltroMarcaChanged()
        {
            if (_suppressFiltroMarcaChanged) return;

            PerifericoActual.Marca = FiltroMarca?.Trim() ?? string.Empty;

            // Cancelar búsqueda anterior y crear una nueva CTS para debounce
            _marcaFilterCts?.Cancel();
            _marcaFilterCts?.Dispose();
            _marcaFilterCts = new CancellationTokenSource();
            var token = _marcaFilterCts.Token;

            try
            {
                await Task.Delay(250, token);
                if (token.IsCancellationRequested) return;

                await FiltrarMarcasAsync(token);
            }
            catch (OperationCanceledException)
            {
                // ignore
            }
            catch (Exception)
            {
                // Se removió log de depuración verbose.
            }
        }

        private async Task FiltrarDispositivosAsync(CancellationToken cancellationToken)
        {
            try
            {
                var filtroActual = FiltroDispositivo ?? string.Empty;
                var dispositivosFiltrados = await _dispositivoService.BuscarAsync(filtroActual);

                if (cancellationToken.IsCancellationRequested) return;

                // Preservar el texto actual del filtro para evitar que el ComboBox lo borre al actualizar ItemsSource
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    try
                    {
                        var textoPreservado = FiltroDispositivo ?? string.Empty;

                        DispositivosFiltrados.Clear();
                        foreach (var dispositivo in dispositivosFiltrados)
                        {
                            DispositivosFiltrados.Add(dispositivo);
                        }

                        // Restaurar el texto sin disparar el handler
                        _suppressFiltroDispositivoChanged = true;
                        FiltroDispositivo = textoPreservado;
                        _suppressFiltroDispositivoChanged = false;
                    }
                    catch (Exception)
                    {
                        // Se removió log de depuración verbose.
                    }
                });
            }
            catch (Exception)
            {
                // Se removió log de depuración verbose.
            }
        }

        private async Task FiltrarMarcasAsync(CancellationToken cancellationToken)
        {
            try
            {
                var filtroActual = FiltroMarca ?? string.Empty;
                var marcasFiltradas = await _marcaService.BuscarAsync(filtroActual);

                if (cancellationToken.IsCancellationRequested) return;

                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    try
                    {
                        var textoPreservado = FiltroMarca ?? string.Empty;

                        MarcasFiltradas.Clear();
                        foreach (var marca in marcasFiltradas)
                        {
                            MarcasFiltradas.Add(marca);
                        }

                        _suppressFiltroMarcaChanged = true;
                        FiltroMarca = textoPreservado;
                        _suppressFiltroMarcaChanged = false;
                    }
                    catch (Exception)
                    {
                        // Se removió log de depuración verbose.
                    }
                });
            }
            catch (Exception)
            {
                // Se removió log de depuración verbose.
            }
        }

        private void OnPersonaConEquipoSeleccionadaChanged()
        {
            if (PersonaConEquipoSeleccionada != null)
            {
                var personaSeleccionada = PersonaConEquipoSeleccionada;
                
                // Mantener UsuarioAsignado en el DTO como el nombre (para persistencia), pero mostrar en el TextBox el nombre + equipo
                PerifericoActual.UsuarioAsignado = personaSeleccionada?.NombreCompleto ?? string.Empty;
                PerifericoActual.CodigoEquipoAsignado = personaSeleccionada?.CodigoEquipo ?? string.Empty;
                
                // Mostrar nombre completo + información del equipo en el TextBox (DisplayText)
                System.Windows.Application.Current?.Dispatcher.BeginInvoke(new Action(() =>
                {
                    try
                    {
                        _suppressFiltroUsuarioChanged = true;
                        FiltroUsuarioAsignado = personaSeleccionada?.DisplayText ?? string.Empty;
                        _suppressFiltroUsuarioChanged = false;
                    }
                    catch (Exception)
                    {
                        // Se removió log de depuración verbose.
                    }
                }), System.Windows.Threading.DispatcherPriority.Background);
            }
            else
            {
                if (string.IsNullOrWhiteSpace(FiltroUsuarioAsignado))
                {
                    PerifericoActual.UsuarioAsignado = string.Empty;
                }
                PerifericoActual.CodigoEquipoAsignado = string.Empty;
            }
        }

        private void OnFiltroUsuarioAsignadoChanged()
        {
            if (_suppressFiltroUsuarioChanged) return;

            var texto = FiltroUsuarioAsignado ?? string.Empty;

            if (PersonaConEquipoSeleccionada == null)
            {
                PerifericoActual.UsuarioAsignado = texto;
                
                if (string.IsNullOrWhiteSpace(texto))
                {
                    PerifericoActual.CodigoEquipoAsignado = string.Empty;
                }
                
                SincronizarSeleccionPorNombre(texto);
            }
            else
            {
                if (string.IsNullOrWhiteSpace(texto))
                {
                    PersonaConEquipoSeleccionada = null;
                    PerifericoActual.UsuarioAsignado = "";
                    PerifericoActual.CodigoEquipoAsignado = string.Empty;
                }
                else if (!string.Equals(PersonaConEquipoSeleccionada?.NombreCompleto, texto, StringComparison.OrdinalIgnoreCase))
                {
                    PersonaConEquipoSeleccionada = null;
                    PerifericoActual.UsuarioAsignado = texto;
                    PerifericoActual.CodigoEquipoAsignado = string.Empty;
                    SincronizarSeleccionPorNombre(texto);
                }
            }

            FiltrarPersonasConEquipo();
        }

        private void SincronizarSeleccionPorNombre(string nombreCompleto)
        {
            if (string.IsNullOrWhiteSpace(nombreCompleto)) return;

            var objetivoNorm = NormalizeString(nombreCompleto);
            
            // Intentar emparejar por NombreCompleto o por DisplayText (nombre + equipo)
            var encontrada = PersonasConEquipoDisponibles.FirstOrDefault(p =>
                NormalizeString(p.NombreCompleto ?? string.Empty) == objetivoNorm || NormalizeString(p.DisplayText ?? string.Empty) == objetivoNorm);

            if (encontrada != null)
            {
                PersonaConEquipoSeleccionada = encontrada;
            }
        }

        public void ConfigurarParaEdicion(PerifericoEquipoInformaticoDto periferico)
        {
            PerifericoActual = new PerifericoEquipoInformaticoDto
            {
                Id = periferico.Id,
                Codigo = periferico.Codigo,
                Dispositivo = periferico.Dispositivo,
                FechaCompra = periferico.FechaCompra,
                Costo = periferico.Costo,
                Marca = periferico.Marca,
                Modelo = periferico.Modelo,
                Serial = periferico.Serial,
                UsuarioAsignado = periferico.UsuarioAsignado,
                CodigoEquipoAsignado = periferico.CodigoEquipoAsignado,
                Sede = periferico.Sede,
                Estado = periferico.Estado,
                Observaciones = periferico.Observaciones
            };

            // Log Information: iniciar configuración para edición
            try
            {
                var logger = GetLogger();
                logger?.LogInformation("[PerifericoDialog] ConfigurarParaEdicion: Codigo={Codigo}, UsuarioAsignado={Usuario}, CodigoEquipoAsignado={CodigoEquipo}",
                    new object[] { periferico.Codigo ?? string.Empty, periferico.UsuarioAsignado ?? string.Empty, periferico.CodigoEquipoAsignado ?? string.Empty });
            }
            catch { /* no bloquear UI por logging */ }

            _suppressFiltroDispositivoChanged = true;
            _suppressFiltroMarcaChanged = true;
            
            FiltroDispositivo = periferico.Dispositivo ?? string.Empty;
            FiltroMarca = periferico.Marca ?? string.Empty;
            
            _suppressFiltroDispositivoChanged = false;
            _suppressFiltroMarcaChanged = false;

            TituloDialog = "Editar Periférico";
            TextoBotonPrincipal = "Actualizar";
            IsEditing = true;
            
            // Iniciar búsqueda asíncrona para sincronizar persona/equipo (no bloquear UI)
            _ = Task.Run(async () => await BuscarPersonaConEquipoExistente(periferico.UsuarioAsignado));
        }

        public async Task CargarPersonasConEquipoAsync()
        {
            try
            {
                await using var dbContext = await _dbContextFactory.CreateDbContextAsync();

                var equiposConUsuarios = await dbContext.EquiposInformaticos
                    .Where(e => !string.IsNullOrEmpty(e.UsuarioAsignado) && !string.IsNullOrEmpty(e.NombreEquipo))
                    .Select(e => new { e.UsuarioAsignado, e.Codigo, e.NombreEquipo })
                    .ToListAsync();
                
                var personasActivas = await dbContext.Personas
                    .Where(p => p.Activo && !string.IsNullOrEmpty(p.Nombres) && !string.IsNullOrEmpty(p.Apellidos))
                    .Select(p => new { p.IdPersona, NombreCompleto = (p.Nombres ?? "") + " " + (p.Apellidos ?? "") })
                    .ToListAsync();
                
                var personasConEquipos = new List<PersonaConEquipoDto>();

                foreach (var equipo in equiposConUsuarios)
                {
                    if (string.IsNullOrWhiteSpace(equipo.UsuarioAsignado)) continue;
                    
                    var persona = personasActivas.FirstOrDefault(p => 
                        !string.IsNullOrWhiteSpace(p.NombreCompleto) && 
                        p.NombreCompleto.Trim().Equals(equipo.UsuarioAsignado.Trim(), StringComparison.OrdinalIgnoreCase));
                    
                    if (persona != null)
                    {
                        var dto = new PersonaConEquipoDto
                        {
                            PersonaId = persona.IdPersona,
                            NombreCompleto = persona.NombreCompleto,
                            CodigoEquipo = equipo.Codigo ?? "",
                            NombreEquipo = equipo.NombreEquipo ?? "",
                            TextoNormalizado = NormalizeString($"{persona.NombreCompleto} {equipo.Codigo} {equipo.NombreEquipo}")
                        };
                        personasConEquipos.Add(dto);
                    }
                }

                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    PersonasConEquipoDisponibles.Clear();
                    PersonasConEquipoFiltradas.Clear();
                    
                    foreach (var persona in personasConEquipos.OrderBy(p => p.NombreCompleto))
                    {
                        PersonasConEquipoDisponibles.Add(persona);
                        PersonasConEquipoFiltradas.Add(persona);
                    }
                });
            }
            catch (Exception ex)
            {
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    MessageBox.Show($"Error al cargar usuarios con equipos: {ex.Message}", 
                                  "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                });
            }
        }

        private async Task BuscarPersonaConEquipoExistente(string? usuarioAsignado)
        {
            if (string.IsNullOrWhiteSpace(usuarioAsignado)) return;

            await CargarPersonasConEquipoAsync();

            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                try
                {
                    var logger = GetLogger();
                    logger?.LogInformation("[PerifericoDialog] BuscarPersonaConEquipoExistente iniciado. UsuarioAsignadoOrigen={Usuario}",
                        new object[] { usuarioAsignado ?? string.Empty });
                }
                catch { }

                var objetivo = NormalizeString(usuarioAsignado ?? string.Empty);
                var codigoEquipoAsignado = PerifericoActual.CodigoEquipoAsignado ?? string.Empty;

                PersonaConEquipoDto? encontrada = null;

                if (!string.IsNullOrWhiteSpace(codigoEquipoAsignado))
                {
                    // Intentar emparejar por nombre (normalizado) + código, o por DisplayText + código
                    encontrada = PersonasConEquipoDisponibles.FirstOrDefault(p =>
                        (NormalizeString(p.NombreCompleto ?? string.Empty) == objetivo && (p.CodigoEquipo ?? string.Empty).Equals(codigoEquipoAsignado, StringComparison.OrdinalIgnoreCase))
                        || (NormalizeString(p.DisplayText ?? string.Empty) == objetivo && (p.CodigoEquipo ?? string.Empty).Equals(codigoEquipoAsignado, StringComparison.OrdinalIgnoreCase))
                    );
                }

                if (encontrada == null)
                {
                    // Intentar varias estrategias de coincidencia cuando no se conoce el código: nombre exacto, displayText exacto,
                    // coincidencia por texto normalizado (contiene nombre+código+nombreEquipo) o por contains en nombre completo
                    encontrada = PersonasConEquipoDisponibles.FirstOrDefault(p =>
                        NormalizeString(p.NombreCompleto ?? string.Empty) == objetivo
                        || NormalizeString(p.DisplayText ?? string.Empty) == objetivo
                        || (!string.IsNullOrWhiteSpace(p.TextoNormalizado) && (p.TextoNormalizado ?? string.Empty).Contains(objetivo))
                        || NormalizeString(p.NombreCompleto ?? string.Empty).Contains(objetivo)
                    );
                }

                if (encontrada != null)
                {
                    PersonaConEquipoSeleccionada = encontrada;
                    // Mostrar nombre + equipo en el filtro
                    FiltroUsuarioAsignado = encontrada.DisplayText ?? string.Empty;
                    PerifericoActual.CodigoEquipoAsignado = encontrada.CodigoEquipo ?? string.Empty;

                    try
                    {
                        var logger = GetLogger();
                        logger?.LogInformation("[PerifericoDialog] Persona encontrada en búsqueda. Persona={Persona}, CodigoEquipo={CodigoEquipo}",
                            new object[] { encontrada.DisplayText ?? string.Empty, encontrada.CodigoEquipo ?? string.Empty });
                    }
                    catch { }
                }
                else
                {
                    try
                    {
                        var logger = GetLogger();
                        logger?.LogInformation("[PerifericoDialog] No se encontró persona con equipo para UsuarioAsignado={Usuario}",
                            new object[] { usuarioAsignado ?? string.Empty });
                    }
                    catch { }

                    FiltroUsuarioAsignado = (usuarioAsignado ?? string.Empty).Trim();
                }
            });
        }

        private void FiltrarPersonasConEquipo()
        {
            PersonasConEquipoFiltradas.Clear();

            if (string.IsNullOrWhiteSpace(FiltroUsuarioAsignado))
            {
                foreach (var persona in PersonasConEquipoDisponibles)
                {
                    PersonasConEquipoFiltradas.Add(persona);
                }
                return;
            }

            var filtroNormalizado = NormalizeString(FiltroUsuarioAsignado);
            var personasFiltradas = PersonasConEquipoDisponibles
                .Where(p => p.TextoNormalizado.Contains(filtroNormalizado))
                .ToList();

            foreach (var persona in personasFiltradas)
            {
                PersonasConEquipoFiltradas.Add(persona);
            }
        }

        private static string NormalizeString(string? input)
        {
            if (string.IsNullOrWhiteSpace(input)) return string.Empty;
            
            var normalized = input!.Normalize(NormalizationForm.FormD);
            var chars = normalized.Where(ch => CharUnicodeInfo.GetUnicodeCategory(ch) != UnicodeCategory.NonSpacingMark).ToArray();
            return new string(chars).Normalize(NormalizationForm.FormC).Trim().ToLowerInvariant();
        }

        [RelayCommand]
        private void Guardar()
        {
            // Log: antes de procesar, capturar estado actual del DTO para diagnóstico
            try
            {
                var logger = GetLogger();
                logger?.LogInformation("[PerifericoDialog] Guardar invoked. Periferico.Codigo={Codigo}, UsuarioAsignadoInput={UsuarioInput}, CodigoEquipoAsignadoBeforeAssign={CodigoEquipoBefore}, PersonaSeleccionada={Persona}",
                    new object[] { PerifericoActual?.Codigo ?? string.Empty, FiltroUsuarioAsignado ?? string.Empty, PerifericoActual?.CodigoEquipoAsignado ?? string.Empty, PersonaConEquipoSeleccionada?.DisplayText ?? string.Empty });
            }
            catch { }

            if (PersonaConEquipoSeleccionada != null)
            {
                PerifericoActual.UsuarioAsignado = PersonaConEquipoSeleccionada.NombreCompleto ?? string.Empty;
                PerifericoActual.CodigoEquipoAsignado = PersonaConEquipoSeleccionada.CodigoEquipo ?? string.Empty;
            }
            else if (!string.IsNullOrWhiteSpace(FiltroUsuarioAsignado))
            {
                var textoFiltro = FiltroUsuarioAsignado.Trim();
                var filtroNormalizado = NormalizeString(textoFiltro);

                PersonaConEquipoDto? personaEncontrada = null;

                if (PersonasConEquipoDisponibles != null)
                {
                    personaEncontrada = PersonasConEquipoDisponibles.FirstOrDefault(p =>
                        // Coincidencia exacta por nombre completo
                        p.NombreCompleto.Equals(textoFiltro, StringComparison.OrdinalIgnoreCase)
                        // Coincidencia exacta por DisplayText (p. ej. "Nombre - Equipo")
                        || p.DisplayText.Equals(textoFiltro, StringComparison.OrdinalIgnoreCase)
                        // Coincidencia por normalización (elimina tildes/acentos, minusculas)
                        || NormalizeString(p.NombreCompleto) == filtroNormalizado
                        || NormalizeString(p.DisplayText) == filtroNormalizado
                        // Coincidencia usando el texto normalizado precalculado (contiene nombre+código+nombreEquipo)
                        || (!string.IsNullOrWhiteSpace(p.TextoNormalizado) && p.TextoNormalizado.Contains(filtroNormalizado))
                    );
                }

                if (personaEncontrada != null)
                {
                    PerifericoActual.UsuarioAsignado = personaEncontrada.NombreCompleto ?? string.Empty;
                    PerifericoActual.CodigoEquipoAsignado = personaEncontrada.CodigoEquipo ?? string.Empty;
                }
                else
                {
                    PerifericoActual.UsuarioAsignado = textoFiltro ?? string.Empty;
                    PerifericoActual.CodigoEquipoAsignado = string.Empty;
                }
            }
            else
            {
                PerifericoActual.UsuarioAsignado = string.Empty;
                PerifericoActual.CodigoEquipoAsignado = string.Empty;
            }

            // Log: estado final del DTO justo antes de validar/aceptar
            try
            {
                var logger = GetLogger();
                logger?.LogInformation("[PerifericoDialog] Guardar - estado final Periferico: Codigo={Codigo}, UsuarioAsignado={Usuario}, CodigoEquipoAsignado={CodigoEquipo}",
                    new object[] { PerifericoActual?.Codigo ?? string.Empty, PerifericoActual?.UsuarioAsignado ?? string.Empty, PerifericoActual?.CodigoEquipoAsignado ?? string.Empty });
            }
            catch { }

            // --- NUEVO: Resolver Dispositivo y Marca robustamente (soporta SelectedItem en ComboBox o texto libre)
            try
            {
                if (!string.IsNullOrWhiteSpace(FiltroDispositivo))
                {
                    var textoDisp = FiltroDispositivo.Trim();

                    // Intentar buscar coincidencia en la lista filtrada (si existe)
                    object? match = null;
                    if (DispositivosFiltrados != null)
                    {
                        match = DispositivosFiltrados.FirstOrDefault(d =>
                        {
                            var s = d?.ToString();
                            return !string.IsNullOrWhiteSpace(s) && string.Equals(s, textoDisp, StringComparison.OrdinalIgnoreCase);
                        });
                    }

                    // Asignar valor seguro (no null)
                    PerifericoActual.Dispositivo = (match?.ToString() ?? textoDisp) ?? string.Empty;
                }
                else
                {
                    PerifericoActual.Dispositivo = string.Empty;
                }

                if (!string.IsNullOrWhiteSpace(FiltroMarca))
                {
                    var textoMarca = FiltroMarca.Trim();

                    object? matchMarca = null;
                    if (MarcasFiltradas != null)
                    {
                        matchMarca = MarcasFiltradas.FirstOrDefault(m =>
                        {
                            var s = m?.ToString();
                            return !string.IsNullOrWhiteSpace(s) && string.Equals(s, textoMarca, StringComparison.OrdinalIgnoreCase);
                        });
                    }

                    PerifericoActual.Marca = (matchMarca?.ToString() ?? textoMarca) ?? string.Empty;
                }
                else
                {
                    PerifericoActual.Marca = string.Empty;
                }
            }
            catch (Exception)
            {
                // No bloquear guardado por un problema menor de resolución; se removió log de depuración verbose.
            }

            if (ValidarFormulario())
            {
                DialogResult = true;

                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    if (System.Windows.Application.Current.Windows.Cast<Window>()
                        .FirstOrDefault(w => w.DataContext == this) is PerifericoDialog dialog)
                    {
                        dialog.DialogResult = true;
                        dialog.Close();
                    }
                });
            }
        }
        
        private bool ValidarFormulario()
        {
            var errores = new List<string>();

            if (string.IsNullOrWhiteSpace(PerifericoActual.Codigo))
                errores.Add("El código es obligatorio");

            if (string.IsNullOrWhiteSpace(PerifericoActual.Dispositivo))
                errores.Add("El dispositivo es obligatorio");

            if (PerifericoActual.FechaCompra == default)
                errores.Add("La fecha de compra es obligatoria");

            if (PerifericoActual.Costo < 0)
                errores.Add("El costo no puede ser negativo");

            if (errores.Any())
            {
                var mensaje = "Por favor corrija los siguientes errores:\n\n" + string.Join("\n", errores);
                MessageBox.Show(mensaje, "Errores de Validación", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            return true;
        }

        [RelayCommand]
        private async Task EliminarAsync()
        {
            if (string.IsNullOrWhiteSpace(PerifericoActual?.Codigo)) return;

            var resultado = MessageBox.Show(
                $"¿Está seguro de que desea eliminar el periférico '{PerifericoActual.Codigo}'?",
                "Confirmar eliminación",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (resultado != MessageBoxResult.Yes) return;

            try
            {
                await using var dbContext = await _dbContextFactory.CreateDbContextAsync();

                var entity = await dbContext.PerifericosEquiposInformaticos
                    .FirstOrDefaultAsync(p => p.Codigo == PerifericoActual.Codigo);

                if (entity != null)
                {
                    dbContext.PerifericosEquiposInformaticos.Remove(entity);
                    await dbContext.SaveChangesAsync();

                    // Enviar mensaje para refrescar listas
                    WeakReferenceMessenger.Default.Send(new PerifericosActualizadosMessage());

                    // Cerrar diálogo: no marcar DialogResult = true para evitar que el caller intente guardar
                    System.Windows.Application.Current.Dispatcher.Invoke(() =>
                    {
                        if (System.Windows.Application.Current.Windows.Cast<Window>()
                            .FirstOrDefault(w => w.DataContext == this) is PerifericoDialog dialog)
                        {
                            // Cerrar sin establecer DialogResult -> ShowDialog() retornará false y el padre no intentará guardar
                            dialog.Close();
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al eliminar el periférico: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        [RelayCommand(CanExecute = nameof(CanStartEdit))]
        private void Editar()
        {
            // Cambiar a modo edición
            IsReadOnlyMode = false;
            IsEditing = true;
            TituloDialog = "Editar Periférico";
            TextoBotonPrincipal = "Actualizar";
            ShowDeleteButton = true;
        }

        private bool CanStartEdit()
        {
            return IsReadOnlyMode;
        }
    }

    /// <summary>
    /// Code-behind para el diálogo de periféricos
    /// </summary>
    public partial class PerifericoDialog : Window
    {
        public PerifericoDialogViewModel ViewModel { get; }        public PerifericoDialog(IDbContextFactory<GestLogDbContext> dbContextFactory)
        {
            InitializeComponent();

            // Obtener servicios del contenedor DI
            var dispositivoService = ((App)System.Windows.Application.Current).ServiceProvider?.GetService<DispositivoAutocompletadoService>();
            var marcaService = ((App)System.Windows.Application.Current).ServiceProvider?.GetService<MarcaAutocompletadoService>();

            // Fallback en caso de que no estén registrados
            if (dispositivoService == null)
                dispositivoService = new DispositivoAutocompletadoService(dbContextFactory);
            if (marcaService == null)
                marcaService = new MarcaAutocompletadoService(dbContextFactory);

            ViewModel = new PerifericoDialogViewModel(dbContextFactory, dispositivoService, marcaService);
            // Configurar para nuevo periférico por defecto
            ViewModel.ConfigurarParaNuevo();
            DataContext = ViewModel;

            Loaded += async (s, e) =>
            {
                await ViewModel.CargarPersonasConEquipoAsync();
            };
        }// Helper para asignar Owner sin forzar tamaño (PerifericoDialog es un dialog normal, no overlay modal)
        public void ConfigurarParaVentanaPadre(System.Windows.Window? parentWindow)
        {
            if (parentWindow != null)
            {
                Owner = parentWindow;
                ShowInTaskbar = false;
                // PerifericoDialog respeta sus propios tamaños (Height="700" Width="900")
                // No forzar WindowState.Maximized ya que no es un overlay modal
            }
        }

        public PerifericoDialog(PerifericoEquipoInformaticoDto perifericoParaEditar, IDbContextFactory<GestLogDbContext> dbContextFactory) : this(dbContextFactory)
        {
            ViewModel.ConfigurarParaEdicion(perifericoParaEditar);
        }

        private void BtnCancelar_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        // BtnGuardar_Click eliminado: uso exclusivo de GuardarCommand desde XAML

        
        protected override void OnClosing(CancelEventArgs e)
        {
            if (ViewModel.DialogResult)
            {
                DialogResult = true;
            }
            base.OnClosing(e);
        }
    }
}
