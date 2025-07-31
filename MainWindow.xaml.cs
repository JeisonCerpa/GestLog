using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using GestLog.Views;
using GestLog.Services.Core.Logging;
using Microsoft.Extensions.Logging;
using System.ComponentModel;

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
        try
        {
            DataContext = this;
            InitializeComponent();
            _navigationStack = new Stack<(System.Windows.Controls.UserControl, string)>();
            _logger = LoggingService.GetLogger<MainWindow>();
            
            // --- NUEVO: Establecer WindowState según configuración ---
            try
            {
                var configService = LoggingService.GetService<GestLog.Services.Configuration.IConfigurationService>();
                
                // NO CARGAR AQUÍ - solo usar la configuración que ya está disponible
                // El ConfigurationService ya se inicializa y carga automáticamente
                bool startMaximized = configService?.Current?.General?.StartMaximized ?? true;
                
                this.WindowState = startMaximized ? WindowState.Maximized : WindowState.Normal;
                _logger.LogInformation($"🪟 Ventana configurada para iniciar: {(startMaximized ? "MAXIMIZADA" : "NORMAL")}");
            }
            catch (System.Exception ex)
            { 
                _logger.LogWarning(ex, "⚠️ Error al leer configuración de ventana, usando maximizada por defecto");
                this.WindowState = WindowState.Maximized; // Fallback
            }
            // --- FIN NUEVO ---
            
            _logger.LogApplicationStarted("MainWindow inicializada correctamente");
            
            // Suscribirse a cambios de estado de la base de datos
            SubscribeToDatabaseStatusChanges();
            
            // Mostrar LoginView como pantalla inicial si no hay sesión
            var loginView = new Views.Authentication.LoginView();
            loginView.LoginSuccessful += (s, e) =>
            {
                IsAuthenticated = true;
                LoadHomeView();
            };
            contentPanel.Content = loginView;
            _currentView = loginView;
            txtCurrentView.Text = "Login";
            btnBack.Visibility = Visibility.Collapsed;
            IsAuthenticated = false;
            // TODO: Suscribirse a evento de login exitoso para navegar a HomeView
        }
        catch (System.Exception ex)
        {
            // Fallback en caso de que el logger no esté disponible
            var fallbackLogger = LoggingService.GetLogger<MainWindow>();
            fallbackLogger.LogError(ex, "Error al inicializar MainWindow");
            throw;
        }
    }

    private void LoadHomeView()
    {
        try
        {
            _logger.LogUserInteraction("🏠", "LoadHomeView", "Cargando vista principal");
            
            using var scope = _logger.BeginOperationScope("LoadHomeView");
            
            var homeView = new HomeView();
            contentPanel.Content = homeView;
            _currentView = homeView;
            txtCurrentView.Text = "Home";
            btnBack.Visibility = Visibility.Collapsed;
            _navigationStack.Clear();
            
            IsAuthenticated = true;
            
            _logger.LogUserInteraction("✅", "LoadHomeView", "Vista Home cargada exitosamente");
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
            _logger.LogUserInteraction("🧭", "NavigateToView", "Navegando a vista: {ViewTitle}", title);
            
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
            
            _logger.LogUserInteraction("✅", "NavigateToView", "Navegación completada a: {ViewTitle}", title);
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
    }    private void btnConfig_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            _logger.LogUserInteraction("⚙️", "btnConfig_Click", "Usuario hizo clic en botón Configuración");
            LoadConfigurationView();
        }
        catch (System.Exception ex)
        {
            _logger.LogError(ex, "Error al procesar clic en botón Configuración");
        }    }
    
    private void LoadConfigurationView()
    {
        try
        {
            _logger.LogUserInteraction("⚙️", "LoadConfigurationView", "Cargando vista general de configuración");
            
            using var scope = _logger.BeginOperationScope("LoadConfigurationView");
            
            // Primero, cargamos la vista general de configuración
            var configView = new Views.Configuration.ConfigurationView();
            
            // Navegar a la vista de configuración
            NavigateToView(configView, "Configuración");
            
            _logger.LogUserInteraction("⚙️", "LoadConfigurationView", "Vista de configuración cargada correctamente");
        }
        catch (System.Exception ex)
        {
            _logger.LogError(ex, "❌ Error al cargar vista de configuración: {Message}", ex.Message);
            System.Windows.MessageBox.Show(
                $"Error al cargar la configuración: {ex.Message}",
                "Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error
            );
        }
    }
    
    // Método para navegar directamente a la configuración de DaaterProcessor
    public void NavigateToDaaterProcessorConfig()
    {
        try
        {
            _logger.LogUserInteraction("📊", "NavigateToDaaterProcessorConfig", "Navegando a configuración de DaaterProcessor");
            
            using var scope = _logger.BeginOperationScope("NavigateToDaaterProcessorConfig");
            
            // Primero, cargamos la vista general de configuración
            var configView = new Views.Configuration.ConfigurationView();
            
            // Navegar a la vista de configuración
            NavigateToView(configView, "Configuración - DaaterProcessor");
            
            // Cargar directamente la vista de configuración del DaaterProcessor
            configView.LoadDaaterProcessorConfigView();
            
            _logger.LogUserInteraction("📊", "NavigateToDaaterProcessorConfig", "Vista de configuración de DaaterProcessor cargada correctamente");
        }
        catch (System.Exception ex)
        {
            _logger.LogError(ex, "❌ Error al cargar vista de configuración de DaaterProcessor: {Message}", ex.Message);
            System.Windows.MessageBox.Show(
                $"Error al cargar la configuración de DaaterProcessor: {ex.Message}",
                "Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error
            );
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
            var databaseService = LoggingService.GetService<GestLog.Services.Interfaces.IDatabaseConnectionService>();
            databaseService.ConnectionStateChanged += OnDatabaseConnectionStateChanged;
            
            // Actualizar estado inicial
            UpdateDatabaseStatusIndicator(databaseService.CurrentState, "Inicial");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al suscribirse a cambios de estado de base de datos");
            // Mostrar estado desconocido si no se puede conectar al servicio
            UpdateDatabaseStatusIndicator(GestLog.Models.Events.DatabaseConnectionState.Unknown, "Error de servicio");
        }
    }

    /// <summary>
    /// Maneja los cambios de estado de conexión a base de datos
    /// </summary>
    private void OnDatabaseConnectionStateChanged(object? sender, GestLog.Models.Events.DatabaseConnectionStateChangedEventArgs e)
    {
        // Asegurar que la actualización se ejecute en el hilo de UI
        Dispatcher.BeginInvoke(() =>
        {
            UpdateDatabaseStatusIndicator(e.CurrentState, e.Message ?? "");
        });
    }

    /// <summary>
    /// Actualiza el indicador visual de estado de base de datos
    /// </summary>
    private void UpdateDatabaseStatusIndicator(GestLog.Models.Events.DatabaseConnectionState state, string message)
    {
        try
        {
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

            // Actualizar los elementos de UI
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
} // cierre de la clase MainWindow
} // cierre del namespace GestLog
