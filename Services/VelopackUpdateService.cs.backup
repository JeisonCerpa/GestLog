using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Velopack;
using GestLog.Services.Core.Logging;

namespace GestLog.Services
{
    public class VelopackUpdateService
    {
        private readonly IGestLogLogger _logger;
        private readonly string _updateUrl;

        public VelopackUpdateService(GestLog.Services.Core.Logging.IGestLogLogger logger, string updateUrl)
        {
            _logger = logger;
            _updateUrl = updateUrl;
        }        /// <summary>
        /// Verifica si hay actualizaciones disponibles SIN descargarlas
        /// </summary>
        public async Task<bool> CheckForUpdatesAsync()
        {
            try
            {
                _logger.LogDebug("🔍 Verificando actualizaciones disponibles...");
                var updater = new UpdateManager(_updateUrl);
                var updateInfo = await updater.CheckForUpdatesAsync();
                
                if (updateInfo != null)
                {
                    var currentVersion = updater.CurrentVersion;
                    var availableVersion = updateInfo.TargetFullRelease?.Version;
                    
                    _logger.LogDebug($"📋 Versión actual: {currentVersion}, Versión disponible: {availableVersion}");
                    
                    // Solo devolver true si hay una versión NUEVA disponible
                    if (availableVersion != null && currentVersion != null && availableVersion > currentVersion)
                    {
                        _logger.LogInformation($"✅ Nueva actualización disponible: {availableVersion} (actual: {currentVersion})");
                        return true;
                    }
                    else
                    {
                        _logger.LogDebug($"ℹ️ Ya tienes la versión más reciente: {currentVersion}");
                        return false;
                    }
                }
                else
                {
                    _logger.LogDebug("ℹ️ No se encontró información de actualizaciones en el servidor");
                    return false;
                }
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("VelopackLocator"))
            {
                // Aplicación no está ejecutándose desde Velopack - esto es normal en desarrollo
                _logger.LogDebug("ℹ️ Actualizaciones no disponibles (modo desarrollo)");
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "⚠️ No se pudo verificar actualizaciones");
                return false;
            }
        }

        /// <summary>
        /// Muestra una notificación al usuario sobre actualizaciones disponibles
        /// </summary>
        public async Task<bool> NotifyAndPromptForUpdateAsync()
        {
            try
            {
                var hasUpdate = await CheckForUpdatesAsync();
                if (!hasUpdate)
                {
                    _logger.LogInformation("ℹ️ No hay actualizaciones disponibles");
                    return false;
                }

                // Obtener información de la versión disponible
                var updater = new UpdateManager(_updateUrl);
                var updateInfo = await updater.CheckForUpdatesAsync();
                var availableVersion = updateInfo?.TargetFullRelease?.Version;                // Mostrar diálogo al usuario
                var result = System.Windows.MessageBox.Show(
                    $"Nueva actualización disponible: v{availableVersion}\n\n" +
                    "¿Desea descargar e instalar la actualización ahora?\n\n" +
                    "Nota: La aplicación se reiniciará para aplicar la actualización.",
                    "Actualización Disponible",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Information);

                if (result == MessageBoxResult.Yes)
                {
                    await DownloadAndInstallUpdatesAsync();
                    return true;
                }
                else
                {
                    _logger.LogInformation("ℹ️ Usuario decidió no actualizar en este momento");
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error al notificar actualizaciones");
                return false;
            }
        }

        /// <summary>
        /// Descarga e instala las actualizaciones (SIN auto-elevación)
        /// </summary>
        public async Task DownloadAndInstallUpdatesAsync(Action<double>? onProgress = null)
        {
            try
            {
                _logger.LogInformation("📥 Iniciando descarga de actualizaciones...");
                var updater = new UpdateManager(_updateUrl);
                var updateInfo = await updater.CheckForUpdatesAsync();
                
                if (updateInfo != null)
                {
                    await updater.DownloadUpdatesAsync(updateInfo, progress =>
                    {
                        onProgress?.Invoke(progress);
                    });
                    
                    _logger.LogInformation("✅ Aplicando actualizaciones directamente (sin auto-elevación)...");
                    
                    // Aplicar actualizaciones directamente - la app ya se ejecuta como admin
                    updater.ApplyUpdatesAndRestart(updateInfo);
                }
                else
                {
                    _logger.LogWarning("⚠️ No se encontraron actualizaciones para descargar");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error descargando/instalando actualizaciones");
                  // Mostrar error amigable al usuario
                System.Windows.MessageBox.Show(
                    $"Error al aplicar la actualización:\n{ex.Message}\n\n" +
                    "Por favor, asegúrese de que:\n" +
                    "1. GestLog se está ejecutando como Administrador\n" +
                    "2. Tiene acceso al servidor de actualizaciones\n" +
                    "3. No hay otro proceso de GestLog ejecutándose",
                    "Error de Actualización",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                
                throw;
            }
        }

        /// <summary>
        /// Método legacy: Verifica, descarga e instala actualizaciones en una sola operación
        /// </summary>
        public async Task<bool> CheckAndUpdateAsync(Action<double> onProgress, CancellationToken cancellationToken)
        {
            try
            {
                var updater = new UpdateManager(_updateUrl);
                var updateInfo = await updater.CheckForUpdatesAsync();
                if (updateInfo != null)
                {
                    await updater.DownloadUpdatesAsync(updateInfo, progress =>
                    {
                        onProgress?.Invoke(progress);
                    }, cancellationToken);
                    updater.ApplyUpdatesAndRestart(updateInfo);
                    _logger.LogDebug("Actualización aplicada. Reiniciando aplicación...");
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en el proceso de actualización Velopack");
                return false;
            }
        }

        /// <summary>
        /// Obtiene información sobre la versión actual y disponible
        /// </summary>
        public async Task<(string? current, string? available)> GetVersionInfoAsync()
        {
            try
            {
                var updater = new UpdateManager(_updateUrl);
                var updateInfo = await updater.CheckForUpdatesAsync();
                
                var currentVersion = updater.CurrentVersion?.ToString();
                var availableVersion = updateInfo?.TargetFullRelease?.Version?.ToString();
                
                return (currentVersion, availableVersion);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error obteniendo información de versiones");
                return (null, null);
            }
        }
    }
}
