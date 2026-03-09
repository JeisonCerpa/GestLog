using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using GestLog.Modules.Shell.Views;
using GestLog.Services.Core.Logging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using System.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using GestLog.Messages;

namespace GestLog
{

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window, INotifyPropertyChanged
{
    private readonly Stack<(System.Windows.Controls.UserControl view, string title)> _navigationStack;
    private System.Windows.Controls.UserControl? _currentView;
    private readonly IGestLogLogger _logger;
    private bool _isAuthenticated = false;

    public bool IsAuthenticated
    {
        get => _isAuthenticated;
        set
        {
            if (_isAuthenticated != value)
            {
                _isAuthenticated = value;
                OnPropertyChanged(nameof(IsAuthenticated));
            }
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged(string propertyName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    public MainWindow()
    {
        InitializeComponent();
        // Actualizar Title automáticamente desde BuildVersion
        Title = $"GestLog - Sistema de Gestión {BuildVersion.VersionLabel}";
        try
        {
            var serviceProvider = GestLog.Services.Core.Logging.LoggingService.GetServiceProvider();
            var loginViewModel = serviceProvider.GetService(typeof(GestLog.Modules.Usuarios.ViewModels.LoginViewModel)) as GestLog.Modules.Usuarios.ViewModels.LoginViewModel;
            var currentUserService = serviceProvider.GetRequiredService<GestLog.Modules.Usuarios.Interfaces.ICurrentUserService>();
            var mainWindowViewModel = new GestLog.ViewModels.MainWindowViewModel(loginViewModel!, currentUserService);
            DataContext = mainWindowViewModel;
            _navigationStack = new Stack<(System.Windows.Controls.UserControl, string)>();
            _logger = GestLog.Services.Core.Logging.LoggingService.GetLogger<MainWindow>();
            SubscribeToDatabaseStatusChanges();
            var loginView = new GestLog.Modules.Usuarios.Views.Authentication.LoginView();
            loginView.LoginSuccessful += (s, e) =>
            {
                if (DataContext is GestLog.ViewModels.MainWindowViewModel vm)
                {
                    var serviceProvider = GestLog.Services.Core.Logging.LoggingService.GetServiceProvider();
                    var currentUserService = serviceProvider.GetService(typeof(GestLog.Modules.Usuarios.Services.CurrentUserService)) as GestLog.Modules.Usuarios.Services.CurrentUserService;
                    string nombrePersona = currentUserService?.GetCurrentUserFullName() ?? string.Empty;
                    vm.SetAuthenticated(true, nombrePersona);
                    vm.NotificarCambioNombrePersona(); // Forzar actualización del binding
                }
                LoadHomeView();
            };
            contentPanel.Content = loginView;
            _currentView = loginView;
            txtCurrentView.Text = "Login";
            btnBack.Visibility = Visibility.Collapsed;
            if (DataContext is GestLog.ViewModels.MainWindowViewModel vm2)
                vm2.SetAuthenticated(false);
            CommunityToolkit.Mvvm.Messaging.WeakReferenceMessenger.Default.Register<GestLog.Messages.ShowLoginViewMessage>(this, (r, m) => MostrarLoginView());

            // Leer configuración para ventana maximizada
            var configService = GestLog.Services.Core.Logging.LoggingService.GetService<GestLog.Services.Configuration.IConfigurationService>();
            bool startMaximized = configService?.Current?.General?.StartMaximized ?? true;
            this.WindowState = startMaximized ? WindowState.Maximized : WindowState.Normal;
            _logger.LogDebug($"🪟 Ventana configurada para iniciar: {(startMaximized ? "MAXIMIZADA" : "NORMAL")}");
        }
        catch (System.Exception ex)
        {
            var fallbackLogger = GestLog.Services.Core.Logging.LoggingService.GetLogger<MainWindow>();
            fallbackLogger.LogError(ex, "Error al inicializar MainWindow");
            throw;
        }
    }

    public void LoadHomeView()
    {
        try
        {
            _logger.LogUserInteraction("🏠", "LoadHomeView", "Cargando vista principal", true);
            
            using var scope = _logger.BeginOperationScope("LoadHomeView");
            
            var homeView = new HomeView();
            contentPanel.Content = homeView;
            _currentView = homeView;
            txtCurrentView.Text = "Home";
            btnBack.Visibility = Visibility.Collapsed;
            _navigationStack.Clear();
            
            IsAuthenticated = true;
            
            _logger.LogUserInteraction("✅", "LoadHomeView", "Vista Home cargada exitosamente", true);

            // Forzar actualización rápida del estado de base de datos para evitar badges desincronizados
#pragma warning disable CS4014
            RefreshDatabaseStatusAsync(5);
#pragma warning restore CS4014
        }
        catch (System.Exception ex)
        {
            _logger.LogError(ex, "Error al cargar la vista Home");
            throw;
        }
    }

    public void NavigateToView(System.Windows.Controls.UserControl view, string title)
    {
        try
        {
            _logger.LogUserInteraction("🧭", "NavigateToView", $"Navegando a vista: {title}", true);
            
            using var scope = _logger.BeginOperationScope("NavigateToView", new { ViewTitle = title });
            
            // Guardar la vista actual en el stack
            if (_currentView != null)
            {
                _navigationStack.Push((_currentView, txtCurrentView.Text));
                _logger.LogDebug("Vista actual guardada en stack: {PreviousView}", txtCurrentView.Text);
            }

            // Navegar a la nueva vista
            contentPanel.Content = view;
            _currentView = view;
            txtCurrentView.Text = title;
            btnBack.Visibility = Visibility.Visible;
            
            _logger.LogUserInteraction("✅", "NavigateToView", $"Navegación completada a: {title}", true);
        }
        catch (System.Exception ex)
        {
            _logger.LogError(ex, "Error al navegar a la vista: {ViewTitle}", title);
            throw;
        }
    }

    private void btnHome_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            _logger.LogUserInteraction("🏠", "btnHome_Click", "Usuario hizo clic en botón Home");
            LoadHomeView();
        }
        catch (System.Exception ex)
        {
            _logger.LogError(ex, "Error al procesar clic en botón Home");
        }
    }

    private void btnBack_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            _logger.LogUserInteraction("⬅️", "btnBack_Click", "Usuario hizo clic en botón Regresar");
            
            using var scope = _logger.BeginOperationScope("BackNavigation");
            
            if (_navigationStack.Count > 0)
            {
                var (previousView, previousTitle) = _navigationStack.Pop();
                contentPanel.Content = previousView;
                _currentView = previousView;
                txtCurrentView.Text = previousTitle;

                _logger.LogUserInteraction("📍", "btnBack_Click", "Regresando a vista: {PreviousTitle}", previousTitle);

                // Si no hay más vistas en el stack, ocultar el botón Back
                if (_navigationStack.Count == 0)
                {
                    btnBack.Visibility = Visibility.Collapsed;
                    _logger.LogDebug("Stack de navegación vacío, ocultando botón Back");
                }
            }
            else
            {
                // Si no hay stack, ir a Home
                _logger.LogDebug("Stack de navegación vacío, cargando vista Home");
                LoadHomeView();
            }        
        }
        catch (System.Exception ex)
        {
            _logger.LogError(ex, "Error al procesar navegación hacia atrás");
        }
    }

