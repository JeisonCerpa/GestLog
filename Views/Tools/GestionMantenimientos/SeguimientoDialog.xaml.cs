using System.Windows;
using System.Windows.Data;
using GestLog.Modules.GestionMantenimientos.Models;
using GestLog.Modules.GestionMantenimientos.Models.Enums;

namespace GestLog.Views.Tools.GestionMantenimientos
{
    public partial class SeguimientoDialog : Window
    {
        public SeguimientoMantenimientoDto Seguimiento { get; private set; }
        public bool ModoRestringido { get; }
        public SeguimientoDialog(SeguimientoMantenimientoDto? seguimiento = null, bool modoRestringido = false)
        {
            InitializeComponent();
            Seguimiento = seguimiento != null ? new SeguimientoMantenimientoDto(seguimiento) : new SeguimientoMantenimientoDto();
            ModoRestringido = modoRestringido;
            // Si es registro nuevo y no tiene fecha, prellenar con la fecha actual
            if (seguimiento == null || Seguimiento.FechaRealizacion == null)
                Seguimiento.FechaRealizacion = System.DateTime.Now;
            DataContext = Seguimiento;
            Loaded += (s, e) =>
            {
                var cvs = (CollectionViewSource)this.Resources["TipoMantenimientoFiltrado"];
                cvs.Filter += (sender, args) =>
                {
                    if (args.Item is TipoMantenimiento tipo)
                    {
                        if (ModoRestringido)
                            args.Accepted = tipo == TipoMantenimiento.Correctivo || tipo == TipoMantenimiento.Predictivo;
                        else
                            args.Accepted = true;
                    }
                    else
                    {
                        args.Accepted = false;
                    }
                };
                cvs.View.Refresh();
            };
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
