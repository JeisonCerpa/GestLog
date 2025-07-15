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

namespace GestLog.Modules.GestionMantenimientos.ViewModels
{
    public partial class SemanaDetalleViewModel : ObservableObject
    {
        private readonly ISeguimientoService? _seguimientoService;

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
        private ObservableCollection<CronogramaMantenimientoDto> mantenimientos = new();        public IRelayCommand<MantenimientoSemanaEstadoDto?> VerSeguimientoCommand { get; }
        public IAsyncRelayCommand<MantenimientoSemanaEstadoDto?> RegistrarMantenimientoCommand { get; }
        public IAsyncRelayCommand<MantenimientoSemanaEstadoDto?> MarcarAtrasadoCommand { get; }        [ObservableProperty]
        private int semanaActual;
        [ObservableProperty]
        private int anioActual;
        
        public bool PuedeRegistrarMantenimiento(int semana, int anio)
        {
            int semanaActual = GetSemanaActual();
            int anioActual = GetAnioActual();
            // Solo permitir registro en la semana actual, una antes y una después
            if (anio == anioActual && Math.Abs(semana - semanaActual) <= 1)
                return true;
            // No permitir en otros casos
            return false;
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
        }
        private void ActualizarPuedeRegistrarMantenimientos()
        {
            if (EstadosMantenimientos == null) return;
            var logger = GestLog.Services.Core.Logging.LoggingService.GetLogger();
            logger.Logger.LogInformation("[SemanaDetalleViewModel] ActualizarPuedeRegistrarMantenimientos - Actualizando {count} estados", EstadosMantenimientos.Count);
            foreach (var estado in EstadosMantenimientos.ToList())
            {
                // Permitir registro solo para Pendiente y Atrasado (NO para NoRealizado)
                var esEstadoRegistrable = estado.Estado == Models.Enums.EstadoSeguimientoMantenimiento.Pendiente || 
                                         estado.Estado == Models.Enums.EstadoSeguimientoMantenimiento.Atrasado;
                // No permitir registro si el estado es NoRealizado
                if (estado.Estado == Models.Enums.EstadoSeguimientoMantenimiento.NoRealizado)
                    esEstadoRegistrable = false;
                var nuevoValor = esEstadoRegistrable && PuedeRegistrarMantenimiento(estado.Semana, estado.Anio);
                logger.Logger.LogInformation("[SemanaDetalleViewModel] Estado: {codigo} - Semana: {semana}, Año: {anio}, EstadoActual: {estadoActual}, EsRegistrable: {esRegistrable}, PuedeRegistrarCalculado: {puedeRegistrar}", 
                    estado.CodigoEquipo, estado.Semana, estado.Anio, estado.Estado, esEstadoRegistrable, nuevoValor);
                if (estado.PuedeRegistrar != nuevoValor)
                {
                    estado.PuedeRegistrar = nuevoValor;
                }
                else
                {
                    // Forzar notificación para la UI
                    estado.PuedeRegistrar = !nuevoValor;
                    estado.PuedeRegistrar = nuevoValor;
                }
                logger.Logger.LogInformation("[SemanaDetalleViewModel] Estado: {codigo} - PuedeRegistrar final: {puedeRegistrarFinal}", 
                    estado.CodigoEquipo, estado.PuedeRegistrar);
            }
        }
        public SemanaDetalleViewModel(string titulo, string rangoFechas, ObservableCollection<MantenimientoSemanaEstadoDto> estados, ObservableCollection<CronogramaMantenimientoDto> mantenimientos, ISeguimientoService seguimientoService)
        {
            Titulo = titulo;
            RangoFechas = rangoFechas;
            EstadosMantenimientos = estados;
            Mantenimientos = mantenimientos;
            _seguimientoService = seguimientoService;
            // Suscribirse a mensajes de actualización de seguimientos
            WeakReferenceMessenger.Default.Register<SeguimientosActualizadosMessage>(this, async (r, m) => await RecargarEstadosAsync());
            
            // Log para debug
            var logger = GestLog.Services.Core.Logging.LoggingService.GetLogger();
            logger.Logger.LogInformation("[SemanaDetalleViewModel] Constructor llamado - Título: {titulo}, Estados: {estadosCount}, Mantenimientos: {mantenimientosCount}", 
                titulo, estados?.Count ?? 0, mantenimientos?.Count ?? 0);
                      // Inicializar comandos
        VerSeguimientoCommand = new RelayCommand<MantenimientoSemanaEstadoDto?>(VerSeguimiento);
        RegistrarMantenimientoCommand = new AsyncRelayCommand<MantenimientoSemanaEstadoDto?>(RegistrarMantenimientoAsync);
        MarcarAtrasadoCommand = new AsyncRelayCommand<MantenimientoSemanaEstadoDto?>(MarcarAtrasadoAsync);
        
        ActualizarPuedeRegistrarMantenimientos();
        }

