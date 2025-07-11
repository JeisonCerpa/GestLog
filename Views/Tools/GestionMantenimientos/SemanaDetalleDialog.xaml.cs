using System.Windows;
using GestLog.Modules.GestionMantenimientos.Models;
using System.Collections.ObjectModel;

namespace GestLog.Views.Tools.GestionMantenimientos
{
    public partial class SemanaDetalleDialog : Window
    {
        public SemanaDetalleDialog(GestLog.Modules.GestionMantenimientos.ViewModels.SemanaDetalleViewModel vm)
        {
            InitializeComponent();
            DataContext = vm;
        }

        private void OnCerrar_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
