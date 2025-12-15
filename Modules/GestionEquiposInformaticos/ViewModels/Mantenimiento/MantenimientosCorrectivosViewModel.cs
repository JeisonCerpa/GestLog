using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using GestLog.Modules.GestionEquiposInformaticos.Interfaces.Data;
using GestLog.Modules.GestionEquiposInformaticos.Messages;
using GestLog.Modules.GestionEquiposInformaticos.Models.Dtos;
using GestLog.Modules.GestionEquiposInformaticos.Models.Enums;
using GestLog.Modules.Usuarios.Models.Authentication;
using GestLog.Services.Core.Logging;
using System.Collections.ObjectModel;
using System.Windows;
using GestLog.Modules.GestionEquiposInformaticos.Views.Mantenimiento;
using WPFApplication = System.Windows.Application;

namespace GestLog.Modules.GestionEquiposInformaticos.ViewModels.Mantenimiento
{
    /// <summary>
    /// ViewModel para gestionar mantenimientos correctivos (reactivos) de equipos e periféricos
    /// </summary>
    public partial class MantenimientosCorrectivosViewModel : ObservableObject
    {
        private readonly IMantenimientoCorrectivoService _service;
        private readonly IGestLogLogger _logger;
        private readonly CurrentUserInfo _currentUser;

        [ObservableProperty]
        private ObservableCollection<MantenimientoCorrectivoDto> mantenimientosCorrectivos = new();

        [ObservableProperty]
        private MantenimientoCorrectivoDto? selectedMantenimiento;

        [ObservableProperty]
        private bool isLoading = false;

        [ObservableProperty]
        private string? errorMessage;

        [ObservableProperty]
        private string? successMessage;

        [ObservableProperty]
        private bool canAccederGestionEquiposInformaticos;

        [ObservableProperty]
        private bool canEliminarGestionEquiposInformaticos;

        [ObservableProperty]
        private EstadoMantenimientoCorrectivo? filtroEstado;

        [ObservableProperty]
        private bool incluirDadosDeBaja = false;

        [ObservableProperty]
        private string filtroEstadoSeleccionado = "Todos";

        [ObservableProperty]
        private string estadisticasText = "No hay mantenimientos";

        [ObservableProperty]
        private bool isEmpty = true;

        public MantenimientosCorrectivosViewModel(
            IMantenimientoCorrectivoService service,
            IGestLogLogger logger,
            CurrentUserInfo currentUser)
        {
            _service = service ?? throw new ArgumentNullException(nameof(service));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _currentUser = currentUser ?? throw new ArgumentNullException(nameof(currentUser));

            RecalcularPermisos();
            PropertyChanged += MantenimientosCorrectivosViewModel_PropertyChanged;
        }

        private async void MantenimientosCorrectivosViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(FiltroEstadoSeleccionado))
            {
                await CargarMantenimientosAsync();
            }
        }

        [RelayCommand]
        public async Task CargarMantenimientosAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                IsLoading = true;
                ErrorMessage = string.Empty;
                SuccessMessage = string.Empty;

                var mantenimientos = await _service.ObtenerTodosAsync(
                    includeDadosDeBaja: IncluirDadosDeBaja,
                    cancellationToken: cancellationToken);

                if (!string.IsNullOrWhiteSpace(FiltroEstadoSeleccionado) && FiltroEstadoSeleccionado != "Todos")
                {
                    var filtroEnum = FiltroEstadoSeleccionado switch
                    {
                        "Pendiente" => EstadoMantenimientoCorrectivo.Pendiente,
                        "En Reparación" => EstadoMantenimientoCorrectivo.EnReparacion,
                        "Completado" => EstadoMantenimientoCorrectivo.Completado,
                        "Cancelado" => EstadoMantenimientoCorrectivo.Cancelado,
                        _ => (EstadoMantenimientoCorrectivo?)null
                    };

                    if (filtroEnum.HasValue)
                    {
                        mantenimientos = mantenimientos
                            .Where(m => m.Estado == filtroEnum.Value)
                            .ToList();
                    }
                }

                MantenimientosCorrectivos.Clear();
                foreach (var mtto in mantenimientos)
                {
                    MantenimientosCorrectivos.Add(mtto);
                }

                ActualizarEstadisticas(mantenimientos);
                _logger.LogInformation("Mantenimientos correctivos cargados. Total: {Count}", mantenimientos.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cargando mantenimientos correctivos");
                ErrorMessage = "Error al cargar los mantenimientos correctivos";
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        public async Task CargarMantenimientosEquipoAsync(int equipoId, CancellationToken cancellationToken = default)
        {
            try
            {
                IsLoading = true;
                ErrorMessage = string.Empty;

                var mantenimientos = await _service.ObtenerPorEquipoAsync(equipoId, cancellationToken);

                MantenimientosCorrectivos.Clear();
                foreach (var mtto in mantenimientos)
                {
                    MantenimientosCorrectivos.Add(mtto);
                }

                _logger.LogInformation("Mantenimientos del equipo {EquipoId} cargados. Total: {Count}", equipoId, mantenimientos.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cargando mantenimientos del equipo {EquipoId}", equipoId);
                ErrorMessage = "Error al cargar los mantenimientos del equipo";
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        public async Task CargarMantenimientosPerifericoAsync(int perifericoId, CancellationToken cancellationToken = default)
        {
            try
            {
                IsLoading = true;
                ErrorMessage = string.Empty;

                var mantenimientos = await _service.ObtenerPorPerifericoAsync(perifericoId, cancellationToken);

                MantenimientosCorrectivos.Clear();
                foreach (var mtto in mantenimientos)
                {
                    MantenimientosCorrectivos.Add(mtto);
                }

                _logger.LogInformation("Mantenimientos del periférico {PerifericoId} cargados. Total: {Count}", perifericoId, mantenimientos.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cargando mantenimientos del periférico {PerifericoId}", perifericoId);
                ErrorMessage = "Error al cargar los mantenimientos del periférico";
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        public async Task CompletarMantenimientoAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                if (SelectedMantenimiento == null)
                {
                    ErrorMessage = "Debe seleccionar un mantenimiento para completar";
                    return;
                }

                if (!CanAccederGestionEquiposInformaticos)
                {
                    ErrorMessage = "No tiene permisos para completar mantenimientos correctivos";
                    return;
                }

                IsLoading = true;
                ErrorMessage = string.Empty;

                var resultado = await _service.CompletarAsync(
                    SelectedMantenimiento.Id ?? 0,
                    SelectedMantenimiento.Observaciones,
                    cancellationToken);

                if (resultado)
                {
                    SuccessMessage = "Mantenimiento correctivo marcado como completado";
                    _logger.LogInformation("Mantenimiento correctivo completado. ID: {Id}", SelectedMantenimiento?.Id ?? 0);

                    await CargarMantenimientosAsync(cancellationToken);
                    WeakReferenceMessenger.Default.Send(new MantenimientosCorrectivosActualizadosMessage());
                }
                else
                {
                    ErrorMessage = "No se encontró el mantenimiento correctivo a completar";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error completando mantenimiento correctivo");
                ErrorMessage = "Error al completar el mantenimiento correctivo";
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        public async Task CancelarMantenimientoAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                if (SelectedMantenimiento == null)
                {
                    ErrorMessage = "Debe seleccionar un mantenimiento para cancelar";
                    return;
                }

                if (!CanAccederGestionEquiposInformaticos)
                {
                    ErrorMessage = "No tiene permisos para cancelar mantenimientos correctivos";
                    return;
                }

                IsLoading = true;
                ErrorMessage = string.Empty;

                var resultado = await _service.CancelarAsync(
                    SelectedMantenimiento.Id ?? 0,
                    SelectedMantenimiento.Observaciones ?? "Cancelación del usuario",
                    cancellationToken);

                if (resultado)
                {
                    SuccessMessage = "Mantenimiento correctivo cancelado";
                    _logger.LogInformation("Mantenimiento correctivo cancelado. ID: {Id}", SelectedMantenimiento?.Id ?? 0);

                    await CargarMantenimientosAsync(cancellationToken);
                    WeakReferenceMessenger.Default.Send(new MantenimientosCorrectivosActualizadosMessage());
                }
                else
                {
                    ErrorMessage = "No se encontró el mantenimiento correctivo a cancelar";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cancelando mantenimiento correctivo");
                ErrorMessage = "Error al cancelar el mantenimiento correctivo";
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        public async Task AplicarFiltrosAsync(CancellationToken cancellationToken = default)
        {
            await CargarMantenimientosAsync(cancellationToken);
        }

        [RelayCommand]
        public async Task LimpiarFiltrosAsync(CancellationToken cancellationToken = default)
        {
            FiltroEstado = null;
            FiltroEstadoSeleccionado = "Todos";
            IncluirDadosDeBaja = false;
            await CargarMantenimientosAsync(cancellationToken);
        }        [RelayCommand]
        public async Task AgregarMantenimientoAsync()
        {
            try
            {
                if (!CanAccederGestionEquiposInformaticos)
                {
                    ErrorMessage = "No tiene permisos para agregar mantenimientos correctivos";
                    return;
                }

                var dialog = new RegistroMantenimientoCorrectivoDialog();
                var ownerWindow = WPFApplication.Current?.MainWindow;
                
                if (ownerWindow != null)
                {
                    dialog.ConfigurarParaVentanaPadre(ownerWindow);
                }

                if (dialog.ShowDialog() == true)
                {
                    SuccessMessage = "Mantenimiento correctivo registrado exitosamente";
                    _logger.LogInformation("Nuevo mantenimiento correctivo registrado por {Usuario}", _currentUser.Username);

                    await CargarMantenimientosAsync();
                    WeakReferenceMessenger.Default.Send(new MantenimientosCorrectivosActualizadosMessage());
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al abrir diálogo de registro de mantenimiento correctivo");
                ErrorMessage = "Error al abrir el formulario de registro";
            }
        }        [RelayCommand]
        public async Task GestionarMantenimientoAsync()
        {
            try
            {
                if (SelectedMantenimiento == null)
                {
                    ErrorMessage = "Debe seleccionar un mantenimiento para gestionar";
                    return;
                }

                if (!CanAccederGestionEquiposInformaticos)
                {
                    ErrorMessage = "No tiene permisos para gestionar mantenimientos correctivos";
                    return;
                }

                var dialog = new CompletarCancelarMantenimientoDialog();
                var ownerWindow = WPFApplication.Current?.MainWindow;
                
                if (ownerWindow != null)
                {
                    dialog.ConfigurarParaVentanaPadre(ownerWindow);
                }

                if (dialog.ViewModel != null)
                {
                    dialog.ViewModel.MantenimientoSeleccionado = SelectedMantenimiento;
                }

                if (dialog.ShowDialog() == true)
                {
                    SuccessMessage = "Mantenimiento correctivo actualizado";
                    _logger.LogInformation("Mantenimiento correctivo actualizado. ID: {Id}", SelectedMantenimiento?.Id ?? 0);

                    await CargarMantenimientosAsync();
                    WeakReferenceMessenger.Default.Send(new MantenimientosCorrectivosActualizadosMessage());
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al abrir diálogo de gestión de mantenimiento correctivo");
                ErrorMessage = "Error al abrir el formulario de gestión";
            }
        }

        private void RecalcularPermisos()
        {
            CanAccederGestionEquiposInformaticos = _currentUser.HasPermission("GestionEquiposInformaticos.Acceder");
            CanEliminarGestionEquiposInformaticos = _currentUser.HasPermission("GestionEquiposInformaticos.Eliminar");
        }

        public static string ObtenerDescripcionEstado(EstadoMantenimientoCorrectivo estado)
        {
            return estado switch
            {
                EstadoMantenimientoCorrectivo.Pendiente => "Pendiente",
                EstadoMantenimientoCorrectivo.EnReparacion => "En Reparación",
                EstadoMantenimientoCorrectivo.Completado => "Completado",
                EstadoMantenimientoCorrectivo.Cancelado => "Cancelado",
                _ => "Desconocido"
            };
        }

        private void ActualizarEstadisticas(List<MantenimientoCorrectivoDto> mantenimientos)
        {
            IsEmpty = mantenimientos.Count == 0;

            if (IsEmpty)
            {
                EstadisticasText = "No hay mantenimientos correctivos registrados";
                return;
            }

            var pendientes = mantenimientos.Count(m => m.Estado == EstadoMantenimientoCorrectivo.Pendiente);
            var enReparacion = mantenimientos.Count(m => m.Estado == EstadoMantenimientoCorrectivo.EnReparacion);
            var completados = mantenimientos.Count(m => m.Estado == EstadoMantenimientoCorrectivo.Completado);
            var cancelados = mantenimientos.Count(m => m.Estado == EstadoMantenimientoCorrectivo.Cancelado);

            EstadisticasText = $"Total: {mantenimientos.Count} | " +
                              $"🟡 Pendientes: {pendientes} | " +
                              $"🟠 En Reparación: {enReparacion} | " +
                              $"🟢 Completados: {completados} | " +
                              $"🔴 Cancelados: {cancelados}";
        }
    }
}