        public SemanaDetalleViewModel(string titulo, string rangoFechas, ObservableCollection<MantenimientoSemanaEstadoDto> estados, ObservableCollection<CronogramaMantenimientoDto> mantenimientos)
        {
            Titulo = titulo;
            RangoFechas = rangoFechas;
            EstadosMantenimientos = estados;
            Mantenimientos = mantenimientos;
            _seguimientoService = null;
            
            // Inicializar comandos
            VerSeguimientoCommand = new RelayCommand<MantenimientoSemanaEstadoDto?>(VerSeguimiento);
            RegistrarMantenimientoCommand = new AsyncRelayCommand<MantenimientoSemanaEstadoDto?>(RegistrarMantenimientoAsync);
            MarcarAtrasadoCommand = new AsyncRelayCommand<MantenimientoSemanaEstadoDto?>(MarcarAtrasadoAsync);
        }        public async Task RegistrarMantenimientoAsync(MantenimientoSemanaEstadoDto? estado)
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
                if (estado.Estado == Models.Enums.EstadoSeguimientoMantenimiento.NoRealizado)
                {
                    MensajeUsuario = "No se puede registrar un mantenimiento para una semana marcada como 'No realizado'.";
                    return;
                }
                var seguimientoDto = new SeguimientoMantenimientoDto
                {
                    Codigo = estado.CodigoEquipo,
                    Nombre = estado.NombreEquipo,
                    FechaRegistro = DateTime.Now,
                    TipoMtno = estado.Seguimiento?.TipoMtno,
                    Semana = estado.Semana,
                    Anio = estado.Anio
                };
                var dialog = new GestLog.Views.Tools.GestionMantenimientos.SeguimientoDialog(seguimientoDto);
                if (dialog.ShowDialog() == true)
                {
                    var seguimiento = dialog.Seguimiento;
                    var fechaRegistro = seguimiento.FechaRegistro ?? DateTime.Now;
                    var cal = System.Globalization.CultureInfo.CurrentCulture.Calendar;
                    int semanaActual = cal.GetWeekOfYear(fechaRegistro, System.Globalization.CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday);
                    int anioActual = fechaRegistro.Year;
                    seguimiento.Semana = estado.Semana;
                    seguimiento.Anio = estado.Anio;
                    if (estado.Anio == anioActual && estado.Semana == semanaActual)
                        seguimiento.Estado = Models.Enums.EstadoSeguimientoMantenimiento.RealizadoEnTiempo;
                    else
                        seguimiento.Estado = Models.Enums.EstadoSeguimientoMantenimiento.RealizadoFueraDeTiempo;
                    await _seguimientoService.AddAsync(seguimiento);
                    // Notificar a otros ViewModels
                    WeakReferenceMessenger.Default.Send(new SeguimientosActualizadosMessage());
                    estado.Realizado = true;
                    estado.Atrasado = false;
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
                {
                    var dialog = new GestLog.Views.Tools.GestionMantenimientos.SeguimientoDialog();
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
        }        public async Task MarcarAtrasadoAsync(MantenimientoSemanaEstadoDto? estado)
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
        }
    }
}
