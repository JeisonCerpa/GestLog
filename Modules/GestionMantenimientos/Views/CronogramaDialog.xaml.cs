using System.Windows;
using GestLog.Modules.GestionMantenimientos.Models;

namespace GestLog.Modules.GestionMantenimientos.Views
{
    public partial class CronogramaDialog : Window
    {
        public CronogramaMantenimientoDto Cronograma { get; private set; }
        public CronogramaDialog(CronogramaMantenimientoDto? cronograma = null)
        {
            InitializeComponent();
            Cronograma = cronograma != null ? new CronogramaMantenimientoDto(cronograma) : new CronogramaMantenimientoDto();
            // Deshabilitar edición de Código si es edición
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


