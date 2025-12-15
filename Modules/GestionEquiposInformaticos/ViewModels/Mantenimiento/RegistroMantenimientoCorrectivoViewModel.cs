using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GestLog.Modules.GestionEquiposInformaticos.Interfaces.Data;
using GestLog.Modules.GestionEquiposInformaticos.Models.Dtos;
using GestLog.Modules.GestionEquiposInformaticos.Models.Entities;
using GestLog.Modules.Usuarios.Models.Authentication;
using GestLog.Services.Core.Logging;
using GestLog.Modules.DatabaseConnection;
using Microsoft.EntityFrameworkCore;
using System.Collections.ObjectModel;

namespace GestLog.Modules.GestionEquiposInformaticos.ViewModels.Mantenimiento
{    
    /// <summary>
    /// ViewModel para el diálogo de registro de mantenimiento correctivo
    /// </summary>
    public partial class RegistroMantenimientoCorrectivoViewModel : ObservableObject
    {
        // Evento que se dispara cuando se guarda exitosamente
        public event EventHandler? OnRegistroGuardado;

        public event EventHandler? OnMantenimientoRegistrado;

        private readonly IMantenimientoCorrectivoService _service;
        private readonly IEquipoInformaticoService? _equipoService;
        private readonly IDbContextFactory<GestLogDbContext> _dbContextFactory;
        private readonly IGestLogLogger _logger;
        private readonly CurrentUserInfo _currentUser;

        [ObservableProperty]
        private ObservableCollection<EquipoInformaticoEntity> equiposInformaticos = new();

        [ObservableProperty]
        private ObservableCollection<PerifericoEquipoInformaticoDto> perifericos = new();

        [ObservableProperty]
        private EquipoInformaticoEntity? equipoInformaticoSeleccionado;

        [ObservableProperty]
        private PerifericoEquipoInformaticoDto? perifericoSeleccionado;

        [ObservableProperty]
        private bool esEquipoInformatico = true;

        [ObservableProperty]
        private bool esPeriferico = false;

        [ObservableProperty]
        private DateTime fechaFalla = DateTime.Now;

        [ObservableProperty]
        private string horaFalla = DateTime.Now.ToString("HH:mm");

        [ObservableProperty]
        private string descripcionFalla = string.Empty;

        [ObservableProperty]
        private string proveedorAsignado = string.Empty;

        [ObservableProperty]
        private string observaciones = string.Empty;

        [ObservableProperty]
        private bool isLoading = false;

        [ObservableProperty]
        private string? errorMessage;

        public RegistroMantenimientoCorrectivoViewModel(
            IMantenimientoCorrectivoService service,
            IEquipoInformaticoService? equipoService,
            IDbContextFactory<GestLogDbContext> dbContextFactory,
            IGestLogLogger logger,
            CurrentUserInfo currentUser)
        {
            _service = service ?? throw new ArgumentNullException(nameof(service));
            _equipoService = equipoService;
            _dbContextFactory = dbContextFactory ?? throw new ArgumentNullException(nameof(dbContextFactory));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _currentUser = currentUser ?? throw new ArgumentNullException(nameof(currentUser));

            PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(EsEquipoInformatico))
                {
                    EsPeriferico = !EsEquipoInformatico;
                }
                else if (e.PropertyName == nameof(EsPeriferico))
                {
                    EsEquipoInformatico = !EsPeriferico;
                }
            };

