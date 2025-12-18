using GestLog.Modules.GestionEquiposInformaticos.ViewModels.Mantenimiento;
using System;
using System.Windows;

namespace GestLog.Modules.GestionEquiposInformaticos.Views.Mantenimiento
{
    /// <summary>
    /// Interaction logic for DetallesMantenimientoWindow.xaml
    /// </summary>
    public partial class DetallesMantenimientoWindow : Window
    {
        private System.Windows.Forms.Screen? _lastScreenOwner;

        public DetallesMantenimientoWindow()
        {
            InitializeComponent();

            this.KeyDown += DetallesMantenimientoWindow_KeyDown;
            this.Loaded += DetallesMantenimientoWindow_Loaded;
            this.Closing += DetallesMantenimientoWindow_Closing;
        }

        private void DetallesMantenimientoWindow_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Escape)
            {
                DialogResult = false;
                Close();
            }
        }

        private void Overlay_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            // Cerrar ventana al hacer clic en el overlay
            if (e.Source == RootGrid)
            {
                DialogResult = false;
                Close();
            }
        }

        private void Panel_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            // Permitir drag de la ventana
            if (e.ClickCount == 1)
            {
                try
                {
                    DragMove();
                }
                catch { }
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        public void ConfigurarParaVentanaPadre(Window? parentWindow)
        {
            if (parentWindow == null) return;

            Owner = parentWindow;
            ShowInTaskbar = false;

            try
            {
                var interopHelper = new System.Windows.Interop.WindowInteropHelper(parentWindow);
                var screen = System.Windows.Forms.Screen.FromHandle(interopHelper.Handle);
                _lastScreenOwner = screen;
                WindowState = WindowState.Maximized;
            }
            catch
            {
                WindowState = WindowState.Maximized;
            }
        }

        private void DetallesMantenimientoWindow_Loaded(object? sender, RoutedEventArgs e)
        {
            if (Owner != null)
            {
                Owner.LocationChanged += Owner_SizeOrLocationChanged;
                Owner.SizeChanged += Owner_SizeOrLocationChanged;
            }
        }

        private void Owner_SizeOrLocationChanged(object? sender, EventArgs e)
        {
            if (Owner == null) return;

            var ownerScreen = System.Windows.Forms.Screen.FromHandle(new System.Windows.Interop.WindowInteropHelper(Owner).Handle);
            if (_lastScreenOwner != ownerScreen)
            {
                _lastScreenOwner = ownerScreen;
                CenterOnOwner();
            }
        }

        private void CenterOnOwner()
        {
            if (Owner == null) return;

            var ownerScreen = System.Windows.Forms.Screen.FromHandle(new System.Windows.Interop.WindowInteropHelper(Owner).Handle);
            var screenArea = ownerScreen.WorkingArea;

            var ownerRect = Owner.RestoreBounds;
            Left = ownerRect.Left + (ownerRect.Width - Width) / 2;
            Top = ownerRect.Top + (ownerRect.Height - Height) / 2;

            if (Left < screenArea.Left) Left = screenArea.Left + 20;
            if (Top < screenArea.Top) Top = screenArea.Top + 20;
            if (Left + Width > screenArea.Right) Left = screenArea.Right - Width - 20;
            if (Top + Height > screenArea.Bottom) Top = screenArea.Bottom - Height - 20;
        }

        private void DetallesMantenimientoWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
        {
            if (Owner != null)
            {
                Owner.LocationChanged -= Owner_SizeOrLocationChanged;
                Owner.SizeChanged -= Owner_SizeOrLocationChanged;
            }
        }
    }
}
