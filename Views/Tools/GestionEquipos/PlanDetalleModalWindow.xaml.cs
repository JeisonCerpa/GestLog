using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using GestLog.Modules.GestionEquiposInformaticos.ViewModels;

namespace GestLog.Views.Tools.GestionEquipos
{
    /// <summary>
    /// Ventana modal para mostrar el detalle de un plan de cronograma
    /// </summary>
    public partial class PlanDetalleModalWindow : Window
    {        public PlanDetalleModalWindow()
        {
            InitializeComponent();
            
            // Manejar tecla Escape
            KeyDown += PlanDetalleModalWindow_KeyDown;
        }        public void ConfigurarParaVentanaPadre(System.Windows.Window parentWindow)
        {
            if (parentWindow != null)
            {
                // Establecer la ventana padre
                Owner = parentWindow;
                
                // Si la ventana padre está maximizada, maximizar esta también
                if (parentWindow.WindowState == WindowState.Maximized)
                {
                    WindowState = WindowState.Maximized;
                }
                else
                {
                    // Para ventanas no maximizadas, obtener los bounds de la pantalla
                    var interopHelper = new System.Windows.Interop.WindowInteropHelper(parentWindow);
                    var screen = System.Windows.Forms.Screen.FromHandle(interopHelper.Handle);
                    
                    // Usar los bounds completos de la pantalla
                    var bounds = screen.Bounds;
                    
                    // Configurar para cubrir toda la pantalla
                    Left = bounds.Left;
                    Top = bounds.Top;
                    Width = bounds.Width;
                    Height = bounds.Height;
                    WindowState = WindowState.Normal;
                }
            }
            else
            {
                // Fallback: maximizar en pantalla principal
                WindowState = WindowState.Maximized;
            }
        }private void PlanDetalleModalWindow_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Escape)
            {
                Close();
            }
        }        private void Overlay_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            // Cerrar al hacer clic en el fondo
            Close();
        }

        private void Panel_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            // Evitar que el clic en el panel cierre la ventana
            e.Handled = true;
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
