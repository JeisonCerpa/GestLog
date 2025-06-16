using GestLog.Models.Configuration;
using GestLog.Models.Enums;
using GestLog.Models.Events;
using GestLog.Services.Core.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace GestLog.Services.Resilience;

/// <summary>
/// Servicio para monitoreo de conectividad de red
/// </summary>
public class NetworkMonitoringService : IDisposable
{
    private readonly NetworkConfig _config;
    private readonly IGestLogLogger _logger;
    private readonly System.Threading.Timer? _connectivityTimer;
    
    private NetworkConnectivityState _currentState = NetworkConnectivityState.Unknown;
    private bool _isNetworkAvailable = false;
    private bool _disposed = false;

    public event EventHandler<NetworkConnectivityChangedEventArgs>? ConnectivityChanged;

    public NetworkMonitoringService(IOptions<DatabaseResilienceConfiguration> config, IGestLogLogger logger)
    {
        _config = config.Value.Network;
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        if (_config.EnableNetworkMonitoring)
        {
            InitializeNetworkMonitoring();
        }

        if (_config.EnableInternetConnectivityCheck)
        {
            // Verificar conectividad inicial
            _ = Task.Run(async () => await CheckConnectivityAsync());
            
            // Timer para verificaciones periódicas (cada 30 segundos)
            _connectivityTimer = new System.Threading.Timer(
                async _ => await CheckConnectivityAsync(), 
                null, 
                TimeSpan.FromSeconds(30), 
                TimeSpan.FromSeconds(30));
        }

        _logger.LogInformation("🌐 NetworkMonitoring inicializado - Monitoring: {Monitoring}, Connectivity: {Connectivity}", 
            _config.EnableNetworkMonitoring, _config.EnableInternetConnectivityCheck);
    }

    /// <summary>
    /// Estado actual de conectividad
    /// </summary>
    public NetworkConnectivityState CurrentState => _currentState;

    /// <summary>
    /// Indica si la red está disponible
    /// </summary>
    public bool IsNetworkAvailable => _isNetworkAvailable;

    /// <summary>
    /// Inicializa el monitoreo de eventos de red
    /// </summary>
    private void InitializeNetworkMonitoring()
    {
        try
        {
            NetworkChange.NetworkAvailabilityChanged += OnNetworkAvailabilityChanged;
            NetworkChange.NetworkAddressChanged += OnNetworkAddressChanged;
            
            // Estado inicial
            _isNetworkAvailable = NetworkInterface.GetIsNetworkAvailable();
            _currentState = _isNetworkAvailable ? NetworkConnectivityState.Available : NetworkConnectivityState.Unavailable;
            
            _logger.LogDebug("🌐 Eventos de red registrados - Estado inicial: {State}", _currentState);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "⚠️ Error al inicializar monitoreo de red");
        }
    }

    /// <summary>
    /// Maneja cambios de disponibilidad de red
    /// </summary>
    private async void OnNetworkAvailabilityChanged(object? sender, NetworkAvailabilityEventArgs e)
    {
        try
        {
            var previousState = _currentState;
            var newState = e.IsAvailable ? NetworkConnectivityState.Available : NetworkConnectivityState.Unavailable;
            
            _isNetworkAvailable = e.IsAvailable;
            _currentState = newState;

            _logger.LogInformation("🌐 Red {Status} - {Previous} → {New}", 
                e.IsAvailable ? "disponible" : "no disponible", previousState, newState);

            // Delay antes de notificar para evitar spam de eventos
            await Task.Delay(_config.NetworkChangeDelay);
            
            ConnectivityChanged?.Invoke(this, new NetworkConnectivityChangedEventArgs(
                previousState, newState, e.IsAvailable));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error procesando cambio de disponibilidad de red");
        }
    }

    /// <summary>
    /// Maneja cambios de dirección de red
    /// </summary>
    private async void OnNetworkAddressChanged(object? sender, EventArgs e)
    {
        try
        {
            _logger.LogDebug("🌐 Dirección de red cambiada");
            
            // Delay antes de verificar conectividad
            await Task.Delay(_config.NetworkChangeDelay);
            
            // Verificar conectividad tras cambio de dirección
            await CheckConnectivityAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error procesando cambio de dirección de red");
        }
    }

    /// <summary>
    /// Verifica conectividad a internet
    /// </summary>
    private async Task CheckConnectivityAsync()
    {
        if (!_config.EnableInternetConnectivityCheck)
            return;

        try
        {
            var previousState = _currentState;
            var isConnected = await TestInternetConnectivityAsync();
            
            var newState = isConnected ? NetworkConnectivityState.Available : 
                          NetworkInterface.GetIsNetworkAvailable() ? NetworkConnectivityState.Limited : 
                          NetworkConnectivityState.Unavailable;

            if (newState != _currentState)
            {
                _currentState = newState;
                _logger.LogInformation("🌐 Conectividad verificada: {Previous} → {New}", previousState, newState);
                
                ConnectivityChanged?.Invoke(this, new NetworkConnectivityChangedEventArgs(
                    previousState, newState, isConnected));
            }
        }        catch (Exception ex)
        {
            _logger.LogDebug("Error verificando conectividad a internet: {Error}", ex.Message);
        }
    }

    /// <summary>
    /// Prueba conectividad a internet
    /// </summary>
    private async Task<bool> TestInternetConnectivityAsync()
    {
        try
        {
            using var client = new TcpClient();
            var connectTask = client.ConnectAsync(_config.ConnectivityCheckHost, _config.ConnectivityCheckPort);
            var timeoutTask = Task.Delay(_config.ConnectivityCheckTimeout);

            var completedTask = await Task.WhenAny(connectTask, timeoutTask);
            
            if (completedTask == connectTask && client.Connected)
            {
                return true;
            }

            return false;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Fuerza una verificación de conectividad
    /// </summary>
    public async Task<bool> ForceConnectivityCheckAsync()
    {
        _logger.LogDebug("🌐 Verificación forzada de conectividad");
        await CheckConnectivityAsync();
        return _currentState == NetworkConnectivityState.Available;
    }

    /// <summary>
    /// Espera hasta que la red esté disponible
    /// </summary>
    public async Task<bool> WaitForNetworkAsync(TimeSpan timeout, CancellationToken cancellationToken = default)
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(timeout);

        while (!cts.Token.IsCancellationRequested)
        {
            if (_currentState == NetworkConnectivityState.Available)
            {
                return true;
            }

            await Task.Delay(1000, cts.Token);
        }

        return false;
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            try
            {
                if (_config.EnableNetworkMonitoring)
                {
                    NetworkChange.NetworkAvailabilityChanged -= OnNetworkAvailabilityChanged;
                    NetworkChange.NetworkAddressChanged -= OnNetworkAddressChanged;
                }

                _connectivityTimer?.Dispose();
                
                _logger.LogDebug("🌐 NetworkMonitoring disposed");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "⚠️ Error al disposed NetworkMonitoring");
            }
            finally
            {
                _disposed = true;
            }
        }
    }
}
