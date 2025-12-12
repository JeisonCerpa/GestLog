using GestLog.Modules.GestionEquiposInformaticos.ViewModels.Mantenimiento;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using GestLog.Modules.GestionEquiposInformaticos.ViewModels.Equipos;

namespace GestLog.Modules.GestionEquiposInformaticos.Views.Mantenimiento
{
    public partial class HistorialEjecucionesView : System.Windows.Controls.UserControl
    {
        private bool _autoLoaded;
        public HistorialEjecucionesView()
        {
            InitializeComponent();
        }

        private async void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            if (_autoLoaded) return;
            if (DataContext is HistorialEjecucionesViewModel vm && vm.Items.Count == 0)
            {
                _autoLoaded = true;
                try
                {
                    await vm.LoadAsync();
                }
                finally
                {
                    // no-op
                }
            }
        }
    }
}