            _ = CargarDatosAsync();
        }

        [RelayCommand]
        private async Task CargarDatosAsync()
        {
            try
            {
                IsLoading = true;
                ErrorMessage = null;

                // Cargar equipos
                if (_equipoService != null)
                {
                    var equipos = await _equipoService.GetAllAsync();
                    EquiposInformaticos = new ObservableCollection<EquipoInformaticoEntity>(equipos ?? new List<EquipoInformaticoEntity>());
                }

                // Cargar periféricos
                await CargarPerifericosAsync();
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error al cargar datos: {ex.Message}";
                _logger.LogError(ex, "Error cargando datos en RegistroMantenimientoCorrectivoViewModel");
            }
            finally
            {
                IsLoading = false;
            }
        }        
        private async Task CargarPerifericosAsync()
        {
            try
            {
                using var dbContext = _dbContextFactory.CreateDbContext();
                dbContext.Database.SetCommandTimeout(15);

                var perifericosEntities = await dbContext.PerifericosEquiposInformaticos
                    .Where(p => p.Estado != GestLog.Modules.GestionEquiposInformaticos.Models.Enums.EstadoPeriferico.DadoDeBaja)
                    .ToListAsync();

                var perifericoDtos = perifericosEntities.Select(p => new PerifericoEquipoInformaticoDto
                {
                    Codigo = p.Codigo,
                    Dispositivo = p.Dispositivo,
                    Marca = p.Marca,
                    Modelo = p.Modelo,
                    Serial = p.SerialNumber,
                    Estado = p.Estado,
                    Sede = p.Sede
                }).ToList();

                Perifericos = new ObservableCollection<PerifericoEquipoInformaticoDto>(perifericoDtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cargando periféricos");
                // No interrumpir el flujo si falla la carga de periféricos
            }
        }

        [RelayCommand]
        public async Task GuardarAsync()
        {
            try
            {
                IsLoading = true;
                ErrorMessage = null;

                // Validaciones
                if (EsEquipoInformatico && EquipoInformaticoSeleccionado == null)
                {
                    ErrorMessage = "Seleccione un equipo informático";
                    return;
                }

                if (EsPeriferico && PerifericoSeleccionado == null)
                {
                    ErrorMessage = "Seleccione un periférico";
                    return;
                }

                if (string.IsNullOrWhiteSpace(DescripcionFalla))
                {
                    ErrorMessage = "La descripción de la falla es obligatoria";
                    return;
                }

                if (string.IsNullOrWhiteSpace(ProveedorAsignado))
                {
                    ErrorMessage = "Asigne un proveedor para la reparación";
                    return;
                }

                var mantenimiento = new MantenimientoCorrectivoDto
                {
                    EquipoInformaticoCodigo = EsEquipoInformatico ? EquipoInformaticoSeleccionado?.Codigo : null,
                    PerifericoEquipoInformaticoCodigo = EsPeriferico ? PerifericoSeleccionado?.Codigo : null,
                    FechaFalla = FechaFalla,
                    HoraFalla = HoraFalla,
                    DescripcionFalla = DescripcionFalla,
                    ProveedorAsignado = ProveedorAsignado,
                    Observaciones = string.IsNullOrWhiteSpace(Observaciones) ? null : Observaciones,
                    UsuarioRegistro = _currentUser.Username
                };                
                // Para CrearAsync necesitamos un usuarioRegistroId de tipo int
                // Usaremos 0 como ID temporal, el servicio debe manejarlo
                await _service.CrearAsync(mantenimiento, 0);
                
                LimpiarFormulario();
                OnMantenimientoRegistrado?.Invoke(this, EventArgs.Empty);
                OnRegistroGuardado?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error al guardar: {ex.Message}";
                _logger.LogError(ex, "Error guardando mantenimiento correctivo");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void LimpiarFormulario()
        {
            FechaFalla = DateTime.Now;
            HoraFalla = DateTime.Now.ToString("HH:mm");
            DescripcionFalla = string.Empty;
            ProveedorAsignado = string.Empty;
            Observaciones = string.Empty;
            EquipoInformaticoSeleccionado = null;
            PerifericoSeleccionado = null;
            ErrorMessage = null;
        }

        public override string ToString()
        {
            return $"RegistroMantenimientoCorrectivoViewModel - Equipo: {EquipoInformaticoSeleccionado?.Codigo}, Periférico: {PerifericoSeleccionado?.Codigo}";
        }
    }
}
