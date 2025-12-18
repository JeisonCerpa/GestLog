using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GestLog.Modules.GestionEquiposInformaticos.ViewModels;
using GestLog.Modules.GestionEquiposInformaticos.ViewModels.Cronograma;
using GestLog.Modules.GestionEquiposInformaticos.ViewModels.Mantenimiento;
using GestLog.Modules.GestionEquiposInformaticos.ViewModels.Perifericos;
using System;
using System.Threading.Tasks;
using System.Windows;

namespace GestLog.Modules.GestionEquiposInformaticos.ViewModels.Equipos
{    
    public partial class GestionEquiposHomeViewModel : ObservableObject
    {
        public GestionEquiposHomeViewModel(
            CronogramaDiarioViewModel cronogramaVm, 
            HistorialEjecucionesViewModel historialVm, 
            PerifericosViewModel perifericosVm,
            MantenimientosCorrectivosViewModel mantenimientosCorrectivosVm)
        {
            CronogramaVm = cronogramaVm;
            HistorialVm = historialVm;
            PerifericosVm = perifericosVm;
            MantenimientosCorrectivosVm = mantenimientosCorrectivosVm;
        }

        public CronogramaDiarioViewModel CronogramaVm { get; }
        public HistorialEjecucionesViewModel HistorialVm { get; }
        public PerifericosViewModel PerifericosVm { get; }
        public MantenimientosCorrectivosViewModel MantenimientosCorrectivosVm { get; }

        [RelayCommand]
        public async Task AbrirCrearMantenimientoAsync()
        {
            try
            {
                var window = new GestLog.Modules.GestionEquiposInformaticos.Views.Mantenimiento.CrearMantenimientoCorrectivoWindow();
                var owner = System.Windows.Application.Current?.MainWindow;
                window.ConfigurarParaVentanaPadre(owner);

                var result = window.ShowDialog();
                if (result == true)
                {
                    // Si se creó con éxito, refrescar la lista
                    if (MantenimientosCorrectivosVm != null)
                        await MantenimientosCorrectivosVm.RefreshAsync();
                }
            }
            catch (Exception)
            {
                // No bloquear UI; logueo si procede
            }
        }
    }
}
