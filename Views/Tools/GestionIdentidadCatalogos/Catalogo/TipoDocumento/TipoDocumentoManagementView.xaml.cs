using System.Windows.Controls;

namespace GestLog.Views.Tools.GestionIdentidadCatalogos.Catalogo.TipoDocumento
{
    public partial class TipoDocumentoManagementView : System.Windows.Controls.UserControl
    {
        public TipoDocumentoManagementView()
        {
            InitializeComponent();
        }

        private void EliminarTipoDocumentoDirecto_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            var button = sender as System.Windows.Controls.Button;
            var tipo = button?.Tag as GestLog.Modules.Usuarios.Models.TipoDocumento;
            if (tipo != null)
            {
                var result = System.Windows.MessageBox.Show(
                    $"¿Está seguro que desea eliminar el tipo de documento '{tipo.Nombre}'?",
                    "Confirmar eliminación",
                    System.Windows.MessageBoxButton.YesNo,
                    System.Windows.MessageBoxImage.Warning);
                if (result == System.Windows.MessageBoxResult.Yes)
                {
                    // Si se requiere lógica para el botón de eliminar, debe usarse el ViewModel centralizado:
                    // Eliminar cualquier referencia a TipoDocumentoManagementViewModel y DataContext aquí.
                }
            }
        }
    }
}
