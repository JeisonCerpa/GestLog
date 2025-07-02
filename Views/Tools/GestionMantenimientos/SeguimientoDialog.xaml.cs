using System.Windows;
using GestLog.Modules.GestionMantenimientos.Models;

namespace GestLog.Views.Tools.GestionMantenimientos
{
    public partial class SeguimientoDialog : Window
    {
        public SeguimientoMantenimientoDto Seguimiento { get; private set; }
        public SeguimientoDialog(SeguimientoMantenimientoDto? seguimiento = null)
        {
            InitializeComponent();
            Seguimiento = seguimiento != null ? new SeguimientoMantenimientoDto(seguimiento) : new SeguimientoMantenimientoDto();
            DataContext = Seguimiento;
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
