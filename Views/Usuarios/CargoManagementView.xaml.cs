using System.Windows;
using System.Windows.Controls;
using GestLog.Views.Usuarios;

namespace GestLog.Views.Usuarios
{
    public partial class CargoManagementView : System.Windows.Controls.UserControl
    {
        public CargoManagementView()
        {
            InitializeComponent();
        }

        private void OnRegistrarCargo_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            var window = new CargoRegistroWindow();
            var parentWindow = System.Windows.Window.GetWindow(this);
            if (parentWindow != null)
                window.Owner = parentWindow;
            window.ShowDialog();
        }
    }
}
