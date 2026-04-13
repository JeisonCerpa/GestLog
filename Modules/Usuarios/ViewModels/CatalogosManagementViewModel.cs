using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GestLog.Modules.Usuarios.Models;
using GestLog.ViewModels.Base;
using GestLog.Services.Interfaces;
using GestLog.Services.Core.Logging;
using Modules.Usuarios.Interfaces;
using GestLog.Modules.Usuarios.Views.GestionIdentidadCatalogos;
using System;
using System.Linq;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;

namespace GestLog.Modules.Usuarios.ViewModels
{
    public partial class CatalogosManagementViewModel : DatabaseAwareViewModel
    {
        private readonly ICargoService _cargoService;
        private readonly ITipoDocumentoRepository _tipoDocumentoRepository;

        [ObservableProperty]
        private ObservableCollection<Cargo> cargos;

        [ObservableProperty]
        private Cargo? cargoSeleccionado;

        [ObservableProperty]
        private string mensajeErrorCargo = string.Empty;

        [ObservableProperty]
        private ObservableCollection<TipoDocumento> tiposDocumento;

        [ObservableProperty]
        private TipoDocumento? tipoDocumentoSeleccionado;

        [ObservableProperty]
        private string mensajeErrorTipoDocumento = string.Empty;

        [ObservableProperty]
        private Cargo? cargoEnEdicion;

        [ObservableProperty]
        private bool isModalCargoVisible;

        [ObservableProperty]
        private TipoDocumento? tipoDocumentoEnEdicion;

        [ObservableProperty]
        private bool isModalTipoDocumentoVisible;

        [ObservableProperty]
        private string filtroTipoDocumentoTexto = string.Empty;

        [ObservableProperty]
        private System.ComponentModel.ICollectionView? tiposDocumentoView;

        [ObservableProperty]
        private bool isLoadingTiposDocumento;

        public bool HasVisibleTiposDocumento => TiposDocumentoView?.Cast<object>().Any() ?? TiposDocumento.Any();
        public bool NoTiposDocumentoMessageVisible => !IsLoadingTiposDocumento && !HasVisibleTiposDocumento;

        public CatalogosManagementViewModel(
            ICargoService cargoService,
            ITipoDocumentoRepository tipoDocumentoRepository,
            IDatabaseConnectionService databaseService,
            IGestLogLogger logger)
            : base(databaseService, logger)
        {
            _cargoService = cargoService;
            _tipoDocumentoRepository = tipoDocumentoRepository;

            Cargos = new ObservableCollection<Cargo>();
            TiposDocumento = new ObservableCollection<TipoDocumento>();
            TiposDocumentoView = CollectionViewSource.GetDefaultView(TiposDocumento);
            if (TiposDocumentoView != null)
                TiposDocumentoView.Filter = FiltrarTipoDocumento;
        }

        public async Task InitializeAsync()
        {
            IsLoadingTiposDocumento = true;
            try
            {
                Cargos = new ObservableCollection<Cargo>(await _cargoService.ObtenerTodosAsync());
                TiposDocumento = new ObservableCollection<TipoDocumento>(await _tipoDocumentoRepository.ObtenerTodosAsync());
                ConfigurarVistaTiposDocumento();
            }
            finally
            {
                IsLoadingTiposDocumento = false;
                OnPropertyChanged(nameof(HasVisibleTiposDocumento));
                OnPropertyChanged(nameof(NoTiposDocumentoMessageVisible));
            }
        }

        private void ConfigurarVistaTiposDocumento()
        {
            TiposDocumentoView = CollectionViewSource.GetDefaultView(TiposDocumento);
            if (TiposDocumentoView != null)
                TiposDocumentoView.Filter = FiltrarTipoDocumento;
            TiposDocumentoView?.Refresh();
            OnPropertyChanged(nameof(HasVisibleTiposDocumento));
            OnPropertyChanged(nameof(NoTiposDocumentoMessageVisible));
        }

