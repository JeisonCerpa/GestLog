using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Animation;
using GestLog.Modules.Usuarios.ViewModels;

namespace GestLog.Views.Usuarios
{
    /// <summary>
    /// Vista modal para cambio de contraseña obligatorio en primer login
    /// Abre como Window modal con overlay y spinner de carga
    /// </summary>
    public partial class ChangePasswordModalView : Window
    {
        private System.Windows.Forms.Screen? _lastScreenOwner;
        private Storyboard? _spinnerStoryboard;        public ChangePasswordModalView()
        {
            InitializeComponent();
            
            // Manejar eventos
            this.KeyDown += ChangePasswordModalView_KeyDown;
            this.Loaded += ChangePasswordModalView_Loaded;
            this.Closing += ChangePasswordModalView_Closing;
        }        private void ChangePasswordModalView_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Escape)
            {
                // Solo cerrar sin ejecutar comandos
                // La lógica se manejará en LoginView basada en DialogResult
                this.DialogResult = false;
                this.Close();
            }
        }

        private void Overlay_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            // Solo cerrar sin ejecutar comandos
            // La lógica se manejará en LoginView basada en DialogResult
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
            // Solo cerrar sin ejecutar comandos
            // La lógica se manejará en LoginView basada en DialogResult
            this.DialogResult = false;
            this.Close();
        }private void ChangePasswordModalView_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
        {
            // Solo establecer DialogResult basado en ShowSuccess
            // NO ejecutar comandos aquí para evitar conflictos con el cierre de ventana
            if (this.DataContext is ChangePasswordViewModel viewModel)
            {
                // Si ShowSuccess es true, fue exitoso
                if (viewModel.ShowSuccess)
                {
                    this.DialogResult = true;
                }
                else
                {
                    // Si ShowSuccess es false, no fue exitoso
                    this.DialogResult = false;
                }
            }
        }

        /// <summary>
        /// Configura la ventana modal para abrir sobre una ventana padre
        /// Sigue el estándar de ForgotPasswordModalView
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
        }

        private void ChangePasswordModalView_Loaded(object? sender, RoutedEventArgs e)
        {
            if (this.Owner != null)
            {
                // Sincronizar si el Owner se mueve o redimensiona
                this.Owner.LocationChanged += Owner_SizeOrLocationChanged;
                this.Owner.SizeChanged += Owner_SizeOrLocationChanged;
            }

            // Establecer DataContext ViewModel si es necesario
            if (this.DataContext is ChangePasswordViewModel viewModel)
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
            if (e.PropertyName == nameof(ChangePasswordViewModel.IsLoading))
            {
                if (this.DataContext is ChangePasswordViewModel viewModel)
                {
                    if (viewModel.IsLoading && _spinnerStoryboard != null)
                    {
                        _spinnerStoryboard.Begin(this, HandoffBehavior.SnapshotAndReplace);
                    }
                    else if (_spinnerStoryboard != null)
                    {
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

        /// <summary>
        /// Obtiene la contraseña actual del PasswordBox
        /// (Se accede desde el ViewModel mediante Binding si es necesario)
        /// </summary>
        public string GetCurrentPassword()
        {
            return CurrentPasswordBox.Password;
        }

        /// <summary>
        /// Obtiene la nueva contraseña del PasswordBox
        /// </summary>
        public string GetNewPassword()
        {
            return NewPasswordBox.Password;
        }

        /// <summary>
        /// Obtiene la confirmación de contraseña del PasswordBox
        /// </summary>
        public string GetConfirmPassword()
        {
            return ConfirmPasswordBox.Password;
        }

        /// <summary>
        /// Limpia los PasswordBox después de un cambio exitoso
        /// </summary>
        public void ClearPasswords()
        {
            CurrentPasswordBox.Clear();
            NewPasswordBox.Clear();
            ConfirmPasswordBox.Clear();
        }
    }
}
