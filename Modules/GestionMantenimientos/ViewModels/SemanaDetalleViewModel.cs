using System;
using System.Collections.ObjectModel;
using GestLog.Modules.GestionMantenimientos.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GestLog.Modules.GestionMantenimientos.Interfaces;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

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
        [ObservableProperty]
        private ObservableCollection<CronogramaMantenimientoDto> mantenimientos = new();        public IRelayCommand<MantenimientoSemanaEstadoDto?> VerSeguimientoCommand { get; }
        public IAsyncRelayCommand<MantenimientoSemanaEstadoDto?> RegistrarMantenimientoCommand { get; }
        public IAsyncRelayCommand<MantenimientoSemanaEstadoDto?> MarcarAtrasadoCommand { get; }        public SemanaDetalleViewModel(string titulo, string rangoFechas, ObservableCollection<MantenimientoSemanaEstadoDto> estados, ObservableCollection<CronogramaMantenimientoDto> mantenimientos, ISeguimientoService seguimientoService)
        {
            Titulo = titulo;
            RangoFechas = rangoFechas;
            EstadosMantenimientos = estados;
            Mantenimientos = mantenimientos;
            _seguimientoService = seguimientoService;
            
            // Log para debug
            var logger = GestLog.Services.Core.Logging.LoggingService.GetLogger();
            logger.Logger.LogInformation("[SemanaDetalleViewModel] Constructor llamado - Título: {titulo}, Estados: {estadosCount}, Mantenimientos: {mantenimientosCount}", 
                titulo, estados?.Count ?? 0, mantenimientos?.Count ?? 0);
              // Inicializar comandos
            VerSeguimientoCommand = new RelayCommand<MantenimientoSemanaEstadoDto?>(VerSeguimiento);
            RegistrarMantenimientoCommand = new AsyncRelayCommand<MantenimientoSemanaEstadoDto?>(RegistrarMantenimientoAsync);
            MarcarAtrasadoCommand = new AsyncRelayCommand<MantenimientoSemanaEstadoDto?>(MarcarAtrasadoAsync);
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
                
                // Crear DTO prellenado para el formulario
                var seguimientoDto = new SeguimientoMantenimientoDto
                {
                    Codigo = estado.CodigoEquipo,
                    Nombre = estado.NombreEquipo,
                    FechaRegistro = DateTime.Now,
                    // Si ya existe seguimiento previo, prellenar TipoMtno
                    TipoMtno = estado.Seguimiento?.TipoMtno
                };
                // Abrir el diálogo de registro de mantenimiento con el DTO prellenado
                var dialog = new GestLog.Views.Tools.GestionMantenimientos.SeguimientoDialog(seguimientoDto);
                if (dialog.ShowDialog() == true)
                {
                    var seguimiento = dialog.Seguimiento;
                    // Calcular semana y año de la fecha actual
                    var fechaRegistro = seguimiento.FechaRegistro ?? DateTime.Now;
                    var cal = System.Globalization.CultureInfo.CurrentCulture.Calendar;
                    int semanaActual = cal.GetWeekOfYear(fechaRegistro, System.Globalization.CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday);
                    int anioActual = fechaRegistro.Year;
                    // Determinar estado
                    if (estado.Anio == anioActual && estado.Semana == semanaActual)
                        seguimiento.Estado = Models.Enums.EstadoSeguimientoMantenimiento.RealizadoEnTiempo;
                    else
                        seguimiento.Estado = Models.Enums.EstadoSeguimientoMantenimiento.RealizadoFueraDeTiempo;
                    await _seguimientoService.AddAsync(seguimiento);
                    estado.Realizado = true;
                    estado.Atrasado = false;
                    estado.Seguimiento = seguimiento;
                    estado.Estado = seguimiento.Estado;
                    EstadosMantenimientos = new ObservableCollection<MantenimientoSemanaEstadoDto>(EstadosMantenimientos);
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
                    EstadosMantenimientos = new ObservableCollection<MantenimientoSemanaEstadoDto>(EstadosMantenimientos);
                    return;
                }
                
                // Si existe seguimiento, actualizarlo en la base de datos
                if (estado.Seguimiento != null)
                {
                    estado.Seguimiento.Observaciones += " [Marcado como atrasado]";
                    await _seguimientoService.UpdateAsync(estado.Seguimiento);
                }
                
                EstadosMantenimientos = new ObservableCollection<MantenimientoSemanaEstadoDto>(EstadosMantenimientos);
                MensajeUsuario = "Mantenimiento marcado como atrasado.";
            }
            catch (Exception ex)
            {
                MensajeUsuario = $"Error al marcar como atrasado: {ex.Message}";
            }
        }
    }
}
