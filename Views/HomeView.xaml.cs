using System.Windows;
using System.Windows.Controls;
using GestLog.Views.Tools;

namespace GestLog.Views
{    /// <summary>
    /// Interaction logic for HomeView.xaml
    /// </summary>
    public partial class HomeView : System.Windows.Controls.UserControl
    {
        private MainWindow? _mainWindow;

        public HomeView()
        {
            InitializeComponent();
            _mainWindow = System.Windows.Application.Current.MainWindow as MainWindow;
        }

        private void btnIrHerramientas_Click(object sender, RoutedEventArgs e)
        {
            var herramientasView = new HerramientasView();
            _mainWindow?.NavigateToView(herramientasView, "Herramientas");
        }        private void btnInfo_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.MessageBox.Show(
                "GestLog v1.0.43\n\n" +
                "Sistema de gestión integrada.\n" +
                "Desarrollado con .NET 9 y WPF\n\n" +
                "Cambios principales en v1.0.43 (15-01-2026):\n\n" +
                "Corregidos:\n" +
                "• Botones duplicados en vista de Seguimientos\n" +
                "• Caracteres UTF-8 corruptos en Gestión de Mantenimientos y Equipos\n" +
                "• Duplicación de mantenimientos correctivos en vista semanal\n" +
                "• Lógica de KPIs en análisis de cumplimiento por estado\n\n" +
                "Mejorado:\n" +
                "• Anchos de columnas en hoja de Seguimientos (automáticos)\n" +
                "• Colores de badges en vista de Detalle de Semana\n" +
                "• Ajuste de columnas en DataGrid de Detalle de Semana\n" +
                "• Nuevo SeguimientosExportService para exportación independiente\n\n" +
                "Estado: ✅ Operativo\n" +
                "Actualizaciones: ✅ Sistema Velopack completamente funcional",
                "Información del Sistema",
                MessageBoxButton.OK,
                MessageBoxImage.Information
            );
        }
    }
}
