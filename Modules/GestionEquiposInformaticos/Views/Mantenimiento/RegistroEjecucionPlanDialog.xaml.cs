using GestLog.Modules.GestionEquiposInformaticos.ViewModels.Mantenimiento;
// filepath: e:\Softwares\GestLog\Views\Tools\GestionEquipos\RegistroEjecucionPlanDialog.xaml.cs
using System;
using System.Windows;
using System.Windows.Input;
using GestLog.Modules.GestionEquiposInformaticos.ViewModels.Equipos;

namespace GestLog.Modules.GestionEquiposInformaticos.Views.Mantenimiento
{    public partial class RegistroEjecucionPlanDialog : Window
    {
        private System.Windows.Forms.Screen? _lastScreenOwner;
        public bool Guardado { get; private set; }
        public RegistroEjecucionPlanDialog(RegistroEjecucionPlanViewModel vm)
        {
            InitializeComponent();
            DataContext = vm;
            vm.OnEjecucionRegistrada += (s,e)=> { Guardado = true; DialogResult = true; Close(); };

            // Manejar Escape
            KeyDown += RegistroEjecucionPlanDialog_KeyDown;
        }

        private void RegistroEjecucionPlanDialog_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
                Close();
        }        public void ConfigurarParaVentanaPadre(System.Windows.Window? parentWindow)
        {
            if (parentWindow == null) return;
            
            this.Owner = parentWindow;
            this.ShowInTaskbar = false;

            try
            {
                // Guardar referencia a la pantalla actual del owner
                var interopHelper = new System.Windows.Interop.WindowInteropHelper(parentWindow);
                var screen = System.Windows.Forms.Screen.FromHandle(interopHelper.Handle);
                _lastScreenOwner = screen;

                // Para un overlay modal, siempre maximizar para cubrir toda la pantalla
                // Esto evita problemas de DPI, pantallas múltiples y posicionamiento
                this.WindowState = WindowState.Maximized;
            }
            catch
            {
                // Fallback: maximizar en pantalla principal
                this.WindowState = WindowState.Maximized;
            }
        }

        private void Cerrar_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void Overlay_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // Cerrar al hacer clic en el fondo (overlay)
            DialogResult = false;
            Close();
        }

        private void Panel_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // Evitar que clics dentro del panel cierren la ventana
            e.Handled = true;
        }
    }
}