    // Método para compatibilidad con código existente
    public void SetContent(System.Windows.Controls.UserControl control)
    {
        try
        {
            var controlType = control?.GetType().Name ?? "Unknown";
            _logger.LogUserInteraction("🔄", "SetContent", "Estableciendo contenido: {ControlType}", controlType);
            
            contentPanel.Content = control;
            _currentView = control;
            
            _logger.LogDebug("Contenido establecido exitosamente: {ControlType}", controlType);
        }
        catch (System.Exception ex)
        {
            _logger.LogError(ex, "Error al establecer contenido");
            throw;
        }
    }        protected override void OnClosed(System.EventArgs e)
    {
        try
        {
            _logger.LogApplicationStarted("MainWindow cerrada por el usuario");
            
            // Forzar cierre de la aplicación cuando se cierra la MainWindow
            System.Windows.Application.Current.Shutdown();
            
            base.OnClosed(e);
        }
        catch (System.Exception ex)
        {
            // En caso de error al cerrar, registrar pero no lanzar excepción
            var fallbackLogger = LoggingService.GetLogger<MainWindow>();
            fallbackLogger.LogError(ex, "Error al cerrar MainWindow");
            
            // Aún así, forzar cierre
            System.Windows.Application.Current.Shutdown();
        }
    }

