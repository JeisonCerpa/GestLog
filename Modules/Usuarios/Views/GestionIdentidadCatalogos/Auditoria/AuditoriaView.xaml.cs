using System.Windows;
using System.Windows.Controls;
using GestLog.Modules.Usuarios.ViewModels;

namespace GestLog.Modules.Usuarios.Views.GestionIdentidadCatalogos.Auditoria
{
    public partial class AuditoriaView : System.Windows.Controls.UserControl
    {
        public AuditoriaView()
        {
            InitializeComponent();
            Loaded += async (_, _) =>
            {
                if (DataContext is AuditoriaViewModel vm)
                    await vm.InicializarAsync();
            };
        }

        private void BtnVerDetalle_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as FrameworkElement)?.DataContext is GestLog.Modules.Usuarios.Models.Auditoria evento)
                MostrarDetalle(evento);
        }

        private void Grid_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (GridAuditoria.SelectedItem is GestLog.Modules.Usuarios.Models.Auditoria evento)
                MostrarDetalle(evento);
        }

        private void MostrarDetalle(GestLog.Modules.Usuarios.Models.Auditoria evento)
        {
            var ventana = new AuditoriaDetalleWindow(evento)
            {
                Owner = Window.GetWindow(this)
            };
            ventana.ShowDialog();
        }
    }
}
