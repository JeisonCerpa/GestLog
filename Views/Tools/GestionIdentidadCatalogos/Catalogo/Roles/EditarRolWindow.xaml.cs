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
    public partial class EditarRolWindow : Window
    {
        private EditarRolViewModel _viewModel;

        public EditarRolWindow(Rol rol)
        {
            InitializeComponent();
            _viewModel = new EditarRolViewModel(rol);
            DataContext = _viewModel;
            
            // Cargar permisos al inicializar
            Loaded += async (s, e) => await _viewModel.CargarPermisosAsync();
        }

        private async void BtnGuardar_Click(object sender, RoutedEventArgs e)
        {
            var resultado = await _viewModel.GuardarCambiosAsync();
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

    public class EditarRolViewModel : INotifyPropertyChanged
    {
        private readonly IRolService _rolService;
        private readonly IPermisoService _permisoService;
        private readonly IGestLogLogger _logger;
        private readonly Rol _rolOriginal;

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

        public EditarRolViewModel(Rol rol)
        {
            _rolOriginal = rol ?? throw new ArgumentNullException(nameof(rol));
            
            // Inicializar propiedades con los datos del rol
            Nombre = rol.Nombre;
            Descripcion = rol.Descripcion;

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
                // Obtener todos los permisos disponibles
                var todosLosPermisos = await _permisoService.ObtenerTodosAsync();
                
                // Obtener permisos ya asignados al rol
                var permisosAsignados = await _rolService.ObtenerPermisosDeRolAsync(_rolOriginal.IdRol);
                var idsPermisosAsignados = permisosAsignados.Select(p => p.IdPermiso).ToHashSet();

                var grupos = todosLosPermisos
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
                                EstaSeleccionado = idsPermisosAsignados.Contains(p.IdPermiso)
                            })
                        )
                    })
                    .OrderBy(m => m.Modulo);

                PermisosPorModulo = new ObservableCollection<ModuloPermisosInfo>(grupos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cargando permisos para editar rol");
                MensajeValidacion = "Error al cargar permisos del rol";
            }
        }

        public async Task<bool> GuardarCambiosAsync()
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
                // Actualizar los datos del rol
                var rolEditado = new Rol
                {
                    IdRol = _rolOriginal.IdRol,
                    Nombre = Nombre.Trim(),
                    Descripcion = Descripcion?.Trim() ?? string.Empty
                };

                await _rolService.EditarRolAsync(rolEditado);

                // Actualizar permisos asignados
                var permisosSeleccionados = PermisosPorModulo
                    .SelectMany(m => m.Permisos)
                    .Where(p => p.EstaSeleccionado)
                    .Select(p => p.IdPermiso)
                    .ToList();

                await _rolService.AsignarPermisosARolAsync(_rolOriginal.IdRol, permisosSeleccionados);

                _logger.LogInformation($"Rol editado exitosamente: {rolEditado.Nombre} con {permisosSeleccionados.Count} permisos");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al editar rol");
                MensajeValidacion = $"Error al editar rol: {ex.Message}";
                return false;
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        
        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
