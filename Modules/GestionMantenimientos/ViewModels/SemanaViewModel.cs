using System;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GestLog.Modules.GestionMantenimientos.Models;

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

        public SemanaViewModel(int numeroSemana, DateTime fechaInicio, DateTime fechaFin)
        {
            NumeroSemana = numeroSemana;
            FechaInicio = fechaInicio;
            FechaFin = fechaFin;
        }

        [RelayCommand]
        public void VerSemana()
        {
            if (Mantenimientos == null || Mantenimientos.Count == 0)
                return;
            var vm = new GestLog.Views.Tools.GestionMantenimientos.SemanaDetalleViewModel(
                $"Semana {NumeroSemana}",
                $"{FechaInicio:dd/MM/yyyy} - {FechaFin:dd/MM/yyyy}",
                Mantenimientos
            );
            var dialog = new GestLog.Views.Tools.GestionMantenimientos.SemanaDetalleDialog(vm);
            dialog.ShowDialog();
        }
    }
}
