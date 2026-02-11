using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using GestLog.Modules.Usuarios.ViewModels;
using GestLog.Messages; // Asegúrate de tener la referencia correcta para el Messenger
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using GestLog.Modules.Usuarios.Interfaces;
using GestLog.Modules.Usuarios.Models.Authentication;
using GestLog.Services.Interfaces;
using GestLog.Models.Events;
using System.Windows.Media;

namespace GestLog.ViewModels
{
    public partial class MainWindowViewModel : ObservableObject
    {
        private readonly LoginViewModel _loginViewModel;
        private readonly ICurrentUserService _currentUserService = null!;
        private CurrentUserInfo _currentUser;

        [ObservableProperty]
        private bool _isAuthenticated = false;
        [ObservableProperty]
        private string _nombrePersona = string.Empty;        // Propiedad calculada que siempre obtiene el nombre actual
        public string NombrePersonaActual
        {
            get
            {                try
                {
                    var serviceProvider = GestLog.Services.Core.Logging.LoggingService.GetServiceProvider();
                    var currentUserService = serviceProvider.GetRequiredService<GestLog.Modules.Usuarios.Interfaces.ICurrentUserService>();
                    return currentUserService?.Current?.FullName ?? string.Empty;
                }
                catch (Exception ex)
                {
                    var logger = GestLog.Services.Core.Logging.LoggingService.GetLogger<MainWindowViewModel>();
                    logger.Logger.LogError(ex, "Error al obtener el nombre del usuario actual: {Message}", ex.Message);
                    return string.Empty;
                }
            }
        }

        [ObservableProperty]
        private bool canAccessAdminPanel;
        [ObservableProperty]
        private bool canAccessUserManagement;
        [ObservableProperty]
        private bool canAccessErrorLog;

        [ObservableProperty]
        private string _databaseStatusIcon = "❓";

        [ObservableProperty]
        private string _databaseStatusText = "Desconocido";

        [ObservableProperty]
        private SolidColorBrush _databaseStatusBackground = new SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#9D9D9C"));

        [ObservableProperty]
        private string _databaseStatusTooltip = "Estado desconocido";

        // (OnInitialized removed - no implementación parcial requerida)

        public MainWindowViewModel(LoginViewModel loginViewModel, ICurrentUserService currentUserService)
        {
            _loginViewModel = loginViewModel;
            _currentUserService = currentUserService;
            _currentUser = _currentUserService.Current ?? new CurrentUserInfo { Username = string.Empty, FullName = string.Empty };
            RecalcularPermisos();
            _currentUserService.CurrentUserChanged += OnCurrentUserChanged;
            // Suscribirse a eventos y mensajes
            if (_currentUserService != null)
            {
                _currentUserService.CurrentUserChanged += (s, user) =>
                {
                    OnPropertyChanged(nameof(NombrePersonaActual));
                };
            }            WeakReferenceMessenger.Default.Register<UserLoggedInMessage>(this, (r, m) =>
            {
                if (m?.Value != null)
                {
                    OnPropertyChanged(nameof(NombrePersonaActual));
                    IsAuthenticated = true;
                    NotificarCambioNombrePersona();
                }
            });
            // --- NUEVO: Forzar notificación inicial para refrescar el binding en el primer render ---
            OnPropertyChanged(nameof(NombrePersonaActual));

            // Suscribirse al servicio de BD para alimentar el badge
            try
            {
                var databaseService = GestLog.Services.Core.Logging.LoggingService.GetService<GestLog.Services.Interfaces.IDatabaseConnectionService>();
                if (databaseService != null)
                {
                    databaseService.ConnectionStateChanged += OnDatabaseConnectionStateChanged;
                    databaseService.NetworkConnectivityChanged += OnNetworkConnectivityChanged;

                    // Iniciar con el estado conocido y lanzar health-check inicial en background
                    UpdateDatabaseStatusFromState(databaseService.CurrentState, "Inicial");

                    _ = InitializeDatabaseStatusAsync(databaseService);
                }
            }
            catch (System.Exception ex)
            {
                var logger = GestLog.Services.Core.Logging.LoggingService.GetLogger<MainWindowViewModel>();
                logger.Logger.LogWarning(ex, "⚠️ No se pudo suscribir al servicio de base de datos para estado");
                UpdateDatabaseStatusFromState(DatabaseConnectionState.Unknown, "Servicio no disponible");
            }
        }
        [RelayCommand]
        public async Task CerrarSesionAsync()
        {
            await _loginViewModel.CerrarSesionAsync();
            IsAuthenticated = false;
            WeakReferenceMessenger.Default.Send(new ShowLoginViewMessage());
        }        public void SetAuthenticated(bool value, string? nombrePersona = null)
        {
            IsAuthenticated = value;
            if (value)
            {
                if (!string.IsNullOrEmpty(nombrePersona))
                {
                    NombrePersona = nombrePersona;
                }
                else
                {                    // Obtener el nombre desde el servicio de usuario actual
                    var serviceProvider = GestLog.Services.Core.Logging.LoggingService.GetServiceProvider();
                    var currentUserService = serviceProvider.GetRequiredService<GestLog.Modules.Usuarios.Interfaces.ICurrentUserService>();
                    NombrePersona = currentUserService?.Current?.FullName ?? string.Empty;
                }
            }
            else
            {
                NombrePersona = string.Empty;
            }
        }        public void NotificarCambioNombrePersona()
        {
            OnPropertyChanged(nameof(NombrePersonaActual));
        }

