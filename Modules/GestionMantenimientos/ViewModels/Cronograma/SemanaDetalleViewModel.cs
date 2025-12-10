using System;
using System.Collections.ObjectModel;
using GestLog.Modules.GestionMantenimientos.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GestLog.Modules.GestionMantenimientos.Interfaces;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using CommunityToolkit.Mvvm.Messaging;
using GestLog.Modules.GestionMantenimientos.Messages;
using GestLog.Modules.Usuarios.Models.Authentication;
using GestLog.Modules.Usuarios.Interfaces;

namespace GestLog.Modules.GestionMantenimientos.ViewModels.Cronograma
{    
    public partial class SemanaDetalleViewModel : ObservableObject
    {
        private readonly ISeguimientoService? _seguimientoService;
        private readonly ICurrentUserService? _currentUserService;

        public string Titulo { get; }
        public string RangoFechas { get; }
        
        [ObservableProperty]
        private string? mensajeUsuario;
        
        [ObservableProperty]
        private ObservableCollection<MantenimientoSemanaEstadoDto> estadosMantenimientos = new();
        partial void OnEstadosMantenimientosChanged(ObservableCollection<MantenimientoSemanaEstadoDto> value)
        {
            ActualizarPuedeRegistrarMantenimientos();
        }
        
        [ObservableProperty]
        private ObservableCollection<CronogramaMantenimientoDto> mantenimientos = new();

        public IRelayCommand<MantenimientoSemanaEstadoDto?> VerSeguimientoCommand { get; }
        public IAsyncRelayCommand<MantenimientoSemanaEstadoDto?> RegistrarMantenimientoCommand { get; }
        public IAsyncRelayCommand<MantenimientoSemanaEstadoDto?> MarcarAtrasadoCommand { get; }

        [ObservableProperty]
        private int semanaActual;
        [ObservableProperty]
        private int anioActual;
        
        // Permisos reactivos para mantenimiento
        [ObservableProperty]
        private bool canRegistrarMantenimiento;
        [ObservableProperty]
        private bool canMarcarAtrasado;
        
        public bool PuedeRegistrarMantenimiento(int semana, int anio)
        {
            int semanaActual = GetSemanaActual();
            int anioActual = GetAnioActual();
            var hoy = DateTime.Now;
            var primerDiaSemana = FirstDateOfWeekISO8601(anio, semana);
            var lunesSiguiente = primerDiaSemana.AddDays(7); // lunes siguiente
            var viernesSiguiente = primerDiaSemana.AddDays(11); // viernes siguiente
            // Permitir registro en la semana actual y la anterior, hasta el viernes de la semana siguiente
            if (anio == anioActual && (semana == semanaActual || semana == semanaActual - 1))
            {
                if (hoy.Date <= viernesSiguiente.Date)
                    return true;
            }
            return false;
        }

        // Determina el estado de registro según la fecha actual
        public Models.Enums.EstadoSeguimientoMantenimiento CalcularEstadoRegistro(int semana, int anio)
        {
            var hoy = DateTime.Now;
            var primerDiaSemana = FirstDateOfWeekISO8601(anio, semana);
            var lunesSiguiente = primerDiaSemana.AddDays(7);
            var viernesSiguiente = primerDiaSemana.AddDays(11);
            if (hoy.Date <= lunesSiguiente.Date)
                return Models.Enums.EstadoSeguimientoMantenimiento.RealizadoEnTiempo;
            if (hoy.Date > lunesSiguiente.Date && hoy.Date <= viernesSiguiente.Date)
                return Models.Enums.EstadoSeguimientoMantenimiento.RealizadoFueraDeTiempo;
            return Models.Enums.EstadoSeguimientoMantenimiento.Atrasado;
        }