        private bool FiltrarTipoDocumento(object obj)
        {
            if (obj is not TipoDocumento tipo)
                return false;

            if (string.IsNullOrWhiteSpace(FiltroTipoDocumentoTexto))
                return true;

            var filtro = FiltroTipoDocumentoTexto.Trim().ToLowerInvariant();
            return (tipo.Nombre ?? string.Empty).ToLowerInvariant().Contains(filtro)
                || (tipo.Codigo ?? string.Empty).ToLowerInvariant().Contains(filtro)
                || (tipo.Descripcion ?? string.Empty).ToLowerInvariant().Contains(filtro);
        }

        partial void OnFiltroTipoDocumentoTextoChanged(string value)
        {
            TiposDocumentoView?.Refresh();
            OnPropertyChanged(nameof(HasVisibleTiposDocumento));
            OnPropertyChanged(nameof(NoTiposDocumentoMessageVisible));
        }

        partial void OnTiposDocumentoChanged(ObservableCollection<TipoDocumento> value)
        {
            ConfigurarVistaTiposDocumento();
        }

        [RelayCommand]
        public void LimpiarFiltrosTipoDocumento()
        {
            FiltroTipoDocumentoTexto = string.Empty;
            TiposDocumentoView?.Refresh();
            OnPropertyChanged(nameof(HasVisibleTiposDocumento));
            OnPropertyChanged(nameof(NoTiposDocumentoMessageVisible));
        }

        [RelayCommand]
        public async Task RegistrarCargo()
        {
            MensajeErrorCargo = string.Empty;
            if (CargoSeleccionado == null || string.IsNullOrWhiteSpace(CargoSeleccionado.Nombre))
            {
                MensajeErrorCargo = "El nombre del cargo es obligatorio.";
                return;
            }
            var existe = await _cargoService.ExisteNombreAsync(CargoSeleccionado.Nombre);
            if (existe)
            {
                MensajeErrorCargo = "Ya existe un cargo con ese nombre.";
                return;
            }
            await _cargoService.CrearCargoAsync(CargoSeleccionado);
            Cargos = new ObservableCollection<Cargo>(await _cargoService.ObtenerTodosAsync());
        }

        [RelayCommand]
        public async Task EditarCargo()
        {
            MensajeErrorCargo = string.Empty;
            if (CargoSeleccionado == null || string.IsNullOrWhiteSpace(CargoSeleccionado.Nombre))
            {
                MensajeErrorCargo = "El nombre del cargo es obligatorio.";
                return;
            }
            await _cargoService.EditarCargoAsync(CargoSeleccionado);
            Cargos = new ObservableCollection<Cargo>(await _cargoService.ObtenerTodosAsync());
        }

        [RelayCommand]
        public async Task DesactivarCargo()
        {
            if (CargoSeleccionado == null) return;
            await _cargoService.EliminarCargoAsync(CargoSeleccionado.IdCargo);
            Cargos = new ObservableCollection<Cargo>(await _cargoService.ObtenerTodosAsync());
        }

        [RelayCommand]
        public async Task RegistrarTipoDocumento()
        {
            MensajeErrorTipoDocumento = string.Empty;
            if (TipoDocumentoSeleccionado == null || string.IsNullOrWhiteSpace(TipoDocumentoSeleccionado.Nombre))
            {
                MensajeErrorTipoDocumento = "El nombre del tipo de documento es obligatorio.";
                return;
            }
            await _tipoDocumentoRepository.AgregarAsync(TipoDocumentoSeleccionado);
            TiposDocumento = new ObservableCollection<TipoDocumento>(await _tipoDocumentoRepository.ObtenerTodosAsync());
        }

        [RelayCommand]
        public async Task EditarTipoDocumento()
        {
            MensajeErrorTipoDocumento = string.Empty;
            if (TipoDocumentoSeleccionado == null || string.IsNullOrWhiteSpace(TipoDocumentoSeleccionado.Nombre))
            {
                MensajeErrorTipoDocumento = "El nombre del tipo de documento es obligatorio.";
                return;
            }
            await _tipoDocumentoRepository.ActualizarAsync(TipoDocumentoSeleccionado);
            TiposDocumento = new ObservableCollection<TipoDocumento>(await _tipoDocumentoRepository.ObtenerTodosAsync());
        }