        private void OnCurrentUserChanged(object? sender, CurrentUserInfo? user)
        {
            _currentUser = user ?? new CurrentUserInfo { Username = string.Empty, FullName = string.Empty };
            RecalcularPermisos();
        }        private void RecalcularPermisos()
        {
            CanAccessAdminPanel = _currentUser.HasRole("Administrador");
            CanAccessUserManagement = _currentUser.HasPermission("Herramientas.AccederGestionUsuarios");
            CanAccessErrorLog = _currentUser.HasPermission("Herramientas.VerErrorLog");
        }

        public async System.Threading.Tasks.Task InitializeDatabaseStatusAsyncProxy(int timeoutSeconds = 5)
        {
            try
            {
                var databaseService = GestLog.Services.Core.Logging.LoggingService.GetService<GestLog.Services.Interfaces.IDatabaseConnectionService>();
                if (databaseService == null)
                {
                    UpdateDatabaseStatusFromState(DatabaseConnectionState.Unknown, "Servicio no disponible");
                    return;
                }

                using var cts = new System.Threading.CancellationTokenSource(System.TimeSpan.FromSeconds(timeoutSeconds));
                var healthy = await databaseService.ForceHealthCheckAsync(cts.Token);
                var state = healthy ? DatabaseConnectionState.Connected : DatabaseConnectionState.Error;
                UpdateDatabaseStatusFromState(state, healthy ? "Health check inicial OK" : "Health check inicial falló");
            }
            catch (System.OperationCanceledException)
            {
                UpdateDatabaseStatusFromState(DatabaseConnectionState.Error, "Health check timeout");
            }
            catch (System.Exception ex)
            {
                var logger = GestLog.Services.Core.Logging.LoggingService.GetLogger<MainWindowViewModel>();
                logger.Logger.LogWarning(ex, "⚠️ Error en health-check inicial desde ViewModel (proxy)");
                UpdateDatabaseStatusFromState(DatabaseConnectionState.Error, $"Health check error: {ex.Message}");
            }
        }