        // Utilidad para obtener el primer día de la semana ISO 8601
        private static DateTime FirstDateOfWeekISO8601(int year, int weekOfYear)
        {
            var jan1 = new DateTime(year, 1, 1);
            int daysOffset = DayOfWeek.Thursday - jan1.DayOfWeek;
            var firstThursday = jan1.AddDays(daysOffset);
            var cal = System.Globalization.CultureInfo.CurrentCulture.Calendar;
            int firstWeek = cal.GetWeekOfYear(firstThursday, System.Globalization.CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday);
            var weekNum = weekOfYear;
            if (firstWeek <= 1)
                weekNum -= 1;
            var result = firstThursday.AddDays(weekNum * 7);
            return result.AddDays(-3);
        }
        private int GetSemanaActual()
        {
            var hoy = DateTime.Now;
            var cal = System.Globalization.CultureInfo.CurrentCulture.Calendar;
            return cal.GetWeekOfYear(hoy, System.Globalization.CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday);
        }
        private int GetAnioActual()
        {
            return DateTime.Now.Year;
        }        private void ActualizarPuedeRegistrarMantenimientos()
        {
            if (EstadosMantenimientos == null) return;
            
            foreach (var estado in EstadosMantenimientos.ToList())
            {
                // Permitir registro para Pendiente, Atrasado y NoRealizado
                var esEstadoRegistrable = estado.Estado == Models.Enums.EstadoSeguimientoMantenimiento.Pendiente || 
                                         estado.Estado == Models.Enums.EstadoSeguimientoMantenimiento.Atrasado ||
                                         estado.Estado == Models.Enums.EstadoSeguimientoMantenimiento.NoRealizado;
                var nuevoValor = esEstadoRegistrable && PuedeRegistrarMantenimiento(estado.Semana, estado.Anio);
                if (estado.PuedeRegistrar != nuevoValor)
                {
                    estado.PuedeRegistrar = nuevoValor;
                }
                else
                {
                    estado.PuedeRegistrar = !nuevoValor;
                    estado.PuedeRegistrar = nuevoValor;
                }
            }
        }        
        public SemanaDetalleViewModel(string titulo, string rangoFechas, ObservableCollection<MantenimientoSemanaEstadoDto> estados, ObservableCollection<CronogramaMantenimientoDto> mantenimientos, ISeguimientoService seguimientoService, ICurrentUserService currentUserService)
        {
            Titulo = titulo;
            RangoFechas = rangoFechas;
            EstadosMantenimientos = estados;
            Mantenimientos = mantenimientos;
            _seguimientoService = seguimientoService;
            _currentUserService = currentUserService ?? throw new ArgumentNullException(nameof(currentUserService));

            // Suscribirse a cambios de usuario
            _currentUserService.CurrentUserChanged += OnCurrentUserChanged;
            RecalcularPermisos();

            // Suscribirse a mensajes de actualización de seguimientos            WeakReferenceMessenger.Default.Register<SeguimientosActualizadosMessage>(this, async (r, m) => await RecargarEstadosAsync());
            
            // Inicializar comandos
            VerSeguimientoCommand = new RelayCommand<MantenimientoSemanaEstadoDto?>(VerSeguimiento);
            RegistrarMantenimientoCommand = new AsyncRelayCommand<MantenimientoSemanaEstadoDto?>(RegistrarMantenimientoAsync, CanExecuteRegistrarMantenimiento);
            MarcarAtrasadoCommand = new AsyncRelayCommand<MantenimientoSemanaEstadoDto?>(MarcarAtrasadoAsync, CanExecuteMarcarAtrasado);            
            ActualizarPuedeRegistrarMantenimientos();
        }

        private bool CanExecuteRegistrarMantenimiento(MantenimientoSemanaEstadoDto? estado)
        {
            return CanRegistrarMantenimiento && estado?.PuedeRegistrar == true;
        }

        private bool CanExecuteMarcarAtrasado(MantenimientoSemanaEstadoDto? estado)
        {
            return CanMarcarAtrasado && estado != null;
        }
        
        private void OnCurrentUserChanged(object? sender, CurrentUserInfo? user)
        {
            RecalcularPermisos();
        }

