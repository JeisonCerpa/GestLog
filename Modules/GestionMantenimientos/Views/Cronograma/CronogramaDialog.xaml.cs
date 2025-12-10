using System.Windows;
using GestLog.Modules.GestionMantenimientos.Models;
using GestLog.Modules.GestionMantenimientos.Models.DTOs;

namespace GestLog.Modules.GestionMantenimientos.Views.Cronograma
{
    public partial class CronogramaDialog : Window
    {
        public CronogramaMantenimientoDto Cronograma { get; private set; }
        public CronogramaDialog(CronogramaMantenimientoDto? cronograma = null)
        {
            InitializeComponent();
            Cronograma = cronograma != null ? new CronogramaMantenimientoDto(cronograma) : new CronogramaMantenimientoDto();
            // Deshabilitar ediciÃ³n de CÃ³digo si es ediciÃ³n
            if (cronograma != null)
            {
                Cronograma.IsCodigoReadOnly = true;
                Cronograma.IsCodigoEnabled = false;
            }
            else
            {
                Cronograma.IsCodigoReadOnly = false;
                Cronograma.IsCodigoEnabled = true;
            }
            DataContext = Cronograma;
        }
        private void Aceptar_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }
        private void Cancelar_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}