        [RelayCommand]
        public async Task DesactivarTipoDocumento()
        {
            if (TipoDocumentoSeleccionado == null) return;
            await _tipoDocumentoRepository.EliminarAsync(TipoDocumentoSeleccionado.IdTipoDocumento);
            TiposDocumento = new ObservableCollection<TipoDocumento>(await _tipoDocumentoRepository.ObtenerTodosAsync());
        }

        [RelayCommand]
        public void AbrirModalNuevoCargo()
        {
            CargoEnEdicion = new Cargo
            {
                IdCargo = Guid.NewGuid(),
                Nombre = string.Empty,
                Descripcion = string.Empty
            };
            MensajeErrorCargo = string.Empty;
            AbrirModalCargo();
        }

        [RelayCommand]
        public void AbrirModalEditarCargo(Cargo cargo)
        {
            if (cargo == null) return;
            CargoEnEdicion = new Cargo
            {
                IdCargo = cargo.IdCargo,
                Nombre = cargo.Nombre,
                Descripcion = cargo.Descripcion
            };
            MensajeErrorCargo = string.Empty;
            AbrirModalCargo();
        }

        [RelayCommand]
        public void CerrarModalCargo()
        {
            IsModalCargoVisible = false;
            CargoEnEdicion = null;
            MensajeErrorCargo = string.Empty;
            SolicitarCerrarModal?.Invoke();
        }

        public event Action? SolicitarCerrarModal;

        [RelayCommand]
        public async Task GuardarCargo()
        {
            MensajeErrorCargo = string.Empty;
            if (CargoEnEdicion == null || string.IsNullOrWhiteSpace(CargoEnEdicion.Nombre))
            {
                MensajeErrorCargo = "El nombre del cargo es obligatorio.";
                return;
            }

            var esEdicion = Cargos.Any(c => c.IdCargo == CargoEnEdicion.IdCargo);
            if (!esEdicion)
            {
                var existe = await _cargoService.ExisteNombreAsync(CargoEnEdicion.Nombre);
                if (existe)
                {
                    MensajeErrorCargo = "Ya existe un cargo con ese nombre.";
                    return;
                }
                await _cargoService.CrearCargoAsync(CargoEnEdicion);
            }
            else
            {
                await _cargoService.EditarCargoAsync(CargoEnEdicion);
            }

            Cargos = new ObservableCollection<Cargo>(await _cargoService.ObtenerTodosAsync());
            CargoEnEdicion = null;
            SolicitarCerrarModal?.Invoke();
        }

        [RelayCommand]
        public async Task EliminarCargo(Cargo cargo)
        {
            if (cargo == null) return;
            var result = System.Windows.MessageBox.Show(
                $"¿Está seguro que desea eliminar el cargo '{cargo.Nombre}'?",
                "Confirmar eliminación",
                System.Windows.MessageBoxButton.YesNo,
                System.Windows.MessageBoxImage.Warning);
            if (result == System.Windows.MessageBoxResult.Yes)
            {
                await _cargoService.EliminarCargoAsync(cargo.IdCargo);
                Cargos = new ObservableCollection<Cargo>(await _cargoService.ObtenerTodosAsync());
            }
        }

        [RelayCommand]
        public void AbrirModalNuevoTipoDocumento()
        {
            TipoDocumentoEnEdicion = new TipoDocumento
            {
                IdTipoDocumento = Guid.NewGuid(),
                Nombre = string.Empty,
                Codigo = string.Empty,
                Descripcion = string.Empty
            };
            MensajeErrorTipoDocumento = string.Empty;
            AbrirModalTipoDocumento();
        }

        [RelayCommand]
        public void AbrirModalEditarTipoDocumento(TipoDocumento tipo)
        {
            if (tipo == null) return;
            TipoDocumentoEnEdicion = new TipoDocumento
            {
                IdTipoDocumento = tipo.IdTipoDocumento,
                Nombre = tipo.Nombre,
                Codigo = tipo.Codigo,
                Descripcion = tipo.Descripcion
            };
            MensajeErrorTipoDocumento = string.Empty;
            AbrirModalTipoDocumento();
        }

        private void AbrirModalCargo()
        {
            var window = new CargoModalWindow(this)
            {
                Owner = System.Windows.Application.Current.Windows.OfType<Window>().FirstOrDefault(w => w.IsActive) ?? System.Windows.Application.Current.MainWindow
            };

            window.ShowDialog();
        }

