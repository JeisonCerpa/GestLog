using System;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using GestLog.Modules.GestionMantenimientos.Interfaces;
using GestLog.Modules.GestionMantenimientos.Messages;
using GestLog.Modules.GestionMantenimientos.Models;
using GestLog.Modules.GestionMantenimientos.Models.Enums;
using GestLog.Modules.GestionMantenimientos.Services;
using Microsoft.Extensions.DependencyInjection;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace GestLog.Modules.GestionMantenimientos.ViewModels
{
    /// <summary>
    /// ViewModel que representa una semana y sus mantenimientos asociados.
    /// </summary>
    public partial class SemanaViewModel : ObservableObject
    {
        public int NumeroSemana { get; }
        public DateTime FechaInicio { get; }
        public DateTime FechaFin { get; }
        public string TituloSemana => $"Semana {NumeroSemana}: {FechaInicio:dd/MM} - {FechaFin:dd/MM}";
        public string RangoFechas => $"{FechaInicio:dd/MM} - {FechaFin:dd/MM}";
        public bool TieneMantenimientos => Mantenimientos != null && Mantenimientos.Count > 0;

        [ObservableProperty]
        private ObservableCollection<CronogramaMantenimientoDto> mantenimientos = new();

        [ObservableProperty]
        private ObservableCollection<MantenimientoSemanaEstadoDto> estadosMantenimientos = new();

        private readonly ICronogramaService _cronogramaService;
        private readonly int _anio;        public SemanaViewModel(int numeroSemana, DateTime fechaInicio, DateTime fechaFin, ICronogramaService cronogramaService, int anio)
        {
            NumeroSemana = numeroSemana;
            FechaInicio = fechaInicio;
            FechaFin = fechaFin;
            _cronogramaService = cronogramaService;
            _anio = anio;
            // Suscribirse a mensajes de actualización de seguimientos
            WeakReferenceMessenger.Default.Register<SeguimientosActualizadosMessage>(this, async (r, m) => await RecargarEstadosAsync(_anio, _cronogramaService));
        }        public async Task VerSemanaAsync()
        {
            if (Mantenimientos == null || Mantenimientos.Count == 0)
                return;
            
            // Log para debug
            var logger = GestLog.Services.Core.Logging.LoggingService.GetLogger();
            logger.Logger.LogInformation("[SemanaViewModel] Ejecutando VerSemanaAsync - Semana {NumeroSemana}, Año {Anio}, Mantenimientos: {Count}", NumeroSemana, _anio, Mantenimientos.Count);
            
            var estados = await _cronogramaService.GetEstadoMantenimientosSemanaAsync(NumeroSemana, _anio);
            
            logger.Logger.LogInformation("[SemanaViewModel] Obtenidos {Count} estados de mantenimiento", estados.Count);
            
            // Obtener el servicio de seguimiento usando DI
            var serviceProvider = GestLog.Services.Core.Logging.LoggingService.GetServiceProvider();
            var seguimientoService = serviceProvider.GetRequiredService<GestLog.Modules.GestionMantenimientos.Interfaces.ISeguimientoService>();
            
            var vm = new GestLog.Modules.GestionMantenimientos.ViewModels.SemanaDetalleViewModel(
                $"Semana {NumeroSemana}",
                $"{FechaInicio:dd/MM/yyyy} - {FechaFin:dd/MM/yyyy}",
                new ObservableCollection<MantenimientoSemanaEstadoDto>(estados),
                new ObservableCollection<CronogramaMantenimientoDto>(Mantenimientos),
                seguimientoService
            );
            
            logger.Logger.LogInformation("[SemanaViewModel] ViewModel creado con {EstadosCount} estados y {MantenimientosCount} mantenimientos", 
                vm.EstadosMantenimientos.Count, vm.Mantenimientos.Count);
            
            var dialog = new GestLog.Views.Tools.GestionMantenimientos.SemanaDetalleDialog(vm);
            dialog.ShowDialog();
            
            logger.Logger.LogInformation("[SemanaViewModel] Dialog cerrado - Semana {NumeroSemana}", NumeroSemana);
        }[RelayCommand]
        public async Task VerSemana()
        {
            await VerSemanaAsync();
        }        public void RefrescarEstados(IList<MantenimientoSemanaEstadoDto> nuevosEstados)
        {
            EstadosMantenimientos.Clear();
            foreach (var estado in nuevosEstados)
            {
                EstadosMantenimientos.Add(estado);
            }
            // Forzar refresco de color y semana vacía tras carga asíncrona
            OnPropertyChanged(nameof(ColorSemana));
            OnPropertyChanged(nameof(EsSemanaVacia));
        }

        public async Task CargarEstadosMantenimientosAsync(int anio, ICronogramaService cronogramaService)
        {
            var estados = await cronogramaService.GetEstadoMantenimientosSemanaAsync(NumeroSemana, anio);
            RefrescarEstados(estados);
        }

        public async Task RecargarEstadosAsync(int anio, ICronogramaService cronogramaService)
        {
            await CargarEstadosMantenimientosAsync(anio, cronogramaService);
        }        // Propiedad calculada para el color de la semana según los estados de los mantenimientos
        public string ColorSemana
        {
            get
            {
                if (EstadosMantenimientos == null || EstadosMantenimientos.Count == 0)
                    return "Transparent"; // Sin datos, transparente para que no se vea borde
                if (EstadosMantenimientos.Any(m => m.Estado == EstadoSeguimientoMantenimiento.NoRealizado))
                    return "#C80000"; // Rojo fuerte personalizado
                if (EstadosMantenimientos.Any(m => m.Estado == EstadoSeguimientoMantenimiento.Atrasado))
                    return "#FFB300"; // Ámbar
                if (EstadosMantenimientos.All(m => m.Estado == EstadoSeguimientoMantenimiento.Pendiente))
                    return "#90CAF9"; // Azul clarito para pendientes
                if (EstadosMantenimientos.All(m => m.Estado == EstadoSeguimientoMantenimiento.RealizadoEnTiempo || m.Estado == EstadoSeguimientoMantenimiento.RealizadoFueraDeTiempo))
                    return "#388E3C"; // Verde fuerte
                if (EstadosMantenimientos.Any(m => m.Estado == EstadoSeguimientoMantenimiento.Pendiente) &&
                    EstadosMantenimientos.Any(m => m.Estado == EstadoSeguimientoMantenimiento.RealizadoEnTiempo || m.Estado == EstadoSeguimientoMantenimiento.RealizadoFueraDeTiempo))
                    return "#FFB300"; // Ámbar
                return "Transparent"; // Default: transparente
            }
        }

        // Propiedad que indica si esta semana es la actual
        public bool IsSemanaActual
        {
            get
            {
                var hoy = DateTime.Now;
                var cal = System.Globalization.CultureInfo.CurrentCulture.Calendar;
                int semanaActual = cal.GetWeekOfYear(hoy, System.Globalization.CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday);
                int anioActual = hoy.Year;
                return NumeroSemana == semanaActual && _anio == anioActual;
            }
        }        // Propiedad que indica si la semana está vacía (sin estados de mantenimiento)
        public bool EsSemanaVacia => EstadosMantenimientos == null || EstadosMantenimientos.Count == 0;        // Notificar cambio de color si cambian los estados
        partial void OnEstadosMantenimientosChanged(ObservableCollection<MantenimientoSemanaEstadoDto> value)
        {
            OnPropertyChanged(nameof(ColorSemana));
            OnPropertyChanged(nameof(EsSemanaVacia));
        }
    }
}
