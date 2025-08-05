using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using GestLog.Modules.Usuarios.Models;
using GestLog.Services.Core.Logging;
using Microsoft.Extensions.DependencyInjection;
using Modules.Usuarios.Interfaces;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace GestLog.Views.Tools.GestionIdentidadCatalogos.Catalogo.Roles
{
    public partial class CrearRolWindow : Window
    {
        private CrearRolViewModel _viewModel;

        public CrearRolWindow()
        {
            InitializeComponent();
            _viewModel = new CrearRolViewModel();
            DataContext = _viewModel;
            
            // Cargar permisos al inicializar
            Loaded += async (s, e) => await _viewModel.CargarPermisosAsync();
        }

        private async void BtnCrear_Click(object sender, RoutedEventArgs e)
        {
            var resultado = await _viewModel.CrearRolAsync();
            if (resultado)
            {
                DialogResult = true;
                Close();
            }
        }

        private void BtnCancelar_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }

    public class CrearRolViewModel : INotifyPropertyChanged
    {
        private readonly IRolService _rolService;
        private readonly IPermisoService _permisoService;
        private readonly IGestLogLogger _logger;

        private string _nombre = string.Empty;
        public string Nombre
        {
            get => _nombre;
            set { _nombre = value; OnPropertyChanged(); }
        }

        private string _descripcion = string.Empty;
        public string Descripcion
        {
            get => _descripcion;
            set { _descripcion = value; OnPropertyChanged(); }
        }

        private string _mensajeValidacion = string.Empty;
        public string MensajeValidacion
        {
            get => _mensajeValidacion;
            set { _mensajeValidacion = value; OnPropertyChanged(); }
        }

        private ObservableCollection<ModuloPermisosInfo> _permisosPorModulo = new();
        public ObservableCollection<ModuloPermisosInfo> PermisosPorModulo
        {
            get => _permisosPorModulo;
            set { _permisosPorModulo = value; OnPropertyChanged(); }
        }

        public CrearRolViewModel()
        {
            try
            {
                var serviceProvider = LoggingService.GetServiceProvider();
                _rolService = serviceProvider.GetRequiredService<IRolService>();
                _permisoService = serviceProvider.GetRequiredService<IPermisoService>();
                _logger = serviceProvider.GetRequiredService<IGestLogLogger>();            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Error al inicializar servicios: {ex.Message}", "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                throw;
            }
        }

        public async Task CargarPermisosAsync()
        {
            try
            {
                var permisos = await _permisoService.ObtenerTodosAsync();
                
                var grupos = permisos
                    .GroupBy(p => p.Modulo)
                    .Select(g => new ModuloPermisosInfo
                    {
                        Modulo = g.Key,
                        Permisos = new ObservableCollection<PermisoSeleccionInfo>(
                            g.Select(p => new PermisoSeleccionInfo
                            {
                                IdPermiso = p.IdPermiso,
                                Nombre = p.Nombre,
                                Descripcion = p.Descripcion,
                                EstaSeleccionado = false
                            })
                        )
                    })
                    .OrderBy(m => m.Modulo);

                PermisosPorModulo = new ObservableCollection<ModuloPermisosInfo>(grupos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cargando permisos para crear rol");
                MensajeValidacion = "Error al cargar permisos disponibles";
            }
        }

        public async Task<bool> CrearRolAsync()
        {
            MensajeValidacion = string.Empty;

            // Validaciones
            if (string.IsNullOrWhiteSpace(Nombre))
            {
                MensajeValidacion = "El nombre del rol es obligatorio";
                return false;
            }

            try
            {
                // Crear el rol
                var nuevoRol = new Rol
                {
                    Nombre = Nombre.Trim(),
                    Descripcion = Descripcion?.Trim() ?? string.Empty
                };

                var rolCreado = await _rolService.CrearRolAsync(nuevoRol);

                // Asignar permisos seleccionados
                var permisosSeleccionados = PermisosPorModulo
                    .SelectMany(m => m.Permisos)
                    .Where(p => p.EstaSeleccionado)
                    .Select(p => p.IdPermiso)
                    .ToList();

                if (permisosSeleccionados.Any())
                {
                    await _rolService.AsignarPermisosARolAsync(rolCreado.IdRol, permisosSeleccionados);
                }

                _logger.LogInformation($"Rol creado exitosamente: {rolCreado.Nombre} con {permisosSeleccionados.Count} permisos");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al crear rol");
                MensajeValidacion = $"Error al crear rol: {ex.Message}";
                return false;
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        
        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class ModuloPermisosInfo
    {
        public string Modulo { get; set; } = string.Empty;
        public ObservableCollection<PermisoSeleccionInfo> Permisos { get; set; } = new();
    }

    public class PermisoSeleccionInfo : INotifyPropertyChanged
    {
        public Guid IdPermiso { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string Descripcion { get; set; } = string.Empty;

        private bool _estaSeleccionado;
        public bool EstaSeleccionado
        {
            get => _estaSeleccionado;
            set { _estaSeleccionado = value; OnPropertyChanged(); }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        
        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
