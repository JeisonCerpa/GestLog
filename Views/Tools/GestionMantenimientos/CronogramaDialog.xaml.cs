using System.Windows;
using GestLog.Modules.GestionMantenimientos.Models;

namespace GestLog.Views.Tools.GestionMantenimientos
{
    public partial class CronogramaDialog : Window
    {
        public CronogramaMantenimientoDto Cronograma { get; private set; }
        public CronogramaDialog(CronogramaMantenimientoDto? cronograma = null)
        {
            InitializeComponent();
            Cronograma = cronograma != null ? new CronogramaMantenimientoDto(cronograma) : new CronogramaMantenimientoDto();
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
