// filepath: e:\Softwares\GestLog\Views\Tools\GestionEquipos\RegistroEjecucionPlanDialog.xaml.cs
using System;
using System.Windows;
using System.Windows.Input;
using GestLog.Modules.GestionEquiposInformaticos.ViewModels;

namespace GestLog.Views.Tools.GestionEquipos
{
    public partial class RegistroEjecucionPlanDialog : Window
    {
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
        }

        public void ConfigurarParaVentanaPadre(System.Windows.Window? parentWindow)
        {
            if (parentWindow != null)
            {
                Owner = parentWindow;

                if (parentWindow.WindowState == WindowState.Maximized)
                {
                    WindowState = WindowState.Maximized;
                }
                else
                {
                    var interopHelper = new System.Windows.Interop.WindowInteropHelper(parentWindow);
                    var screen = System.Windows.Forms.Screen.FromHandle(interopHelper.Handle);
                    var bounds = screen.Bounds;
                    Left = bounds.Left;
                    Top = bounds.Top;
                    Width = bounds.Width;
                    Height = bounds.Height;
                    WindowState = WindowState.Normal;
                }
            }
            else
            {
                WindowState = WindowState.Maximized;
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