        private void RecalcularPermisos()
        {
            if (_currentUserService?.Current != null)
            {
                CanRegistrarMantenimiento = _currentUserService.Current.HasPermission("GestionMantenimientos.RegistrarMantenimiento");
                CanMarcarAtrasado = _currentUserService.Current.HasPermission("GestionMantenimientos.MarcarAtrasado");
            }
            else
            {
                CanRegistrarMantenimiento = false;
                CanMarcarAtrasado = false;
            }
        }        
        public SemanaDetalleViewModel(string titulo, string rangoFechas, ObservableCollection<MantenimientoSemanaEstadoDto> estados, ObservableCollection<CronogramaMantenimientoDto> mantenimientos, ICurrentUserService? currentUserService = null)
        {
            Titulo = titulo;
            RangoFechas = rangoFechas;
            EstadosMantenimientos = estados;
            Mantenimientos = mantenimientos;
            _seguimientoService = null;
            _currentUserService = currentUserService;
            
            // Suscribirse a cambios de usuario si el servicio está disponible
            if (_currentUserService != null)
            {
                _currentUserService.CurrentUserChanged += OnCurrentUserChanged;
            }
            RecalcularPermisos();
            
            // Inicializar comandos
            VerSeguimientoCommand = new RelayCommand<MantenimientoSemanaEstadoDto?>(VerSeguimiento);
            RegistrarMantenimientoCommand = new AsyncRelayCommand<MantenimientoSemanaEstadoDto?>(RegistrarMantenimientoAsync, CanExecuteRegistrarMantenimiento);
            MarcarAtrasadoCommand = new AsyncRelayCommand<MantenimientoSemanaEstadoDto?>(MarcarAtrasadoAsync, CanExecuteMarcarAtrasado);
        }
        
        public async Task RegistrarMantenimientoAsync(MantenimientoSemanaEstadoDto? estado)
        {
            try
            {
                MensajeUsuario = null;
                if (estado == null)
                {
                    MensajeUsuario = "No se ha seleccionado un estado de mantenimiento.";
                    return;
                }
                if (_seguimientoService == null)
                {
                    MensajeUsuario = "El servicio de seguimiento no está disponible.";
                    return;
                }
                // Permitir registrar como NoRealizado y guardar la fecha de registro
                var seguimientoDto = new SeguimientoMantenimientoDto
                {
                    Codigo = estado.CodigoEquipo,
                    Nombre = estado.NombreEquipo,
                    FechaRegistro = DateTime.Now,
                    TipoMtno = estado.Seguimiento?.TipoMtno,
                    Semana = estado.Semana,
                    Anio = estado.Anio
                };                var dialog = new GestLog.Modules.GestionMantenimientos.Views.Seguimiento.SeguimientoDialog(seguimientoDto, true, esDesdeCronograma: true); // modoRestringido: true, esDesdeCronograma: true
                dialog.Owner = System.Windows.Application.Current.MainWindow;
                if (dialog.ShowDialog() == true)
                {
                    var seguimiento = dialog.Seguimiento;
                    seguimiento.FechaRegistro = DateTime.Now;
                    seguimiento.Semana = estado.Semana;
                    seguimiento.Anio = estado.Anio;
                    // Si el usuario marca como NoRealizado, guardar la fecha de registro
                    if (seguimiento.Estado == Models.Enums.EstadoSeguimientoMantenimiento.NoRealizado)
                    {
                        seguimiento.FechaRegistro = DateTime.Now;
                        seguimiento.FechaRealizacion = null;
                    }
                    await _seguimientoService.AddAsync(seguimiento);
                    WeakReferenceMessenger.Default.Send(new SeguimientosActualizadosMessage());
                    estado.Realizado = seguimiento.Estado == Models.Enums.EstadoSeguimientoMantenimiento.RealizadoEnTiempo || seguimiento.Estado == Models.Enums.EstadoSeguimientoMantenimiento.RealizadoFueraDeTiempo;
                    estado.Atrasado = seguimiento.Estado == Models.Enums.EstadoSeguimientoMantenimiento.Atrasado;
                    estado.Seguimiento = seguimiento;
                    estado.Estado = seguimiento.Estado;
                    RefrescarEstados(EstadosMantenimientos);
                    MensajeUsuario = "Mantenimiento registrado exitosamente.";
                }
            }
            catch (Exception ex)
            {
                MensajeUsuario = $"Error al registrar el mantenimiento: {ex.Message}";
            }
        }

