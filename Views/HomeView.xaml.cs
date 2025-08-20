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
        {            System.Windows.MessageBox.Show(
                "GestLog v1.0.9\n\n" +
                "Sistema modular de gestión integrada\n" +
                "Desarrollado con .NET 9 y WPF\n\n" +
                "Módulos integrados:\n" +
                "• DaaterProccesor - Procesamiento de datos Excel\n" +
                "• Gestión de Cartera - Estados de cuenta PDF\n" +
                "• Envío de Catálogo - Envío masivo de catálogo\n\n" +
                "Estado: ✅ Operativo\n" +
                "Actualizaciones: ✅ Sistema Velopack completamente funcional\n" +                "Seguridad: 🔐 Privilegios de administrador mediante manifiesto\n" +
                "Comportamiento: 🎯 Detección silenciosa, aplicación con confirmación del usuario",
                "Información del Sistema",
                MessageBoxButton.OK,
                MessageBoxImage.Information
            );
        }
    }
}
