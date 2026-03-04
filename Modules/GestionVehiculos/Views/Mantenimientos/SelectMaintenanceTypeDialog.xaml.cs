using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Controls;

namespace GestLog.Modules.GestionVehiculos.Views.Mantenimientos
{
    public partial class SelectMaintenanceTypeDialog : Window
    {
        public string? SelectedType { get; private set; }

        public SelectMaintenanceTypeDialog()
        {
            InitializeComponent();

            // Configurar como modal maximizado sobre la ventana padre
            var ownerWindow = System.Windows.Application.Current?.MainWindow;
            if (ownerWindow != null)
                ConfigurarParaVentanaPadre(ownerWindow);

            // Manejar Escape para cerrar
            this.KeyDown += (s, e) =>
            {
                if (e.Key == Key.Escape)
                {
                    DialogResult = false;
                    Close();
                }
            };
        }

        /// <summary>
        /// Configura la ventana como modal maximizado, asegurando que el overlay cubra toda la pantalla
        /// </summary>
        private void ConfigurarParaVentanaPadre(Window parentWindow)
        {
            this.Owner = parentWindow;
            this.ShowInTaskbar = false;
            this.WindowState = WindowState.Maximized;
        }

        private void BtnCancelar_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void Overlay_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // Cerrar al hacer clic en el overlay oscuro (solo RootGrid)
            if (sender is Grid grid && grid.Name == "RootGrid")
            {
                e.Handled = true;
                DialogResult = false;
                Close();
            }
        }

        private void Panel_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // Evitar que el clic en el panel principal dispare el cierre del overlay
            e.Handled = true;
        }

        private void BtnPreventivo_Click(object sender, MouseButtonEventArgs e)
        {
            SelectedType = "preventivo";
            DialogResult = true;
            Close();
        }

        private void BtnCorrectivo_Click(object sender, MouseButtonEventArgs e)
        {
            SelectedType = "correctivo";
            DialogResult = true;
            Close();
        }
    }
}
