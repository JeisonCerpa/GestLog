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
            // Si es registro nuevo y no tiene fecha, prellenar con la fecha actual
            if (seguimiento == null || Seguimiento.FechaRealizacion == null)
                Seguimiento.FechaRealizacion = System.DateTime.Now;
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
