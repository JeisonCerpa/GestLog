using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Animation;
using GestLog.Modules.Usuarios.ViewModels;

namespace GestLog.Views.Usuarios
{
    /// <summary>
    /// Vista modal para recuperación de contraseña olvidada
    /// Abre como Window modal con overlay y spinner de carga
    /// </summary>
    public partial class ForgotPasswordModalView : Window
    {
        private System.Windows.Forms.Screen? _lastScreenOwner;
        private Storyboard? _spinnerStoryboard;

        public ForgotPasswordModalView()
        {
            InitializeComponent();
            
            // Manejar tecla Escape
            this.KeyDown += ForgotPasswordModalView_KeyDown;
            this.Loaded += ForgotPasswordModalView_Loaded;
        }

        private void ForgotPasswordModalView_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                this.DialogResult = false;
                this.Close();
            }
        }

        private void Overlay_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            // Cerrar al hacer clic en el overlay
            this.DialogResult = false;
            this.Close();
        }

        private void Panel_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            // Evitar que el clic dentro del panel cierre la ventana
            e.Handled = true;
        }

        private void BtnCerrar_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }

        /// <summary>
        /// Configura la ventana modal para abrir sobre una ventana padre
        /// Sigue el estándar PerifericoDetalleView
        /// </summary>
        public void ConfigurarParaVentanaPadre(Window? parentWindow)
        {
            if (parentWindow == null) return;
            
            this.Owner = parentWindow;
            this.ShowInTaskbar = false;

            try
            {
                // Para un overlay modal, siempre maximizar para cubrir toda la pantalla
                var interopHelper = new System.Windows.Interop.WindowInteropHelper(parentWindow);
                var screen = System.Windows.Forms.Screen.FromHandle(interopHelper.Handle);
                _lastScreenOwner = screen;

                this.WindowState = WindowState.Maximized;
            }
            catch
            {
                // Fallback: maximizar en pantalla principal
                this.WindowState = WindowState.Maximized;
            }
        }        private void ForgotPasswordModalView_Loaded(object? sender, RoutedEventArgs e)
        {
            if (this.Owner != null)
            {
                // Sincronizar si el Owner se mueve o redimensiona
                this.Owner.LocationChanged += Owner_SizeOrLocationChanged;
                this.Owner.SizeChanged += Owner_SizeOrLocationChanged;
            }

            // Establecer DataContext ViewModel si es necesario
            if (this.DataContext is ForgotPasswordViewModel viewModel)
            {
                viewModel.SetView(this);
                
                // Suscribirse a cambios de IsLoading para controlar el spinner
                viewModel.PropertyChanged += ViewModel_PropertyChanged;
            }
            
            // Obtener la animación del storyboard
            if (this.Resources.Contains("SpinnerRotation"))
            {
                _spinnerStoryboard = this.Resources["SpinnerRotation"] as Storyboard;
            }
        }

        /// <summary>
        /// Controla la animación del spinner cuando IsLoading cambia
        /// </summary>
        private void ViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ForgotPasswordViewModel.IsLoading))
            {
                if (this.DataContext is ForgotPasswordViewModel viewModel)
                {
                    if (viewModel.IsLoading && _spinnerStoryboard != null)
                    {
                        // Iniciar animación
                        _spinnerStoryboard.Begin(this, HandoffBehavior.SnapshotAndReplace);
                    }
                    else if (_spinnerStoryboard != null)
                    {
                        // Detener animación
                        _spinnerStoryboard.Stop(this);
                    }
                }
            }
        }

        private void Owner_SizeOrLocationChanged(object? sender, System.EventArgs e)
        {
            if (this.Owner == null) return;

            this.Dispatcher.Invoke(() =>
            {
                try
                {
                    this.WindowState = WindowState.Maximized;
                    
                    var interopHelper = new System.Windows.Interop.WindowInteropHelper(this.Owner);
                    var currentScreen = System.Windows.Forms.Screen.FromHandle(interopHelper.Handle);

                    if (_lastScreenOwner == null || !_lastScreenOwner.DeviceName.Equals(currentScreen.DeviceName))
                    {
                        _lastScreenOwner = currentScreen;
                    }
                }
                catch
                {
                    this.WindowState = WindowState.Maximized;
                }
            });
        }
    }
}