        public void UpdateDatabaseStatusFromState(DatabaseConnectionState state, string message)
        {
            try
            {
                switch (state)
                {
                    case DatabaseConnectionState.Connected:
                        DatabaseStatusIcon = "✅";
                        DatabaseStatusText = "Conectado";
                        DatabaseStatusBackground = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#2B8E3F"));
                        DatabaseStatusTooltip = $"Conectado a base de datos - {message}";
                        break;

                    case DatabaseConnectionState.Connecting:
                        DatabaseStatusIcon = "🔄";
                        DatabaseStatusText = "Conectando...";
                        DatabaseStatusBackground = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#E67E22"));
                        DatabaseStatusTooltip = $"Conectando a base de datos - {message}";
                        break;

                    case DatabaseConnectionState.Reconnecting:
                        DatabaseStatusIcon = "🔄";
                        DatabaseStatusText = "Reconectando...";
                        DatabaseStatusBackground = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#D68910"));
                        DatabaseStatusTooltip = $"Reconectando a base de datos - {message}";
                        break;

                    case DatabaseConnectionState.Disconnected:
                        DatabaseStatusIcon = "⏸️";
                        DatabaseStatusText = "Desconectado";
                        DatabaseStatusBackground = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#706F6F"));
                        DatabaseStatusTooltip = $"Desconectado de base de datos - {message}";
                        break;

                    case DatabaseConnectionState.Error:
                        DatabaseStatusIcon = "❌";
                        DatabaseStatusText = "Error";
                        DatabaseStatusBackground = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#C0392B"));
                        DatabaseStatusTooltip = $"Error de conexión a base de datos - {message}";
                        break;

                    default:
                        DatabaseStatusIcon = "❓";
                        DatabaseStatusText = "Desconocido";
                        DatabaseStatusBackground = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#9D9D9C"));
                        DatabaseStatusTooltip = $"Estado desconocido de base de datos - {message}";
                        break;
                }

                // Notificar cambios
                OnPropertyChanged(nameof(DatabaseStatusIcon));
                OnPropertyChanged(nameof(DatabaseStatusText));
                OnPropertyChanged(nameof(DatabaseStatusBackground));
                OnPropertyChanged(nameof(DatabaseStatusTooltip));
            }
            catch (System.Exception ex)
            {
                var logger = GestLog.Services.Core.Logging.LoggingService.GetLogger<MainWindowViewModel>();
                logger.Logger.LogWarning(ex, "⚠️ Error actualizando estado de BD en ViewModel (public)");
            }
        }

        private async System.Threading.Tasks.Task InitializeDatabaseStatusAsync(IDatabaseConnectionService databaseService)
        {
            try
            {
                using var cts = new System.Threading.CancellationTokenSource(System.TimeSpan.FromSeconds(5));
                var healthy = await databaseService.ForceHealthCheckAsync(cts.Token);
                var state = healthy ? DatabaseConnectionState.Connected : DatabaseConnectionState.Error;
                UpdateDatabaseStatusFromState(state, healthy ? "Health check inicial OK" : "Health check inicial falló");
            }
            catch (System.OperationCanceledException)
            {
                UpdateDatabaseStatusFromState(DatabaseConnectionState.Error, "Health check timeout");
            }
            catch (System.Exception ex)
            {
                var logger = GestLog.Services.Core.Logging.LoggingService.GetLogger<MainWindowViewModel>();
                logger.Logger.LogWarning(ex, "⚠️ Error en health-check inicial desde ViewModel");
                UpdateDatabaseStatusFromState(DatabaseConnectionState.Error, $"Health check error: {ex.Message}");
            }
        }

        private void OnDatabaseConnectionStateChanged(object? sender, DatabaseConnectionStateChangedEventArgs e)
        {
            UpdateDatabaseStatusFromState(e.CurrentState, e.Message ?? string.Empty);
        }

        private void OnNetworkConnectivityChanged(object? sender, GestLog.Models.Events.NetworkConnectivityChangedEventArgs e)
        {
            // Mostrar disponibilidad de red como parte del tooltip
            var msg = e.IsAvailable ? "Red disponible" : "Red no disponible";
            UpdateDatabaseStatusFromState(e.IsAvailable ? DatabaseConnectionState.Reconnecting : DatabaseConnectionState.Error, msg);
        }
    }
}