        public void VerSeguimiento(MantenimientoSemanaEstadoDto? estado)
        {
            try
            {
                MensajeUsuario = null;
                  if (estado?.Seguimiento != null)
                {                    var dialog = new GestLog.Modules.GestionMantenimientos.Views.Seguimiento.SeguimientoDialog();
                    dialog.Owner = System.Windows.Application.Current.MainWindow;
                    dialog.DataContext = estado.Seguimiento;
                    dialog.ShowDialog();
                }
                else
                {
                    MensajeUsuario = "No existe seguimiento registrado para este mantenimiento.";
                }
            }
            catch (Exception ex)
            {
                MensajeUsuario = $"Error al abrir el seguimiento: {ex.Message}";
            }
        }        
        public async Task MarcarAtrasadoAsync(MantenimientoSemanaEstadoDto? estado)
        {
            try
            {
                MensajeUsuario = null;
                if (estado == null)
                {
                    MensajeUsuario = "No se ha seleccionado un estado de mantenimiento.";
                    return;
                }
                estado.Atrasado = true;
                if (_seguimientoService == null)
                {
                    MensajeUsuario = "El servicio de seguimiento no está disponible.";
                    RefrescarEstados(EstadosMantenimientos);
                    ActualizarPuedeRegistrarMantenimientos();
                    return;
                }
                // Si existe seguimiento, actualizarlo en la base de datos
                if (estado.Seguimiento != null)
                {
                    estado.Seguimiento.Observaciones += " [Marcado como atrasado]";
                    await _seguimientoService.UpdateAsync(estado.Seguimiento);
                    // Notificar a otros ViewModels
                    WeakReferenceMessenger.Default.Send(new SeguimientosActualizadosMessage());
                }
                RefrescarEstados(EstadosMantenimientos);
                ActualizarPuedeRegistrarMantenimientos();
                MensajeUsuario = "Mantenimiento marcado como atrasado.";
            }
            catch (Exception ex)
            {
                MensajeUsuario = $"Error al marcar como atrasado: {ex.Message}";
            }
        }

        public async Task RecargarEstadosAsync()
        {
            if (EstadosMantenimientos.Count > 0)
            {
                var anio = EstadosMantenimientos[0].Anio;
                var semana = EstadosMantenimientos[0].Semana;
                var cronogramaService = GestLog.Services.Core.Logging.LoggingService.GetServiceProvider().GetService(typeof(GestLog.Modules.GestionMantenimientos.Interfaces.ICronogramaService)) as GestLog.Modules.GestionMantenimientos.Interfaces.ICronogramaService;
                if (cronogramaService != null)
                {
                    var nuevosEstados = await cronogramaService.GetEstadoMantenimientosSemanaAsync(semana, anio);
                    RefrescarEstados(nuevosEstados);
                }
            }
        }

        public void RefrescarEstados(IList<MantenimientoSemanaEstadoDto> nuevosEstados)
        {
            GestLog.Services.Core.UI.DispatcherService.InvokeOnUIThread(() =>
            {
                EstadosMantenimientos.Clear();
                foreach (var estado in nuevosEstados)
                {
                    // Clonar el DTO para forzar refresco de la UI
                    var clon = new MantenimientoSemanaEstadoDto
                    {
                        CodigoEquipo = estado.CodigoEquipo,
                        NombreEquipo = estado.NombreEquipo,
                        Semana = estado.Semana,
                        Anio = estado.Anio,
                        Frecuencia = estado.Frecuencia,
                        Programado = estado.Programado,
                        Realizado = estado.Realizado,
                        Atrasado = estado.Atrasado,
                        Seguimiento = estado.Seguimiento,
                        Estado = estado.Estado,
                        PuedeRegistrar = estado.PuedeRegistrar
                    };
                    EstadosMantenimientos.Add(clon);
                }
                ActualizarPuedeRegistrarMantenimientos();
            });
        }
    }
}