    #region Database Status Management

    /// <summary>
    /// Suscribe a los cambios de estado de la base de datos para actualizar el indicador visual
    /// </summary>
    private void SubscribeToDatabaseStatusChanges()
    {
        try
        {
            // El ViewModel ahora gestiona la suscripción y health-checks.
            // Sólo actualizamos el indicador inicial si el servicio no está disponible.
            var databaseService = LoggingService.GetService<GestLog.Services.Interfaces.IDatabaseConnectionService>();
            if (databaseService == null)
            {
                _logger.LogWarning("⚠️ Servicio de base de datos no disponible al suscribirse a estado");
                if (DataContext is GestLog.ViewModels.MainWindowViewModel vm)
                {
                    vm.UpdateDatabaseStatusFromState(GestLog.Models.Events.DatabaseConnectionState.Unknown, "Servicio no disponible");
                }
                return;
            }

            // Forzar un refresh rápido desde el ViewModel al iniciar
            if (DataContext is GestLog.ViewModels.MainWindowViewModel vm2)
            {
#pragma warning disable CS4014
                vm2.InitializeDatabaseStatusAsyncProxy();
#pragma warning restore CS4014
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al suscribirse a cambios de estado de base de datos");
            // Mostrar estado desconocido si no se puede conectar al servicio
            if (DataContext is GestLog.ViewModels.MainWindowViewModel vm)
            {
                vm.UpdateDatabaseStatusFromState(GestLog.Models.Events.DatabaseConnectionState.Unknown, "Error de servicio");
            }
        }
    }

    /// <summary>
    /// Forzar un health-check rápido y actualizar el indicador de BD en la UI
    /// </summary>
    private async System.Threading.Tasks.Task RefreshDatabaseStatusAsync(int timeoutSeconds = 5)
    {
        // Este método se queda por compatibilidad pero delega al ViewModel si existe
        try
        {
            if (DataContext is GestLog.ViewModels.MainWindowViewModel vm)
            {
                await vm.InitializeDatabaseStatusAsyncProxy(timeoutSeconds);
                return;
            }

            var databaseService = LoggingService.GetService<GestLog.Services.Interfaces.IDatabaseConnectionService>();
            if (databaseService == null)
            {
                var op1 = Dispatcher.BeginInvoke(new System.Action(() =>
                {
                    UpdateDatabaseStatusIndicator(GestLog.Models.Events.DatabaseConnectionState.Unknown, "Servicio no disponible");
                }));
                await op1.Task; // await la operación del dispatcher
                return;
            }

            using var cts = new System.Threading.CancellationTokenSource(TimeSpan.FromSeconds(timeoutSeconds));
            bool healthy = await databaseService.ForceHealthCheckAsync(cts.Token);

            var state = healthy ? GestLog.Models.Events.DatabaseConnectionState.Connected : GestLog.Models.Events.DatabaseConnectionState.Error;
            var op2 = Dispatcher.BeginInvoke(new System.Action(() =>
            {
                UpdateDatabaseStatusIndicator(state, healthy ? "Health check OK" : "Health check falló");
            }));
            await op2.Task;
        }
        catch (OperationCanceledException)
        {
            var op3 = Dispatcher.BeginInvoke(new System.Action(() =>
            {
                UpdateDatabaseStatusIndicator(GestLog.Models.Events.DatabaseConnectionState.Error, "Health check timeout");
            }));
            await op3.Task;
            _logger.LogWarning("⚠️ Timeout en RefreshDatabaseStatusAsync");
        }
        catch (System.Exception ex)
        {
            var op4 = Dispatcher.BeginInvoke(new System.Action(() =>
            {
                UpdateDatabaseStatusIndicator(GestLog.Models.Events.DatabaseConnectionState.Error, $"Health check error: {ex.Message}");
            }));
            await op4.Task;
            _logger.LogWarning(ex, "⚠️ Error en RefreshDatabaseStatusAsync");
        }
    }

    /// <summary>
    /// Actualiza el indicador visual de estado de base de datos
    /// </summary>
    private void UpdateDatabaseStatusIndicator(GestLog.Models.Events.DatabaseConnectionState state, string message)
    {
        try
        {
            // Mantener como fallback para llamadas directas internas, pero la UI ahora está ligada al ViewModel
            if (DataContext is GestLog.ViewModels.MainWindowViewModel vm)
            {
                vm.UpdateDatabaseStatusFromState(state, message);
                return;
            }

            string icon, text, backgroundColor, tooltip;            switch (state)
            {                case GestLog.Models.Events.DatabaseConnectionState.Connected:
                    icon = "✅";
                    text = "Conectado";
                    backgroundColor = "#2B8E3F"; // Verde unificado con botones de navegación
                    tooltip = $"Conectado a base de datos - {message}";
                    break;

                case GestLog.Models.Events.DatabaseConnectionState.Connecting:
                    icon = "🔄";
                    text = "Conectando...";
                    backgroundColor = "#E67E22"; // Naranja 
                    tooltip = $"Conectando a base de datos - {message}";
                    break;

                case GestLog.Models.Events.DatabaseConnectionState.Reconnecting:
                    icon = "🔄";
                    text = "Reconectando...";
                    backgroundColor = "#D68910"; // Naranja oscuro
                    tooltip = $"Reconectando a base de datos - {message}";
                    break;

                case GestLog.Models.Events.DatabaseConnectionState.Disconnected:
                    icon = "⏸️";
                    text = "Desconectado";
                    backgroundColor = "#706F6F"; // Gris medio de la paleta
                    tooltip = $"Desconectado de base de datos - {message}";
                    break;

                case GestLog.Models.Events.DatabaseConnectionState.Error:
                    icon = "❌";
                    text = "Error";
                    backgroundColor = "#C0392B"; // Rojo de la paleta
                    tooltip = $"Error de conexión a base de datos - {message}";
                    break;

                default:
                    icon = "❓";
                    text = "Desconocido";
                    backgroundColor = "#9D9D9C"; // Gris claro de la paleta
                    tooltip = $"Estado desconocido de base de datos - {message}";
                    break;
            }

            // Actualizar los elementos de UI si el ViewModel no existe
            DatabaseStatusIcon.Text = icon;
            DatabaseStatusText.Text = text;
            DatabaseStatusBorder.Background = new System.Windows.Media.SolidColorBrush(
                (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(backgroundColor));
            DatabaseStatusBorder.ToolTip = tooltip;

            _logger.LogDebug("🔄 Indicador de BD actualizado: {State} - {Message}", state, message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al actualizar indicador de estado de base de datos");
        }
    }

    #endregion

    public ContentControl MainContent => contentPanel;

    private async void btnCerrarSesion_Click(object sender, RoutedEventArgs e)
    {
        // Esperar el comando de cierre de sesión (enlazado por Command en XAML)
        if (DataContext is ViewModels.MainWindowViewModel vm && vm.CerrarSesionCommand.CanExecute(null))
        {
            await vm.CerrarSesionCommand.ExecuteAsync(null);
        }
        // Navegar a la vista de login
        MostrarLoginView();
    }

    private void MostrarLoginView()
    {
        if (DataContext is GestLog.ViewModels.MainWindowViewModel vm)
            vm.SetAuthenticated(false);
        var loginView = new GestLog.Modules.Usuarios.Views.Authentication.LoginView();
        loginView.LoginSuccessful += (s, e) =>
        {
            if (DataContext is GestLog.ViewModels.MainWindowViewModel vm2)
            {
                var serviceProvider = GestLog.Services.Core.Logging.LoggingService.GetServiceProvider();
                var currentUserService = serviceProvider.GetService(typeof(GestLog.Modules.Usuarios.Services.CurrentUserService)) as GestLog.Modules.Usuarios.Services.CurrentUserService;
                string nombrePersona = currentUserService?.GetCurrentUserFullName() ?? string.Empty;
                vm2.SetAuthenticated(true, nombrePersona);
                vm2.NotificarCambioNombrePersona(); // Forzar actualización del binding
            }
            LoadHomeView();
        };
        contentPanel.Content = loginView;
        _currentView = loginView;
        txtCurrentView.Text = "Login";
        btnBack.Visibility = Visibility.Collapsed;
    }
} // cierre de la clase MainWindow
} // cierre del namespace GestLog
