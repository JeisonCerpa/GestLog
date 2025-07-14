using System;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GestLog.Modules.GestionMantenimientos.Interfaces;
using GestLog.Modules.GestionMantenimientos.Models;
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
        }

        public void RefrescarEstados(IList<MantenimientoSemanaEstadoDto> nuevosEstados)
        {
            EstadosMantenimientos.Clear();
            foreach (var estado in nuevosEstados)
            {
                EstadosMantenimientos.Add(estado);
            }
        }

        public async Task CargarEstadosMantenimientosAsync(int anio, ICronogramaService cronogramaService)
        {
            var estados = await cronogramaService.GetEstadoMantenimientosSemanaAsync(NumeroSemana, anio);
            RefrescarEstados(estados);
        }

        public async Task RecargarEstadosAsync(int anio, ICronogramaService cronogramaService)
        {
            await CargarEstadosMantenimientosAsync(anio, cronogramaService);
        }
    }
}
