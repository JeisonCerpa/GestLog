using CommunityToolkit.Mvvm.ComponentModel;
using GestLog.Services.Core.Logging;
using GestLog.Services.Interfaces;
using GestLog.Models.Events;
using System;
using System.Threading.Tasks;

namespace GestLog.ViewModels.Base
{
    /// <summary>
    /// ViewModel base que implementa auto-refresh automático cuando vuelve la conexión a BD
    /// Todos los ViewModels que usen base de datos deben heredar de esta clase
    /// </summary>
    public abstract partial class DatabaseAwareViewModel : ObservableObject, IDisposable
    {
        private readonly IDatabaseConnectionService _databaseService;
        protected readonly IGestLogLogger _logger;
        protected bool _disposed = false;

        [ObservableProperty]
        protected string _statusMessage = "Listo";

        [ObservableProperty]
        protected bool _isLoading = false;        /// <summary>
        /// Constructor base para ViewModels que usan base de datos
        /// </summary>
        protected DatabaseAwareViewModel(IDatabaseConnectionService databaseService, IGestLogLogger logger)
        {
            _databaseService = databaseService ?? throw new ArgumentNullException(nameof(databaseService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            
            // Suscribirse automáticamente al auto-refresh
            _databaseService.ConnectionStateChanged += OnDatabaseConnectionStateChanged;
            
            // Log reducido - solo Debug para evitar ruido
            _logger.LogDebug("[{ViewModelType}] ViewModel inicializado", GetType().Name);
        }

        /// <summary>
        /// Método abstracto que debe implementar cada ViewModel para refrescar sus datos
        /// </summary>
        protected abstract Task RefreshDataAsync();

        /// <summary>
        /// Método virtual para manejar cuando se pierde la conexión (override si necesario)
        /// </summary>
        protected virtual void OnConnectionLost()
        {
            StatusMessage = "Sin conexión - Datos no disponibles";
        }        /// <summary>
        /// Maneja automáticamente los cambios de estado de conexión
        /// </summary>
        private void OnDatabaseConnectionStateChanged(object? sender, DatabaseConnectionStateChangedEventArgs e)
        {
            if (_disposed) return;

            switch (e.CurrentState)
            {
                case DatabaseConnectionState.Connected:
                    // Log reducido - solo cuando se reconecta
                    _logger.LogInformation("[{ViewModelType}] Conexión restaurada, actualizando datos", GetType().Name);
                    
                    // Auto-refresh en el hilo de UI
                    System.Windows.Application.Current?.Dispatcher.BeginInvoke(async () =>
                    {
                        try
                        {
                            StatusMessage = "Reconectando...";
                            await RefreshDataAsync();
                            StatusMessage = "Datos actualizados automáticamente";
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "[{ViewModelType}] Error durante auto-refresh", GetType().Name);
                            StatusMessage = "Error al actualizar automáticamente";
                        }
                    });
                    break;

                case DatabaseConnectionState.Disconnected:
                    // Log reducido - solo Warning cuando se pierde conexión
                    _logger.LogWarning("[{ViewModelType}] Conexión perdida", GetType().Name);
                    System.Windows.Application.Current?.Dispatcher.BeginInvoke(() =>
                    {
                        OnConnectionLost();
                    });
                    break;
            }
        }        /// <summary>
        /// Cleanup automático - llamar desde las vistas o usar IDisposable
        /// </summary>
        public virtual void Dispose()
        {
            if (_disposed) return;

            try
            {
                _databaseService.ConnectionStateChanged -= OnDatabaseConnectionStateChanged;
                _logger.LogDebug("[{ViewModelType}] ViewModel disposed", GetType().Name);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[{ViewModelType}] Error durante dispose", GetType().Name);
            }
            finally
            {
                _disposed = true;
            }
        }
    }
}