        private void AbrirModalTipoDocumento()
        {
            var window = new TipoDocumentoModalWindow(this)
            {
                Owner = System.Windows.Application.Current.Windows.OfType<Window>().FirstOrDefault(w => w.IsActive) ?? System.Windows.Application.Current.MainWindow
            };

            window.ShowDialog();
        }

        [RelayCommand]
        public void CerrarModalTipoDocumento()
        {
            IsModalTipoDocumentoVisible = false;
            TipoDocumentoEnEdicion = null;
            MensajeErrorTipoDocumento = string.Empty;
            SolicitarCerrarModal?.Invoke();
        }

        [RelayCommand]
        public async Task GuardarTipoDocumento()
        {
            MensajeErrorTipoDocumento = string.Empty;
            if (TipoDocumentoEnEdicion == null || string.IsNullOrWhiteSpace(TipoDocumentoEnEdicion.Nombre))
            {
                MensajeErrorTipoDocumento = "El nombre del tipo de documento es obligatorio.";
                return;
            }
            if (string.IsNullOrWhiteSpace(TipoDocumentoEnEdicion.Codigo))
            {
                MensajeErrorTipoDocumento = "El código es obligatorio.";
                return;
            }
            if (string.IsNullOrWhiteSpace(TipoDocumentoEnEdicion.Descripcion))
            {
                MensajeErrorTipoDocumento = "La descripción es obligatoria.";
                return;
            }

            var todos = await _tipoDocumentoRepository.ObtenerTodosAsync();
            var esEdicion = todos.Any(td => td.IdTipoDocumento == TipoDocumentoEnEdicion.IdTipoDocumento);
            if (!esEdicion)
            {
                var existeNombre = todos.Any(td => td.Nombre.Equals(TipoDocumentoEnEdicion.Nombre, StringComparison.OrdinalIgnoreCase));
                var existeCodigo = todos.Any(td => td.Codigo.Equals(TipoDocumentoEnEdicion.Codigo, StringComparison.OrdinalIgnoreCase));
                if (existeNombre)
                {
                    MensajeErrorTipoDocumento = "Ya existe un tipo de documento con ese nombre.";
                    return;
                }
                if (existeCodigo)
                {
                    MensajeErrorTipoDocumento = "Ya existe un tipo de documento con ese código.";
                    return;
                }
                await _tipoDocumentoRepository.AgregarAsync(TipoDocumentoEnEdicion);
            }
            else
            {
                await _tipoDocumentoRepository.ActualizarAsync(TipoDocumentoEnEdicion);
            }

            TiposDocumento = new ObservableCollection<TipoDocumento>(await _tipoDocumentoRepository.ObtenerTodosAsync());
            TipoDocumentoEnEdicion = null;
            SolicitarCerrarModal?.Invoke();
        }

        [RelayCommand]
        public async Task EliminarTipoDocumento(TipoDocumento tipo)
        {
            if (tipo == null) return;
            var result = System.Windows.MessageBox.Show(
                $"¿Está seguro que desea eliminar el tipo de documento '{tipo.Nombre}'?",
                "Confirmar eliminación",
                System.Windows.MessageBoxButton.YesNo,
                System.Windows.MessageBoxImage.Warning);
            if (result == System.Windows.MessageBoxResult.Yes)
            {
                await _tipoDocumentoRepository.EliminarAsync(tipo.IdTipoDocumento);
                TiposDocumento = new ObservableCollection<TipoDocumento>(await _tipoDocumentoRepository.ObtenerTodosAsync());
            }
        }

        protected override async Task RefreshDataAsync()
        {
            try
            {
                _logger.LogDebug("[CatalogosManagementViewModel] Refrescando datos automáticamente");
                await InitializeAsync();
                _logger.LogDebug("[CatalogosManagementViewModel] Datos refrescados exitosamente");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[CatalogosManagementViewModel] Error al refrescar datos");
                throw;
            }
        }

        protected override void OnConnectionLost()
        {
            MensajeErrorCargo = "Sin conexión - Gestión de catálogos no disponible";
            MensajeErrorTipoDocumento = "Sin conexión - Gestión de catálogos no disponible";
        }
    }
}

