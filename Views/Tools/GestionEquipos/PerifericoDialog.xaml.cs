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

namespace GestLog.Views.Tools.GestionEquipos
{
    /// <summary>
    /// ViewModel para el diálogo de periféricos con autocompletado basado en datos existentes
    /// </summary>
    public partial class PerifericoDialogViewModel : ObservableObject
    {
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

        public bool DialogResult { get; private set; }        public PerifericoDialogViewModel(
            IDbContextFactory<GestLogDbContext> dbContextFactory,
            DispositivoAutocompletadoService dispositivoService,
            MarcaAutocompletadoService marcaService)
        {
            _dbContextFactory = dbContextFactory ?? throw new ArgumentNullException(nameof(dbContextFactory));
            _dispositivoService = dispositivoService ?? throw new ArgumentNullException(nameof(dispositivoService));
            _marcaService = marcaService ?? throw new ArgumentNullException(nameof(marcaService));
            
            PerifericoActual.FechaCompra = DateTime.Now;
            PerifericoActual.Estado = EstadoPeriferico.EnUso;
            PerifericoActual.Sede = SedePeriferico.AdministrativaBarranquilla;
            PerifericoActual.Costo = 0;
            
            PropertyChanged += OnPropertyChanged;
            
            _ = Task.Run(CargarDatosAutocompletadoAsync);
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
                
                PerifericoActual.UsuarioAsignado = personaSeleccionada.NombreCompleto;
                PerifericoActual.CodigoEquipoAsignado = personaSeleccionada.CodigoEquipo;
                
                System.Windows.Application.Current?.Dispatcher.BeginInvoke(new Action(() =>
                {
                    try
                    {
                        _suppressFiltroUsuarioChanged = true;
                        FiltroUsuarioAsignado = personaSeleccionada.NombreCompleto;
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
                else if (!PersonaConEquipoSeleccionada.NombreCompleto.Equals(texto, StringComparison.OrdinalIgnoreCase))
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

            var encontrada = PersonasConEquipoDisponibles.FirstOrDefault(p =>
                p.NombreCompleto.Equals(nombreCompleto.Trim(), StringComparison.OrdinalIgnoreCase));

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

            _suppressFiltroDispositivoChanged = true;
            _suppressFiltroMarcaChanged = true;
            
            FiltroDispositivo = periferico.Dispositivo ?? string.Empty;
            FiltroMarca = periferico.Marca ?? string.Empty;
            
            _suppressFiltroDispositivoChanged = false;
            _suppressFiltroMarcaChanged = false;

            TituloDialog = "Editar Periférico";
            TextoBotonPrincipal = "Actualizar";
            
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
                var objetivo = NormalizeString(usuarioAsignado);
                var codigoEquipoAsignado = PerifericoActual.CodigoEquipoAsignado;
                
                PersonaConEquipoDto? encontrada = null;
                
                if (!string.IsNullOrWhiteSpace(codigoEquipoAsignado))
                {
                    encontrada = PersonasConEquipoDisponibles.FirstOrDefault(p => 
                        NormalizeString(p.NombreCompleto) == objetivo && 
                        p.CodigoEquipo.Equals(codigoEquipoAsignado, StringComparison.OrdinalIgnoreCase));
                }
                
                if (encontrada == null)
                {
                    encontrada = PersonasConEquipoDisponibles.FirstOrDefault(p => 
                        NormalizeString(p.NombreCompleto) == objetivo);
                }

                if (encontrada != null)
                {
                    PersonaConEquipoSeleccionada = encontrada;
                    FiltroUsuarioAsignado = encontrada.NombreCompleto;
                    PerifericoActual.CodigoEquipoAsignado = encontrada.CodigoEquipo;
                }
                else
                {
                    FiltroUsuarioAsignado = usuarioAsignado.Trim();
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

        private static string NormalizeString(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return string.Empty;
            
            var normalized = input.Normalize(NormalizationForm.FormD);
            var chars = normalized.Where(ch => CharUnicodeInfo.GetUnicodeCategory(ch) != UnicodeCategory.NonSpacingMark).ToArray();
            return new string(chars).Normalize(NormalizationForm.FormC).Trim().ToLowerInvariant();
        }

        [RelayCommand]
        private void Guardar()
        {
            if (PersonaConEquipoSeleccionada != null)
            {
                PerifericoActual.UsuarioAsignado = PersonaConEquipoSeleccionada.NombreCompleto;
                PerifericoActual.CodigoEquipoAsignado = PersonaConEquipoSeleccionada.CodigoEquipo;
            }
            else if (!string.IsNullOrWhiteSpace(FiltroUsuarioAsignado))
            {
                var personaEncontrada = PersonasConEquipoDisponibles?.FirstOrDefault(p => 
                    p.NombreCompleto.Equals(FiltroUsuarioAsignado.Trim(), StringComparison.OrdinalIgnoreCase));

                if (personaEncontrada != null)
                {
                    PerifericoActual.UsuarioAsignado = personaEncontrada.NombreCompleto;
                    PerifericoActual.CodigoEquipoAsignado = personaEncontrada.CodigoEquipo;
                }
                else
                {
                    PerifericoActual.UsuarioAsignado = FiltroUsuarioAsignado.Trim();
                    PerifericoActual.CodigoEquipoAsignado = null;
                }
            }
            else
            {
                PerifericoActual.UsuarioAsignado = null;
                PerifericoActual.CodigoEquipoAsignado = null;
            }

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
    }

    /// <summary>
    /// Code-behind para el diálogo de periféricos
    /// </summary>
    public partial class PerifericoDialog : Window
    {
        public PerifericoDialogViewModel ViewModel { get; }

        public PerifericoDialog(IDbContextFactory<GestLogDbContext> dbContextFactory)
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
            DataContext = ViewModel;

            Loaded += async (s, e) =>
            {
                await ViewModel.CargarPersonasConEquipoAsync();
            };
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
