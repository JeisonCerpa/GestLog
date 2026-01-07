using System;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using GestLog.Modules.GestionMantenimientos.Interfaces.Data;
using GestLog.Modules.GestionMantenimientos.Messages.Mantenimientos;
using GestLog.Modules.GestionMantenimientos.Models;
using GestLog.Modules.GestionMantenimientos.Models.DTOs;
using GestLog.Modules.GestionMantenimientos.Models.Enums;
using GestLog.Modules.GestionMantenimientos.Services;
using Microsoft.Extensions.DependencyInjection;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Serilog;

namespace GestLog.Modules.GestionMantenimientos.ViewModels.Cronograma
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
        private ObservableCollection<MantenimientoSemanaEstadoDto> estadosMantenimientos = new();        private readonly ICronogramaService _cronogramaService;
        private readonly int _anio;

        public SemanaViewModel(int numeroSemana, DateTime fechaInicio, DateTime fechaFin, ICronogramaService cronogramaService, int anio)
        {
            NumeroSemana = numeroSemana;
            FechaInicio = fechaInicio;
            FechaFin = fechaFin;
            _cronogramaService = cronogramaService;
            _anio = anio;
            // Suscribirse a mensajes de actualizaciÃ³n de seguimientos
            WeakReferenceMessenger.Default.Register<SeguimientosActualizadosMessage>(this, async (r, m) => await RecargarEstadosAsync(_anio, _cronogramaService));
        }        public async Task VerSemanaAsync()
        {
            if (Mantenimientos == null || Mantenimientos.Count == 0)
                return;
            
            var estados = await _cronogramaService.GetEstadoMantenimientosSemanaAsync(NumeroSemana, _anio);
            
            // Obtener el servicio de seguimiento usando DI
            var serviceProvider = GestLog.Services.Core.Logging.LoggingService.GetServiceProvider();
            var seguimientoService = serviceProvider.GetRequiredService<GestLog.Modules.GestionMantenimientos.Interfaces.Data.ISeguimientoService>();
            var currentUserService = serviceProvider.GetRequiredService<GestLog.Modules.Usuarios.Interfaces.ICurrentUserService>();
            var currentUser = currentUserService.Current ?? new GestLog.Modules.Usuarios.Models.Authentication.CurrentUserInfo {
                UserId = Guid.Empty,
                Username = "",
                FullName = "",
                Email = "",
                LoginTime = DateTime.Now,
                LastActivity = DateTime.Now,
                Roles = new List<string>(),
                Permissions = new List<string>()
            };            // NUEVA LÃ“GICA: Agregar estados para correctivos que estÃ¡n en esta semana
            var estadosConCorrectivos = new ObservableCollection<MantenimientoSemanaEstadoDto>(estados);
            
            var todosSeguimientos = await seguimientoService.GetSeguimientosAsync();
            var correctivosEnSemana = todosSeguimientos.Where(s => 
                s.Semana == NumeroSemana && 
                s.Anio == _anio &&
                s.TipoMtno == GestLog.Modules.GestionMantenimientos.Models.Enums.TipoMantenimiento.Correctivo
            ).ToList();
              // Agregar TODOS los correctivos encontrados, sin importar duplicados
            // Los correctivos deben coexistir con los preventivos en la misma semana
            foreach (var correctivo in correctivosEnSemana)
            {
                var nuevoEstado = new MantenimientoSemanaEstadoDto
                {
                    CodigoEquipo = correctivo.Codigo ?? "",
                    NombreEquipo = correctivo.Nombre ?? "",
                    Sede = correctivo.Sede,
                    Semana = NumeroSemana,
                    Anio = _anio,
                    Frecuencia = correctivo.Frecuencia,
                    Estado = correctivo.Estado,
                    PuedeRegistrar = true,
                    Seguimiento = correctivo
                };
                estadosConCorrectivos.Add(nuevoEstado);
            }
              var vm = new GestLog.Modules.GestionMantenimientos.ViewModels.Cronograma.SemanaDetalleViewModel(
                $"Semana {NumeroSemana}",
                $"{FechaInicio:dd/MM/yyyy} - {FechaFin:dd/MM/yyyy}",
                estadosConCorrectivos,
                Mantenimientos ?? new ObservableCollection<CronogramaMantenimientoDto>(),
                seguimientoService,
                currentUserService
            );
            
            var dialog = new GestLog.Modules.GestionMantenimientos.Views.Cronograma.SemanaDetalle.SemanaDetalleDialog(vm);
            var ownerWindow = System.Windows.Application.Current?.MainWindow;
            dialog.ConfigurarParaVentanaPadre(ownerWindow);
            dialog.ShowDialog();
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
            // Forzar refresco de color y semana vacÃ­a tras carga asÃ­ncrona
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
        }        // Propiedad calculada para el color de la semana segÃºn los estados de los mantenimientos
        public string ColorSemana
        {
            get
            {
                if (EstadosMantenimientos == null || EstadosMantenimientos.Count == 0)
                    return "Transparent"; // Sin datos, transparente para que no se vea borde
                if (EstadosMantenimientos.Any(m => m.Estado == EstadoSeguimientoMantenimiento.NoRealizado))
                    return "#C80000"; // Rojo fuerte personalizado
                if (EstadosMantenimientos.Any(m => m.Estado == EstadoSeguimientoMantenimiento.Atrasado))
                    return "#FFB300"; // Ãmbar
                if (EstadosMantenimientos.All(m => m.Estado == EstadoSeguimientoMantenimiento.Pendiente))
                    return "#90CAF9"; // Azul clarito para pendientes
                if (EstadosMantenimientos.All(m => m.Estado == EstadoSeguimientoMantenimiento.RealizadoEnTiempo || m.Estado == EstadoSeguimientoMantenimiento.RealizadoFueraDeTiempo))
                    return "#388E3C"; // Verde fuerte
                if (EstadosMantenimientos.Any(m => m.Estado == EstadoSeguimientoMantenimiento.Pendiente) &&
                    EstadosMantenimientos.Any(m => m.Estado == EstadoSeguimientoMantenimiento.RealizadoEnTiempo || m.Estado == EstadoSeguimientoMantenimiento.RealizadoFueraDeTiempo))
                    return "#FFB300"; // Ãmbar
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
        }        // Propiedad que indica si la semana estÃ¡ vacÃ­a (sin estados de mantenimiento)
        public bool EsSemanaVacia => EstadosMantenimientos == null || EstadosMantenimientos.Count == 0;

        // Propiedad que indica si es semana actual Y estÃ¡ vacÃ­a
        public bool IsSemanaActualYVacia => IsSemanaActual && EsSemanaVacia;        // Notificar cambio de color si cambian los estados
        partial void OnEstadosMantenimientosChanged(ObservableCollection<MantenimientoSemanaEstadoDto> value)
        {
            OnPropertyChanged(nameof(ColorSemana));
            OnPropertyChanged(nameof(EsSemanaVacia));
            OnPropertyChanged(nameof(IsSemanaActualYVacia));
        }
    }
}


